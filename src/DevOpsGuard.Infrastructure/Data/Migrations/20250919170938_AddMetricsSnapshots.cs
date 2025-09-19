using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevOpsGuard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMetricsSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetricsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BacklogHealthPct = table.Column<double>(type: "float", nullable: false),
                    SlaBreachRatePct = table.Column<double>(type: "float", nullable: false),
                    OverdueCount = table.Column<int>(type: "int", nullable: false),
                    RiskAvg = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricsSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetricsSnapshots_CapturedAtUtc",
                table: "MetricsSnapshots",
                column: "CapturedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetricsSnapshots");
        }
    }
}
