using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alfanar.MarketIntel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContactManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyContactInfo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Company = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HeadquartersAddressLine1 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HeadquartersAddressLine2 = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HeadquartersLandmark = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HeadquartersPoBox = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HeadquartersCity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HeadquartersPostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HeadquartersCountry = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HeadquartersCountryCode = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    SupportEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SalesEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MainPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TollFreePhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneAvailabilityDays = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneAvailabilityHours = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneAvailabilityTimezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyContactInfo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContactFormSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ResponseMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactFormSubmissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyOffices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyContactInfoId = table.Column<int>(type: "int", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OfficeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Building = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Floor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Tower = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BuildingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Street = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PoBox = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyOffices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyOffices_CompanyContactInfo_CompanyContactInfoId",
                        column: x => x.CompanyContactInfoId,
                        principalTable: "CompanyContactInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyContactInfo_Company",
                table: "CompanyContactInfo",
                column: "Company",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOffices_CompanyContactInfoId_Region",
                table: "CompanyOffices",
                columns: new[] { "CompanyContactInfoId", "Region" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOffices_Country",
                table: "CompanyOffices",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_ContactFormSubmissions_Email",
                table: "ContactFormSubmissions",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_ContactFormSubmissions_IsRead",
                table: "ContactFormSubmissions",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_ContactFormSubmissions_Status",
                table: "ContactFormSubmissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ContactFormSubmissions_SubmittedAt",
                table: "ContactFormSubmissions",
                column: "SubmittedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyOffices");

            migrationBuilder.DropTable(
                name: "ContactFormSubmissions");

            migrationBuilder.DropTable(
                name: "CompanyContactInfo");
        }
    }
}
