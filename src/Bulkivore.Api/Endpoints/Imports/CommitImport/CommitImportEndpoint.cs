using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Bulkivore.Api.Domain.Ingestion;
using Bulkivore.Api.Domain.Ingestion.Ports;
using Bulkivore.Api.Domain.Schema;
using Bulkivore.Api.Infrastructure.Persistence;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using Npgsql;
using NpgsqlTypes;

namespace Bulkivore.Api.Endpoints.Imports.CommitImport;

public class CommitImportEndpoint(
    AppDbContext dbContext,
    ISchemaInspector schemaInspector,
    [FromKeyedServices("bulkivore-test-db")]
    NpgsqlDataSource targetDataSource,
    IFileStorage fileStorage
)
    : Ep.Req<CommitImportRequest>.Res<ErrorOr<CommitImportResponse>>
{
    public override void Configure()
    {
        Group<ImportsGroup>();
        Post("{Id}/commit");
        AllowAnonymous();
    }

    public override async Task<ErrorOr<CommitImportResponse>> ExecuteAsync(
        CommitImportRequest req,
        CancellationToken ct)
    {
        var session = await dbContext.ImportSessions.FirstOrDefaultAsync(x => x.Id == req.Id, ct);
        if (session == null) return Error.NotFound();

        var errorOr = session.StartIngesting();
        if (errorOr.IsError) return errorOr.Errors;
        await dbContext.SaveChangesAsync(ct);

        var stopWatch = Stopwatch.StartNew();
        List<RowError> rowErrors = [];
        var successCount = 0;

        try
        {
            var tableSchema = await schemaInspector.InspectTableAsync(session.TargetTable, ct: ct);

            var mappings = session.ColumnMappings;
            var targetColumns = mappings.Values.ToList();

            var quotedColumns = string.Join(", ", targetColumns.Select(c => $"\"{c}\""));
            var copyCommand =
                $"""
                 COPY "{session.TargetTable}" ({quotedColumns}) FROM STDIN (FORMAT BINARY)
                 """;

            await using var connection = await targetDataSource.OpenConnectionAsync(ct);
            await using var writer = await connection.BeginBinaryImportAsync(copyCommand, ct);
            await using var fileStream = await fileStorage.OpenReadAsync(session.StorageKey, ct);
            var rows = fileStream.QueryAsync(useHeaderRow: true, cancellationToken: ct);

            var currentRowIndex = 1;

            await foreach (var row in rows)
            {
                currentRowIndex++;

                if (row is not IDictionary<string, object> rowDict) continue;

                List<ParsedCell> rowValues = [];
                var hasRowError = false;

                foreach (var (sourceHeader, targetColumnName) in session.ColumnMappings)
                {
                    rowDict.TryGetValue(sourceHeader, out var rawVal);
                    var colMeta = tableSchema[targetColumnName];

                    var cell = ParseCell(rawVal, colMeta);

                    if (cell is ParsedError error)
                    {
                        rowErrors.Add(
                            RowError.Create(currentRowIndex, sourceHeader, rawVal?.ToString(), error.Message)
                        );
                        hasRowError = true;
                        break;
                    }

                    rowValues.Add(cell);
                }

                if (hasRowError) continue;

                await writer.StartRowAsync(ct);
                foreach (var cell in rowValues)
                {
                    await cell.Match(
                        suc => WritePreconvertedValueAsync(writer, suc),
                        _ => writer.WriteNullAsync(ct),
                        _ => Task.CompletedTask
                    );
                }

                successCount++;
            }

            await writer.CompleteAsync(ct);

            session.Complete(successCount, rowErrors);
            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception e)
        {
            session.Fail(e.Message);
            await dbContext.SaveChangesAsync(ct);
            ThrowError($"Ingestion pipeline failed: {e.Message}", 500);
        }
        finally
        {
            await TryDeleteStorageFileAsync(session.StorageKey, ct);
            stopWatch.Stop();
        }

        return new CommitImportResponse(session, stopWatch.ElapsedMilliseconds);
    }

    private async Task TryDeleteStorageFileAsync(string storageKey, CancellationToken ct)
    {
        try
        {
            await fileStorage.DeleteAsync(storageKey, ct);
        }
        catch (Exception ex)
        {
            // Log warning without failing the user response
            Logger.LogWarning(ex, "Failed to purge storage key '{StorageKey}' from bucket", storageKey);
        }
    }


    private static ParsedCell ParseCell(object? rawValue, ColumnMetadata col)
    {
        // 1. Check for null or empty strings
        var isEmpty = rawValue is null or DBNull;
        var str = isEmpty ? null : rawValue!.ToString()?.Trim();

        if (isEmpty || string.IsNullOrEmpty(str))
        {
            // Enforce column nullability constraint
            if (!col.IsNullable)
            {
                return new ParsedError($"Column '{col.Name}' is required and cannot be null or empty.");
            }

            return new ParsedNull();
        }

        // 2. Parse by ColumnDataType
        return col.DataType switch
        {
            ColumnDataType.Integer =>
                int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
                    ? new ParsedSuccess(v, ColumnDataType.Integer)
                    : new ParsedError($"'{str}' is not a valid 32-bit integer."),
            ColumnDataType.BigInt =>
                long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v)
                    ? new ParsedSuccess(v, ColumnDataType.BigInt)
                    : new ParsedError($"'{str}' is not a valid 64-bit integer."),
            ColumnDataType.Decimal =>
                decimal.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
                    ? new ParsedSuccess(v, ColumnDataType.Decimal)
                    : new ParsedError($"'{str}' is not a valid decimal."),
            ColumnDataType.Boolean =>
                str switch
                {
                    "1" or "true" or "True" or "yes" or "Yes" => new ParsedSuccess(true, ColumnDataType.Boolean),
                    "0" or "false" or "False" or "no" or "No" => new ParsedSuccess(false, ColumnDataType.Boolean),
                    _ => new ParsedError($"'{str}' is not a valid boolean.")
                },
            ColumnDataType.DateTime =>
                DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var v)
                    ? new ParsedSuccess(v, ColumnDataType.DateTime)
                    : new ParsedError($"'{str}' is not a valid timestamp."),
            ColumnDataType.Date =>
                DateOnly.TryParse(str, CultureInfo.InvariantCulture, out var v)
                    ? new ParsedSuccess(v, ColumnDataType.Date)
                    : new ParsedError($"'{str}' is not a valid date."),
            ColumnDataType.Uuid =>
                Guid.TryParse(str, out var v)
                    ? new ParsedSuccess(v, ColumnDataType.Uuid)
                    : new ParsedError($"'{str}' is not a valid UUID."),
            ColumnDataType.Text or ColumnDataType.Json =>
                new ParsedSuccess(str, ColumnDataType.Text),
            ColumnDataType.Binary =>
                TryParseBinary(rawValue, str, out var bytes, out var err)
                    ? new ParsedSuccess(bytes, ColumnDataType.Binary)
                    : new ParsedError(err),
            _ => new ParsedSuccess(str, ColumnDataType.Text)
        };
    }

    private static bool TryParseBinary(
        object? raw,
        string str,
        [NotNullWhen(true)] out byte[]? bytes,
        [NotNullWhen(false)] out string? error)
    {
        try
        {
            bytes = raw as byte[] ?? Convert.FromBase64String(str);
            error = null;
            return true;
        }
        catch
        {
            bytes = null;
            error = "Value is not valid base64 binary.";
            return false;
        }
    }

    private static async Task WritePreconvertedValueAsync(
        NpgsqlBinaryImporter writer,
        ParsedSuccess success)
    {
        var value = success.Value;
        var dataType = success.DataType;

        switch (dataType)
        {
            case ColumnDataType.Integer: await writer.WriteAsync((int)value, NpgsqlDbType.Integer); break;
            case ColumnDataType.BigInt: await writer.WriteAsync((long)value, NpgsqlDbType.Bigint); break;
            case ColumnDataType.Decimal: await writer.WriteAsync((decimal)value, NpgsqlDbType.Numeric); break;
            case ColumnDataType.Boolean: await writer.WriteAsync((bool)value, NpgsqlDbType.Boolean); break;
            case ColumnDataType.DateTime: await writer.WriteAsync((DateTime)value, NpgsqlDbType.Timestamp); break;
            case ColumnDataType.Date: await writer.WriteAsync((DateOnly)value, NpgsqlDbType.Date); break;
            case ColumnDataType.Uuid: await writer.WriteAsync((Guid)value, NpgsqlDbType.Uuid); break;
            case ColumnDataType.Json: await writer.WriteAsync((string)value, NpgsqlDbType.Jsonb); break;
            case ColumnDataType.Binary: await writer.WriteAsync((byte[])value, NpgsqlDbType.Bytea); break;
            case ColumnDataType.Text:
            default:
                await writer.WriteAsync((string)value, NpgsqlDbType.Text);
                break;
        }
    }
}
