using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlertSubType",
                table: "SmartAlerts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    NotifyOnCritical = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnHigh = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnMedium = table.Column<bool>(type: "bit", nullable: false),
                    AlertTypesToNotify = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    KeywordsToNotify = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationQueues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Recipient = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationQueues", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_EmailEnabled",
                table: "NotificationPreferences",
                column: "EmailEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueues_AlertId",
                table: "NotificationQueues",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueues_CreatedAt",
                table: "NotificationQueues",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueues_Status",
                table: "NotificationQueues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationQueues_UserId",
                table: "NotificationQueues",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "NotificationQueues");

            migrationBuilder.DropColumn(
                name: "AlertSubType",
                table: "SmartAlerts");
        }
    }
}
