using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIntelligenceReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IntelligenceReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Keyword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GeneratedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MarketMovements = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompetitorUpdates = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MaSignals = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RisksAndOpportunities = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RawArticleCount = table.Column<int>(type: "int", nullable: false),
                    DeduplicatedArticleCount = table.Column<int>(type: "int", nullable: false),
                    AiModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    ProcessingTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    PdfFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntelligenceReportResults",
                columns: table => new
                {
                    IntelligenceReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebSearchResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntelligenceReportResults", x => new { x.IntelligenceReportId, x.WebSearchResultId });
                    table.ForeignKey(
                        name: "FK_IntelligenceReportResults_IntelligenceReports_IntelligenceReportId",
                        column: x => x.IntelligenceReportId,
                        principalTable: "IntelligenceReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntelligenceReportResults_WebSearchResults_WebSearchResultId",
                        column: x => x.WebSearchResultId,
                        principalTable: "WebSearchResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceReportResults_WebSearchResultId",
                table: "IntelligenceReportResults",
                column: "WebSearchResultId");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceReports_GeneratedUtc",
                table: "IntelligenceReports",
                column: "GeneratedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceReports_Keyword",
                table: "IntelligenceReports",
                column: "Keyword");

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceReports_Keyword_GeneratedUtc",
                table: "IntelligenceReports",
                columns: new[] { "Keyword", "GeneratedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IntelligenceReports_Status",
                table: "IntelligenceReports",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntelligenceReportResults");

            migrationBuilder.DropTable(
                name: "IntelligenceReports");
        }
    }
}
