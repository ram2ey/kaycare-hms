using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Part3Tier2CoreChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Wards",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "VitalSigns",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Users",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Suppliers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ServiceCatalogItems",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Refunds",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Referrals",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "RadiologyOrders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "PurchaseOrders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "PrescriptionTemplates",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Prescriptions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<Guid>(
                name: "DrugInventoryId",
                table: "PrescriptionItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Payments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "PayerTariffs",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Payers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Patients",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "PatientDocuments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "NursingNotes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "MedicationAdministrations",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "LabResults",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "LabOrders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "InsuranceClaims",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "FacilitySettings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "DrugInventory",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CriticalCallLogs",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CreditNotes",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Consultations",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "BillTemplates",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Bills",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Beds",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Appointments",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Admissions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "RevokedTokens",
                columns: table => new
                {
                    RevokedTokenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Jti = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevokedTokens", x => x.RevokedTokenId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_DrugInventoryId",
                table: "PrescriptionItems",
                column: "DrugInventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_TenantId_DrugInventoryId",
                table: "PrescriptionItems",
                columns: new[] { "TenantId", "DrugInventoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_RevokedTokens_ExpiresAt",
                table: "RevokedTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RevokedTokens_Jti",
                table: "RevokedTokens",
                column: "Jti",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PrescriptionItems_DrugInventory_DrugInventoryId",
                table: "PrescriptionItems",
                column: "DrugInventoryId",
                principalTable: "DrugInventory",
                principalColumn: "DrugInventoryId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrescriptionItems_DrugInventory_DrugInventoryId",
                table: "PrescriptionItems");

            migrationBuilder.DropTable(
                name: "RevokedTokens");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_DrugInventoryId",
                table: "PrescriptionItems");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_TenantId_DrugInventoryId",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Wards");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "VitalSigns");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ServiceCatalogItems");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Refunds");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Referrals");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "PrescriptionTemplates");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "DrugInventoryId",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "PayerTariffs");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Payers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "PatientDocuments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "NursingNotes");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "MedicationAdministrations");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "LabOrders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "InsuranceClaims");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "FacilitySettings");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "DrugInventory");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "CriticalCallLogs");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "CreditNotes");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Consultations");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "BillTemplates");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Admissions");
        }
    }
}
