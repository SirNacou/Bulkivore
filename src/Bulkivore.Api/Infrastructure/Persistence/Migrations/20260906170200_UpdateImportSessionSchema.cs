using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bulkivore.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateImportSessionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "import_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_table = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file = table.Column<string>(type: "text", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    column_mappings = table.Column<IReadOnlyDictionary<string, string>>(type: "jsonb", nullable: false),
                    success_row_count = table.Column<int>(type: "integer", nullable: false),
                    failed_row_count = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_errors = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_import_sessions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_import_sessions_tenant_target_table",
                table: "import_sessions",
                columns: new[] { "tenant_id", "target_table" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_sessions");
        }
    }
}
