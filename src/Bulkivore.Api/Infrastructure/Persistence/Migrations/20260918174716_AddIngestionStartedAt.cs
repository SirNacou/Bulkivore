using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bulkivore.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIngestionStartedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ingestion_started_at",
                table: "import_sessions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ingestion_started_at",
                table: "import_sessions");
        }
    }
}
