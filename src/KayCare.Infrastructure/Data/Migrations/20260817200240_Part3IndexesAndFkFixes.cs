using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Part3IndexesAndFkFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayerTariffs_Payers_PayerId",
                table: "PayerTariffs");

            migrationBuilder.DropForeignKey(
                name: "FK_PayerTariffs_ServiceCatalogItems_ServiceCatalogItemId",
                table: "PayerTariffs");

            migrationBuilder.AlterColumn<decimal>(
                name: "TariffPrice",
                table: "PayerTariffs",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionItems_TenantId_PrescriptionId",
                table: "PrescriptionItems",
                columns: new[] { "TenantId", "PrescriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_TenantId_NationalId",
                table: "Patients",
                columns: new[] { "TenantId", "NationalId" },
                unique: true,
                filter: "\"NationalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrderItems_TenantId_LabOrderId",
                table: "LabOrderItems",
                columns: new[] { "TenantId", "LabOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispenseEventItems_TenantId_DispenseEventId",
                table: "DispenseEventItems",
                columns: new[] { "TenantId", "DispenseEventId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillTemplateItems_TenantId_BillTemplateId",
                table: "BillTemplateItems",
                columns: new[] { "TenantId", "BillTemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_AdmissionId",
                table: "Bills",
                column: "AdmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_BillItems_TenantId_BillId",
                table: "BillItems",
                columns: new[] { "TenantId", "BillId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Admissions_AdmissionId",
                table: "Bills",
                column: "AdmissionId",
                principalTable: "Admissions",
                principalColumn: "AdmissionId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayerTariffs_Payers_PayerId",
                table: "PayerTariffs",
                column: "PayerId",
                principalTable: "Payers",
                principalColumn: "PayerId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayerTariffs_ServiceCatalogItems_ServiceCatalogItemId",
                table: "PayerTariffs",
                column: "ServiceCatalogItemId",
                principalTable: "ServiceCatalogItems",
                principalColumn: "ServiceCatalogItemId",
                onDelete: ReferentialAction.Restrict);

            // L3/DB16: append-only was convention-only (AuditService never issues UPDATE/DELETE,
            // but nothing stopped a bug or a direct DB session from doing so). Enforce it at the
            // DB level.
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION prevent_auditlog_modification()
                RETURNS TRIGGER AS $$
                BEGIN
                    RAISE EXCEPTION 'AuditLogs is append-only: % is not permitted', TG_OP;
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER trg_auditlogs_append_only
                BEFORE UPDATE OR DELETE ON ""AuditLogs""
                FOR EACH ROW EXECUTE FUNCTION prevent_auditlog_modification();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TRIGGER IF EXISTS trg_auditlogs_append_only ON ""AuditLogs"";
                DROP FUNCTION IF EXISTS prevent_auditlog_modification();
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Admissions_AdmissionId",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_PayerTariffs_Payers_PayerId",
                table: "PayerTariffs");

            migrationBuilder.DropForeignKey(
                name: "FK_PayerTariffs_ServiceCatalogItems_ServiceCatalogItemId",
                table: "PayerTariffs");

            migrationBuilder.DropIndex(
                name: "IX_PrescriptionItems_TenantId_PrescriptionId",
                table: "PrescriptionItems");

            migrationBuilder.DropIndex(
                name: "IX_Patients_TenantId_NationalId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_LabOrderItems_TenantId_LabOrderId",
                table: "LabOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_DispenseEventItems_TenantId_DispenseEventId",
                table: "DispenseEventItems");

            migrationBuilder.DropIndex(
                name: "IX_BillTemplateItems_TenantId_BillTemplateId",
                table: "BillTemplateItems");

            migrationBuilder.DropIndex(
                name: "IX_Bills_AdmissionId",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_BillItems_TenantId_BillId",
                table: "BillItems");

            migrationBuilder.AlterColumn<decimal>(
                name: "TariffPrice",
                table: "PayerTariffs",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AddForeignKey(
                name: "FK_PayerTariffs_Payers_PayerId",
                table: "PayerTariffs",
                column: "PayerId",
                principalTable: "Payers",
                principalColumn: "PayerId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PayerTariffs_ServiceCatalogItems_ServiceCatalogItemId",
                table: "PayerTariffs",
                column: "ServiceCatalogItemId",
                principalTable: "ServiceCatalogItems",
                principalColumn: "ServiceCatalogItemId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
