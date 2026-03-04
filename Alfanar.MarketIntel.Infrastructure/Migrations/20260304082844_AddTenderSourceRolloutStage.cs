using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderSourceRolloutStage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCanary",
                table: "TenderSources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RolloutStage",
                table: "TenderSources",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TenderSources_IsCanary",
                table: "TenderSources",
                column: "IsCanary");

            migrationBuilder.CreateIndex(
                name: "IX_TenderSources_RolloutStage",
                table: "TenderSources",
                column: "RolloutStage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TenderSources_IsCanary",
                table: "TenderSources");

            migrationBuilder.DropIndex(
                name: "IX_TenderSources_RolloutStage",
                table: "TenderSources");

            migrationBuilder.DropColumn(
                name: "IsCanary",
                table: "TenderSources");

            migrationBuilder.DropColumn(
                name: "RolloutStage",
                table: "TenderSources");
        }
    }
}
