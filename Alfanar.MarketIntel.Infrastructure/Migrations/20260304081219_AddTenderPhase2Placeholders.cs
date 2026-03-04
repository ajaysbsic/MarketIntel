using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderPhase2Placeholders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderAiAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExtractedRequirementsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderAiAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderAiAnalyses_TenderVersions_TenderVersionId",
                        column: x => x.TenderVersionId,
                        principalTable: "TenderVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderCapabilityGaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Requirement = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InternalCapability = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GapLevel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderCapabilityGaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderCapabilityGaps_TenderVersions_TenderVersionId",
                        column: x => x.TenderVersionId,
                        principalTable: "TenderVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WinProbability = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    RiskScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    ComponentsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    ScoringModel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderScores_TenderVersions_TenderVersionId",
                        column: x => x.TenderVersionId,
                        principalTable: "TenderVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenderAiAnalyses_CreatedAt",
                table: "TenderAiAnalyses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAiAnalyses_TenderVersionId",
                table: "TenderAiAnalyses",
                column: "TenderVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderCapabilityGaps_CreatedAt",
                table: "TenderCapabilityGaps",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenderCapabilityGaps_GapLevel",
                table: "TenderCapabilityGaps",
                column: "GapLevel");

            migrationBuilder.CreateIndex(
                name: "IX_TenderCapabilityGaps_TenderVersionId",
                table: "TenderCapabilityGaps",
                column: "TenderVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderScores_CreatedAt",
                table: "TenderScores",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenderScores_TenderVersionId",
                table: "TenderScores",
                column: "TenderVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenderAiAnalyses");

            migrationBuilder.DropTable(
                name: "TenderCapabilityGaps");

            migrationBuilder.DropTable(
                name: "TenderScores");
        }
    }
}
