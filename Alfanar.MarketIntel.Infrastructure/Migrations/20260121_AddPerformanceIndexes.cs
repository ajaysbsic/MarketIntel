using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NewsArticles - Common query patterns
            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_PublishedUtc",
                table: "NewsArticles",
                column: "PublishedUtc",
                descending: new bool[] { true });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_Title_Summary",
                table: "NewsArticles",
                columns: new[] { "Title", "Summary" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticles_Source_PublishedUtc",
                table: "NewsArticles",
                columns: new[] { "Source", "PublishedUtc" },
                descending: new bool[] { false, true });

            // FinancialReports - Common query patterns
            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_PublishedDate",
                table: "FinancialReports",
                column: "PublishedDate",
                descending: new bool[] { true });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_Company_PublishedDate",
                table: "FinancialReports",
                columns: new[] { "Company", "PublishedDate" },
                descending: new bool[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialReports_Title",
                table: "FinancialReports",
                column: "Title");

            // SmartAlerts - Common query patterns
            migrationBuilder.CreateIndex(
                name: "IX_SmartAlerts_Status_CreatedUtc",
                table: "SmartAlerts",
                columns: new[] { "Status", "CreatedUtc" },
                descending: new bool[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_SmartAlerts_Severity",
                table: "SmartAlerts",
                column: "Severity");

            // CompanyContactInfo - Lookup optimization
            migrationBuilder.CreateIndex(
                name: "IX_CompanyContactInfo_Company",
                table: "CompanyContactInfo",
                column: "Company");

            // CompanyOffices - Region queries
            migrationBuilder.CreateIndex(
                name: "IX_CompanyOffices_CompanyContactInfoId_Region",
                table: "CompanyOffices",
                columns: new[] { "CompanyContactInfoId", "Region" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOffices_Country",
                table: "CompanyOffices",
                column: "Country");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_PublishedUtc",
                table: "NewsArticles");

            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_Title_Summary",
                table: "NewsArticles");

            migrationBuilder.DropIndex(
                name: "IX_NewsArticles_Source_PublishedUtc",
                table: "NewsArticles");

            migrationBuilder.DropIndex(
                name: "IX_FinancialReports_PublishedDate",
                table: "FinancialReports");

            migrationBuilder.DropIndex(
                name: "IX_FinancialReports_Company_PublishedDate",
                table: "FinancialReports");

            migrationBuilder.DropIndex(
                name: "IX_FinancialReports_Title",
                table: "FinancialReports");

            migrationBuilder.DropIndex(
                name: "IX_SmartAlerts_Status_CreatedUtc",
                table: "SmartAlerts");

            migrationBuilder.DropIndex(
                name: "IX_SmartAlerts_Severity",
                table: "SmartAlerts");

            migrationBuilder.DropIndex(
                name: "IX_CompanyContactInfo_Company",
                table: "CompanyContactInfo");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOffices_CompanyContactInfoId_Region",
                table: "CompanyOffices");

            migrationBuilder.DropIndex(
                name: "IX_CompanyOffices_Country",
                table: "CompanyOffices");
        }
    }
}
