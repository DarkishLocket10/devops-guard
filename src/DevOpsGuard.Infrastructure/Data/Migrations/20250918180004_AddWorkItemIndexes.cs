using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevOpsGuard.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_DueDate",
                table: "WorkItems",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_Service",
                table: "WorkItems",
                column: "Service");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_Status",
                table: "WorkItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkItems_UpdatedAtUtc",
                table: "WorkItems",
                column: "UpdatedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkItems_DueDate",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_Service",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_Status",
                table: "WorkItems");

            migrationBuilder.DropIndex(
                name: "IX_WorkItems_UpdatedAtUtc",
                table: "WorkItems");
        }
    }
}
