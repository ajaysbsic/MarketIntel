using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWebSearchAndMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KeywordMonitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Keyword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CheckIntervalMinutes = table.Column<int>(type: "int", nullable: false),
                    LastCheckedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MaxResultsPerCheck = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeywordMonitors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechnologyReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PdfFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalResults = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebSearchResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeywordMonitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Keyword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Snippet = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PublishedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SearchProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RetrievedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFromMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebSearchResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebSearchResults_KeywordMonitors_KeywordMonitorId",
                        column: x => x.KeywordMonitorId,
                        principalTable: "KeywordMonitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ReportResults",
                columns: table => new
                {
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebSearchResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportResults", x => new { x.ReportId, x.WebSearchResultId });
                    table.ForeignKey(
                        name: "FK_ReportResults_TechnologyReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "TechnologyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportResults_WebSearchResults_WebSearchResultId",
                        column: x => x.WebSearchResultId,
                        principalTable: "WebSearchResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KeywordMonitors_IsActive",
                table: "KeywordMonitors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_KeywordMonitors_Keyword",
                table: "KeywordMonitors",
                column: "Keyword");

            migrationBuilder.CreateIndex(
                name: "IX_KeywordMonitors_LastCheckedUtc",
                table: "KeywordMonitors",
                column: "LastCheckedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReportResults_WebSearchResultId",
                table: "ReportResults",
                column: "WebSearchResultId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyReports_EndDate",
                table: "TechnologyReports",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyReports_GeneratedUtc",
                table: "TechnologyReports",
                column: "GeneratedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyReports_StartDate",
                table: "TechnologyReports",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_WebSearchResults_IsFromMonitoring",
                table: "WebSearchResults",
                column: "IsFromMonitoring");

            migrationBuilder.CreateIndex(
                name: "IX_WebSearchResults_Keyword",
                table: "WebSearchResults",
                column: "Keyword");

            migrationBuilder.CreateIndex(
                name: "IX_WebSearchResults_Keyword_Url",
                table: "WebSearchResults",
                columns: new[] { "Keyword", "Url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebSearchResults_KeywordMonitorId",
                table: "WebSearchResults",
                column: "KeywordMonitorId");

            migrationBuilder.CreateIndex(
                name: "IX_WebSearchResults_PublishedDate",
                table: "WebSearchResults",
                column: "PublishedDate");

            migrationBuilder.CreateIndex(
                name: "IX_WebSearchResults_RetrievedUtc",
                table: "WebSearchResults",
                column: "RetrievedUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportResults");

            migrationBuilder.DropTable(
                name: "TechnologyReports");

            migrationBuilder.DropTable(
                name: "WebSearchResults");

            migrationBuilder.DropTable(
                name: "KeywordMonitors");
        }
    }
}
