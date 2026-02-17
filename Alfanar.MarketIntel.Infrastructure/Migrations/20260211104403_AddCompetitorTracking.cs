using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitorTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "SmartAlerts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "SmartAlerts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "SmartAlerts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Competitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Industry = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsAutoDetected = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrendSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Keyword = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MentionCount = table.Column<int>(type: "int", nullable: false),
                    NewsCount = table.Column<int>(type: "int", nullable: false),
                    WebSearchCount = table.Column<int>(type: "int", nullable: false),
                    AverageSentiment = table.Column<double>(type: "float", nullable: false),
                    TopSources = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompetitorMentionCounts = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SignalStrength = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrendSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetitorMentions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Snippet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SentimentScore = table.Column<double>(type: "float", nullable: true),
                    SentimentLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MentionContext = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DetectedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAutoDetected = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitorMentions_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmartAlerts_SourceId",
                table: "SmartAlerts",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartAlerts_SourceType",
                table: "SmartAlerts",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorMentions_CompetitorId",
                table: "CompetitorMentions",
                column: "CompetitorId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorMentions_DetectedUtc",
                table: "CompetitorMentions",
                column: "DetectedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorMentions_SourceId",
                table: "CompetitorMentions",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorMentions_SourceType",
                table: "CompetitorMentions",
                column: "SourceType");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_IsActive",
                table: "Competitors",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_IsAutoDetected",
                table: "Competitors",
                column: "IsAutoDetected");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_Name",
                table: "Competitors",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrendSnapshots_Keyword",
                table: "TrendSnapshots",
                column: "Keyword");

            migrationBuilder.CreateIndex(
                name: "IX_TrendSnapshots_Keyword_SnapshotDate",
                table: "TrendSnapshots",
                columns: new[] { "Keyword", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrendSnapshots_SnapshotDate",
                table: "TrendSnapshots",
                column: "SnapshotDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitorMentions");

            migrationBuilder.DropTable(
                name: "TrendSnapshots");

            migrationBuilder.DropTable(
                name: "Competitors");

            migrationBuilder.DropIndex(
                name: "IX_SmartAlerts_SourceId",
                table: "SmartAlerts");

            migrationBuilder.DropIndex(
                name: "IX_SmartAlerts_SourceType",
                table: "SmartAlerts");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "SmartAlerts");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "SmartAlerts");

            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "SmartAlerts");
        }
    }
}
