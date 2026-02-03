using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedContactData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CompanyContactInfo",
                columns: new[] { "Id", "Company", "CreatedAt", "HeadquartersAddressLine1", "HeadquartersAddressLine2", "HeadquartersCity", "HeadquartersCountry", "HeadquartersCountryCode", "HeadquartersLandmark", "HeadquartersPoBox", "HeadquartersPostalCode", "MainPhone", "PhoneAvailabilityDays", "PhoneAvailabilityHours", "PhoneAvailabilityTimezone", "SalesEmail", "SupportEmail", "TollFreePhone", "UpdatedAt" },
                values: new object[] { 1, "alfanar", new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc), "Al-Nafl - Northern Ring Road", "Between Exits 5 & 6", "Riyadh", "Kingdom of Saudi Arabia", "KSA", "Near King Abdulaziz Center for National Dialogue", "P.O. Box 301", "11411", "+966 573786035", "Mon-Fri", "9AM-6PM", "EST", "sales@alfanar.com", "support@alfanar.com", "800-124-1333", new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "CompanyOffices",
                columns: new[] { "Id", "Area", "Building", "BuildingNumber", "City", "CompanyContactInfoId", "CompanyName", "Country", "CreatedAt", "District", "Floor", "OfficeType", "PoBox", "Region", "Street", "Tower", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "alfanar Industrial City", "Sales and Marketing Building", null, null, 1, null, "Saudi Arabia", new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Sales and Marketing", null, "Saudi Arabia", null, null, new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, null, null, "Madrid", 1, null, "Spain", new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Regional Office", null, "Europe", null, null, new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, null, null, null, null, 1, "alfanar Electrical Systems LLC", "United Arab Emirates", new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Subsidiary", null, "UAE", null, null, new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "DLF Cybercity, Phase-3", null, "Building No. 5", "Gurgaon", 1, null, "India", new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc), null, "15th Floor", "Regional Office", null, "India", null, "Tower B", new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "Sheraton Al Matar", null, null, "Cairo", 1, null, "Egypt", new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc), "El Nozha", null, "Regional Office", "P.O. Box 11736", "Egypt", "181 El-Orouba St", null, new DateTime(2025, 1, 21, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CompanyOffices",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CompanyOffices",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CompanyOffices",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CompanyOffices",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CompanyOffices",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "CompanyContactInfo",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
