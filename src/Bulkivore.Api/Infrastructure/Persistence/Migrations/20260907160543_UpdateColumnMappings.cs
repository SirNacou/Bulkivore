using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bulkivore.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateColumnMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "column_mappings",
                table: "import_sessions",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(IReadOnlyDictionary<string, string>),
                oldType: "jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<IReadOnlyDictionary<string, string>>(
                name: "column_mappings",
                table: "import_sessions",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);
        }
    }
}
