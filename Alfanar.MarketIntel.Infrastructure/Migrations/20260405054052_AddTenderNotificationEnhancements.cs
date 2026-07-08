using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenderNotificationEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenderNotices_TenderVersions_CurrentVersionId",
                table: "TenderNotices");

            migrationBuilder.DropForeignKey(
                name: "FK_TenderNotificationLogs_TenderVersions_TenderVersionId",
                table: "TenderNotificationLogs");

            migrationBuilder.AddColumn<string>(
                name: "EntityFilter",
                table: "TenderNotificationRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "TenderNotificationLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotificationBody",
                table: "TenderNotificationLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationTitle",
                table: "TenderNotificationLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "TenderNotificationLogs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderNotices_TenderVersions_CurrentVersionId",
                table: "TenderNotices",
                column: "CurrentVersionId",
                principalTable: "TenderVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderNotificationLogs_TenderVersions_TenderVersionId",
                table: "TenderNotificationLogs",
                column: "TenderVersionId",
                principalTable: "TenderVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TenderNotices_TenderVersions_CurrentVersionId",
                table: "TenderNotices");

            migrationBuilder.DropForeignKey(
                name: "FK_TenderNotificationLogs_TenderVersions_TenderVersionId",
                table: "TenderNotificationLogs");

            migrationBuilder.DropColumn(
                name: "EntityFilter",
                table: "TenderNotificationRules");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "TenderNotificationLogs");

            migrationBuilder.DropColumn(
                name: "NotificationBody",
                table: "TenderNotificationLogs");

            migrationBuilder.DropColumn(
                name: "NotificationTitle",
                table: "TenderNotificationLogs");

            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "TenderNotificationLogs");

            migrationBuilder.AddForeignKey(
                name: "FK_TenderNotices_TenderVersions_CurrentVersionId",
                table: "TenderNotices",
                column: "CurrentVersionId",
                principalTable: "TenderVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderNotificationLogs_TenderVersions_TenderVersionId",
                table: "TenderNotificationLogs",
                column: "TenderVersionId",
                principalTable: "TenderVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
