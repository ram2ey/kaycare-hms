using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckConstraintsAndFixCascades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CriticalCallLogs_LabOrderItems_LabOrderItemId",
                table: "CriticalCallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_InpatientCharges_Admissions_AdmissionId",
                table: "InpatientCharges");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAiEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "AllowedAiTiers",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Standard",
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "AiRequestsThisMonth",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "AiMonthlyQuota",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 500,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Wards_DailyRate_NonNegative",
                table: "Wards",
                sql: "\"DailyRate\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Tenants_Quotas_Positive",
                table: "Tenants",
                sql: "\"MaxUsers\" > 0 AND \"StorageQuotaGB\" > 0 AND \"AiMonthlyQuota\" >= 0 AND \"AiRequestsThisMonth\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovements_Quantities_NonNegative",
                table: "StockMovements",
                sql: "\"Quantity\" > 0 AND \"PreviousStock\" >= 0 AND \"NewStock\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceCatalogItems_UnitPrice_NonNegative",
                table: "ServiceCatalogItems",
                sql: "\"UnitPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Refunds_Amount_Positive",
                table: "Refunds",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Refunds_Status",
                table: "Refunds",
                sql: "\"Status\" IN ('Pending','Processed','Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Referrals_Status",
                table: "Referrals",
                sql: "\"Status\" IN ('Draft','Sent','Accepted','Completed','Declined','Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RadiologyOrders_Status",
                table: "RadiologyOrders",
                sql: "\"Status\" IN ('Pending','Scheduled','InProgress','Completed','Signed','Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RadiologyOrderItems_Status",
                table: "RadiologyOrderItems",
                sql: "\"Status\" IN ('Ordered','Acquired','Reported','Signed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PurchaseOrders_Status",
                table: "PurchaseOrders",
                sql: "\"Status\" IN ('Draft','Ordered','PartiallyReceived','Received','Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PurchaseOrderItems_Quantities_NonNegative",
                table: "PurchaseOrderItems",
                sql: "\"Quantity\" > 0 AND \"QuantityReceived\" >= 0 AND \"UnitCost\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PrescriptionTemplateItems_Quantities_NonNegative",
                table: "PrescriptionTemplateItems",
                sql: "\"Quantity\" > 0 AND \"DurationDays\" > 0 AND \"Refills\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Prescriptions_Status",
                table: "Prescriptions",
                sql: "\"Status\" IN ('Active','Dispensed','Cancelled','Expired','PartiallyDispensed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PrescriptionItems_Quantities_NonNegative",
                table: "PrescriptionItems",
                sql: "\"Quantity\" > 0 AND \"DurationDays\" > 0 AND \"Refills\" >= 0 AND \"QuantityDispensed\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PayerTariffs_TariffPrice_NonNegative",
                table: "PayerTariffs",
                sql: "\"TariffPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MedicationAdministrations_Status",
                table: "MedicationAdministrations",
                sql: "\"Status\" IN ('Given','Held','Refused','NotAvailable')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LabResults_Status",
                table: "LabResults",
                sql: "\"Status\" IN ('Received','Verified')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LabOrders_Status",
                table: "LabOrders",
                sql: "\"Status\" IN ('Pending','Active','PartiallyCompleted','Completed','Signed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LabOrderItems_Status",
                table: "LabOrderItems",
                sql: "\"Status\" IN ('Ordered','SampleReceived','Resulted','Signed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InsuranceClaims_Amounts_NonNegative",
                table: "InsuranceClaims",
                sql: "\"ClaimAmount\" >= 0 AND \"ApprovedAmount\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InsuranceClaims_Status",
                table: "InsuranceClaims",
                sql: "\"Status\" IN ('Draft','Submitted','Approved','PartiallyApproved','Rejected','Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InpatientCharges_Quantity_Positive",
                table: "InpatientCharges",
                sql: "\"Quantity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_InpatientCharges_UnitPrice_NonNegative",
                table: "InpatientCharges",
                sql: "\"UnitPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DrugInventory_Stock_NonNegative",
                table: "DrugInventory",
                sql: "\"CurrentStock\" >= 0 AND \"ReorderThreshold\" >= 0 AND \"UnitCost\" >= 0 AND \"SellingPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DispenseEventItems_QuantityDispensed_Positive",
                table: "DispenseEventItems",
                sql: "\"QuantityDispensed\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CreditNotes_Amount_Positive",
                table: "CreditNotes",
                sql: "\"Amount\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CreditNotes_Status",
                table: "CreditNotes",
                sql: "\"Status\" IN ('Draft','Approved','Applied','Voided')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Consultations_Status",
                table: "Consultations",
                sql: "\"Status\" IN ('Draft','Signed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BillTemplateItems_Quantity_Positive",
                table: "BillTemplateItems",
                sql: "\"Quantity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BillTemplateItems_UnitPrice_NonNegative",
                table: "BillTemplateItems",
                sql: "\"UnitPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bills_Amounts_NonNegative",
                table: "Bills",
                sql: "\"TotalAmount\" >= 0 AND \"DiscountAmount\" >= 0 AND \"WriteOffAmount\" >= 0 AND \"PaidAmount\" >= 0 AND \"CreditNoteTotal\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Bills_Status",
                table: "Bills",
                sql: "\"Status\" IN ('Draft','Issued','PartiallyPaid','Paid','Cancelled','Void','WrittenOff')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BillItems_Quantity_Positive",
                table: "BillItems",
                sql: "\"Quantity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BillItems_UnitPrice_NonNegative",
                table: "BillItems",
                sql: "\"UnitPrice\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Beds_Status",
                table: "Beds",
                sql: "\"Status\" IN ('Available','Occupied','Maintenance')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Appointments_Status",
                table: "Appointments",
                sql: "\"Status\" IN ('Scheduled','Confirmed','CheckedIn','InProgress','Completed','Cancelled','NoShow')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Admissions_Status",
                table: "Admissions",
                sql: "\"Status\" IN ('Active','Discharged')");

            migrationBuilder.AddForeignKey(
                name: "FK_CriticalCallLogs_LabOrderItems_LabOrderItemId",
                table: "CriticalCallLogs",
                column: "LabOrderItemId",
                principalTable: "LabOrderItems",
                principalColumn: "LabOrderItemId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InpatientCharges_Admissions_AdmissionId",
                table: "InpatientCharges",
                column: "AdmissionId",
                principalTable: "Admissions",
                principalColumn: "AdmissionId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CriticalCallLogs_LabOrderItems_LabOrderItemId",
                table: "CriticalCallLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_InpatientCharges_Admissions_AdmissionId",
                table: "InpatientCharges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Wards_DailyRate_NonNegative",
                table: "Wards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Tenants_Quotas_Positive",
                table: "Tenants");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovements_Quantities_NonNegative",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceCatalogItems_UnitPrice_NonNegative",
                table: "ServiceCatalogItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Refunds_Amount_Positive",
                table: "Refunds");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Refunds_Status",
                table: "Refunds");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Referrals_Status",
                table: "Referrals");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RadiologyOrders_Status",
                table: "RadiologyOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RadiologyOrderItems_Status",
                table: "RadiologyOrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PurchaseOrders_Status",
                table: "PurchaseOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PurchaseOrderItems_Quantities_NonNegative",
                table: "PurchaseOrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PrescriptionTemplateItems_Quantities_NonNegative",
                table: "PrescriptionTemplateItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Prescriptions_Status",
                table: "Prescriptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PrescriptionItems_Quantities_NonNegative",
                table: "PrescriptionItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_Amount_Positive",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PayerTariffs_TariffPrice_NonNegative",
                table: "PayerTariffs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MedicationAdministrations_Status",
                table: "MedicationAdministrations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LabResults_Status",
                table: "LabResults");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LabOrders_Status",
                table: "LabOrders");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LabOrderItems_Status",
                table: "LabOrderItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InsuranceClaims_Amounts_NonNegative",
                table: "InsuranceClaims");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InsuranceClaims_Status",
                table: "InsuranceClaims");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InpatientCharges_Quantity_Positive",
                table: "InpatientCharges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_InpatientCharges_UnitPrice_NonNegative",
                table: "InpatientCharges");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DrugInventory_Stock_NonNegative",
                table: "DrugInventory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DispenseEventItems_QuantityDispensed_Positive",
                table: "DispenseEventItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CreditNotes_Amount_Positive",
                table: "CreditNotes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CreditNotes_Status",
                table: "CreditNotes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Consultations_Status",
                table: "Consultations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BillTemplateItems_Quantity_Positive",
                table: "BillTemplateItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BillTemplateItems_UnitPrice_NonNegative",
                table: "BillTemplateItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bills_Amounts_NonNegative",
                table: "Bills");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Bills_Status",
                table: "Bills");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BillItems_Quantity_Positive",
                table: "BillItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BillItems_UnitPrice_NonNegative",
                table: "BillItems");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Beds_Status",
                table: "Beds");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Appointments_Status",
                table: "Appointments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Admissions_Status",
                table: "Admissions");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAiEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "AllowedAiTiers",
                table: "Tenants",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldDefaultValue: "Standard");

            migrationBuilder.AlterColumn<int>(
                name: "AiRequestsThisMonth",
                table: "Tenants",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "AiMonthlyQuota",
                table: "Tenants",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 500);

            migrationBuilder.AddForeignKey(
                name: "FK_CriticalCallLogs_LabOrderItems_LabOrderItemId",
                table: "CriticalCallLogs",
                column: "LabOrderItemId",
                principalTable: "LabOrderItems",
                principalColumn: "LabOrderItemId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InpatientCharges_Admissions_AdmissionId",
                table: "InpatientCharges",
                column: "AdmissionId",
                principalTable: "Admissions",
                principalColumn: "AdmissionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
