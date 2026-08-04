using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentManagerRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "Description", "RoleName" },
                values: new object[,]
                {
                    { 9, "Head of Pharmacy / Chief Pharmacist", "PharmacyManager" },
                    { 10, "Head of Billing & Revenue Management", "BillingManager" },
                    { 11, "Head of Laboratory / Lab Director", "LabManager" },
                    { 12, "Head of Radiology & Imaging", "RadiologyManager" },
                    { 13, "Chief Nursing Officer / Matron", "NurseManager" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 13);
        }
    }
}
