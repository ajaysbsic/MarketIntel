using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialReportsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RelatedFinancialReportId",
                table: "NewsArticles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinancialReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DownloadUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    FiscalQuarter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    FiscalYear = table.Column<int>(type: "int", nullable: true),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sector = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExtractedText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    RequiredOcr = table.Column<bool>(type: "bit", nullable: false),
                    ProcessingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KeyHighlights = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinancialMetrics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrategicInitiatives = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MarketOutlook = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskFactors = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompetitivePosition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvestmentThesis = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentimentScore = table.Column<double>(type: "float", nullable: true),
                    SentimentLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AnalysisConfidence = table.Column<double>(type: "float", nullable: true),
                    AiModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    ProcessingTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RelatedEntities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportAnalyses_FinancialReports_FinancialReportId",
                        column: x => x.FinancialReportId,
                        principalTable: "FinancialReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinancialReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SectionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PageNumbers = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeyDataPoints = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExtractionConfidence = table.Column<double>(type: "float", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportSections_FinancialReports_FinancialReportId",
                        column: x => x.FinancialReportId,
                        principalTable: "FinancialReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_RelatedFinancialReportId",
                table: "NewsArticles",
                column: "RelatedFinancialReportId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_CompanyName",
                table: "FinancialReports",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_FiscalYear_FiscalQuarter",
                table: "FinancialReports",
                columns: new[] { "FiscalYear", "FiscalQuarter" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_IsProcessed",
                table: "FinancialReports",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_ProcessingStatus",
                table: "FinancialReports",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_PublishedDate",
                table: "FinancialReports",
                column: "PublishedDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_ReportType",
                table: "FinancialReports",
                column: "ReportType");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_SourceUrl",
                table: "FinancialReports",
                column: "SourceUrl",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportAnalyses_CreatedUtc",
                table: "ReportAnalyses",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAnalyses_FinancialReportId",
                table: "ReportAnalyses",
                column: "FinancialReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportAnalyses_SentimentScore",
                table: "ReportAnalyses",
                column: "SentimentScore");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSections_FinancialReportId",
                table: "ReportSections",
                column: "FinancialReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSections_FinancialReportId_OrderIndex",
                table: "ReportSections",
                columns: new[] { "FinancialReportId", "OrderIndex" });

            migrationBuilder.AddForeignKey(
                name: "FK_NewsArticles_FinancialReports_RelatedFinancialReportId",
                table: "NewsArticles",
                column: "RelatedFinancialReportId",
                principalTable: "FinancialReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NewsArticles_FinancialReports_RelatedFinancialReportId",
                table: "NewsArticles");

            migrationBuilder.DropTable(
                name: "ReportAnalyses");

            migrationBuilder.DropTable(
                name: "ReportSections");

            migrationBuilder.DropTable(
                name: "FinancialReports");

            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_RelatedFinancialReportId",
                table: "NewsArticles");

            migrationBuilder.DropColumn(
                name: "RelatedFinancialReportId",
                table: "NewsArticles");
        }
    }
}
