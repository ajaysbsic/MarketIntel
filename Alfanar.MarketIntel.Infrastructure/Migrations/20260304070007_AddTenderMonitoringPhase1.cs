using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderMonitoringPhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenderCountries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsoCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RegionGroup = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderCountries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenderNotificationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Channels = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryFilter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SectorFilter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuthorityFilter = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ValueMin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ValueMax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Keywords = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderNotificationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenderSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AuthMode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PollPriority = table.Column<int>(type: "int", nullable: false),
                    PollIntervalMin = table.Column<int>(type: "int", nullable: false),
                    RateLimitPolicyJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LegalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Owner = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenderAuthorities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AliasesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderAuthorities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderAuthorities_TenderCountries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "TenderCountries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenderAuditRaw",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RetrievedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RetentionUntil = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderAuditRaw", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderAuditRaw_TenderSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "TenderSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderIngestionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemsFetched = table.Column<int>(type: "int", nullable: false),
                    ItemsNew = table.Column<int>(type: "int", nullable: false),
                    ItemsUpdated = table.Column<int>(type: "int", nullable: false),
                    Errors = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    WorkerId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderIngestionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderIngestionRuns_TenderSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "TenderSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderNoticeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StoragePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RetrievedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenderNotices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Sector = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PublishDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CurrentVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FirstSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderNotices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderNotices_TenderAuthorities_AuthorityId",
                        column: x => x.AuthorityId,
                        principalTable: "TenderAuthorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TenderNotices_TenderCountries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "TenderCountries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenderNotices_TenderSources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "TenderSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenderVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderNoticeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    RawHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedFieldsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderVersions_TenderNotices_TenderNoticeId",
                        column: x => x.TenderNoticeId,
                        principalTable: "TenderNotices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderNotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderNoticeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenderVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DedupKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderNotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderNotificationLogs_TenderNotices_TenderNoticeId",
                        column: x => x.TenderNoticeId,
                        principalTable: "TenderNotices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenderNotificationLogs_TenderNotificationRules_RuleId",
                        column: x => x.RuleId,
                        principalTable: "TenderNotificationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenderNotificationLogs_TenderVersions_TenderVersionId",
                        column: x => x.TenderVersionId,
                        principalTable: "TenderVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenderAuditRaw_PayloadHash",
                table: "TenderAuditRaw",
                column: "PayloadHash");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAuditRaw_SourceId_ExternalId_RetrievedAt",
                table: "TenderAuditRaw",
                columns: new[] { "SourceId", "ExternalId", "RetrievedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderAuthorities_CountryId",
                table: "TenderAuthorities",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAuthorities_CountryId_NormalizedName",
                table: "TenderAuthorities",
                columns: new[] { "CountryId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderAuthorities_NormalizedName",
                table: "TenderAuthorities",
                column: "NormalizedName");

            migrationBuilder.CreateIndex(
                name: "IX_TenderCountries_IsActive",
                table: "TenderCountries",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TenderCountries_IsoCode",
                table: "TenderCountries",
                column: "IsoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderCountries_RegionGroup",
                table: "TenderCountries",
                column: "RegionGroup");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_TenderNoticeId",
                table: "TenderDocuments",
                column: "TenderNoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderDocuments_TenderNoticeId_FileHash",
                table: "TenderDocuments",
                columns: new[] { "TenderNoticeId", "FileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderIngestionRuns_SourceId_StartedAt",
                table: "TenderIngestionRuns",
                columns: new[] { "SourceId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderIngestionRuns_Status",
                table: "TenderIngestionRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotices_AuthorityId_PublishDate",
                table: "TenderNotices",
                columns: new[] { "AuthorityId", "PublishDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotices_CountryId_PublishDate",
                table: "TenderNotices",
                columns: new[] { "CountryId", "PublishDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotices_CurrentVersionId",
                table: "TenderNotices",
                column: "CurrentVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotices_IsActive",
                table: "TenderNotices",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotices_LastChangedAt",
                table: "TenderNotices",
                column: "LastChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotices_SourceId_ExternalId",
                table: "TenderNotices",
                columns: new[] { "SourceId", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotificationLogs_DedupKey",
                table: "TenderNotificationLogs",
                column: "DedupKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotificationLogs_RuleId",
                table: "TenderNotificationLogs",
                column: "RuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotificationLogs_SentAt",
                table: "TenderNotificationLogs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotificationLogs_TenderNoticeId",
                table: "TenderNotificationLogs",
                column: "TenderNoticeId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotificationLogs_TenderVersionId",
                table: "TenderNotificationLogs",
                column: "TenderVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotificationRules_IsActive",
                table: "TenderNotificationRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TenderNotificationRules_Scope_UserId",
                table: "TenderNotificationRules",
                columns: new[] { "Scope", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderSources_IsEnabled",
                table: "TenderSources",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_TenderSources_Name",
                table: "TenderSources",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderSources_PollPriority",
                table: "TenderSources",
                column: "PollPriority");

            migrationBuilder.CreateIndex(
                name: "IX_TenderVersions_DetectedAt",
                table: "TenderVersions",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TenderVersions_TenderNoticeId_VersionNo",
                table: "TenderVersions",
                columns: new[] { "TenderNoticeId", "VersionNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderDocuments_TenderNotices_TenderNoticeId",
                table: "TenderDocuments",
                column: "TenderNoticeId",
                principalTable: "TenderNotices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderNotices_TenderVersions_CurrentVersionId",
                table: "TenderNotices",
                column: "CurrentVersionId",
                principalTable: "TenderVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenderNotices_TenderSources_SourceId",
                table: "TenderNotices");

            migrationBuilder.DropForeignKey(
                name: "FK_TenderAuthorities_TenderCountries_CountryId",
                table: "TenderAuthorities");

            migrationBuilder.DropForeignKey(
                name: "FK_TenderNotices_TenderCountries_CountryId",
                table: "TenderNotices");

            migrationBuilder.DropForeignKey(
                name: "FK_TenderVersions_TenderNotices_TenderNoticeId",
                table: "TenderVersions");

            migrationBuilder.DropTable(
                name: "TenderAuditRaw");

            migrationBuilder.DropTable(
                name: "TenderDocuments");

            migrationBuilder.DropTable(
                name: "TenderIngestionRuns");

            migrationBuilder.DropTable(
                name: "TenderNotificationLogs");

            migrationBuilder.DropTable(
                name: "TenderNotificationRules");

            migrationBuilder.DropTable(
                name: "TenderSources");

            migrationBuilder.DropTable(
                name: "TenderCountries");

            migrationBuilder.DropTable(
                name: "TenderNotices");

            migrationBuilder.DropTable(
                name: "TenderAuthorities");

            migrationBuilder.DropTable(
                name: "TenderVersions");
        }
    }
}
