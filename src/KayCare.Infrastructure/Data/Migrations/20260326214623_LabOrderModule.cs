using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LabOrderModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LabOrderItemId",
                table: "LabResults",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LabOrders",
                columns: table => new
                {
                    LabOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BillId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderingDoctorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Organisation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabOrders", x => x.LabOrderId);
                    table.ForeignKey(
                        name: "FK_LabOrders_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "BillId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabOrders_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "Consultations",
                        principalColumn: "ConsultationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabOrders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabOrders_Users_OrderingDoctorUserId",
                        column: x => x.OrderingDoctorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabTestCatalog",
                columns: table => new
                {
                    LabTestCatalogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TestCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstrumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsManualEntry = table.Column<bool>(type: "bit", nullable: false),
                    TatHours = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabTestCatalog", x => x.LabTestCatalogId);
                });

            migrationBuilder.CreateTable(
                name: "LabOrderItems",
                columns: table => new
                {
                    LabOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    LabOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LabTestCatalogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstrumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsManualEntry = table.Column<bool>(type: "bit", nullable: false),
                    TatHours = table.Column<int>(type: "int", nullable: false),
                    AccessionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SampleReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ManualResult = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManualResultNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LabResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabOrderItems", x => x.LabOrderItemId);
                    table.ForeignKey(
                        name: "FK_LabOrderItems_LabOrders_LabOrderId",
                        column: x => x.LabOrderId,
                        principalTable: "LabOrders",
                        principalColumn: "LabOrderId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LabOrderItems_LabTestCatalog_LabTestCatalogId",
                        column: x => x.LabTestCatalogId,
                        principalTable: "LabTestCatalog",
                        principalColumn: "LabTestCatalogId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LabTestCatalog",
                columns: new[] { "LabTestCatalogId", "Department", "InstrumentType", "IsActive", "IsManualEntry", "TatHours", "TestCode", "TestName" },
                values: new object[,]
                {
                    { new Guid("10000001-0000-0000-0000-000000000001"), "Haematology", "DxH560", true, false, 2, "FBC", "Full Blood Count" },
                    { new Guid("10000001-0000-0000-0000-000000000002"), "Haematology", "DxH560", true, false, 2, "ESR", "Erythrocyte Sedimentation Rate" },
                    { new Guid("10000001-0000-0000-0000-000000000003"), "Haematology", null, true, true, 4, "MPS", "Blood Film for Malaria Parasite Screen" },
                    { new Guid("10000001-0000-0000-0000-000000000004"), "Chemistry", "DxC500", true, false, 3, "BUE", "Blood Urea and Electrolytes & Creatinine" },
                    { new Guid("10000001-0000-0000-0000-000000000005"), "Chemistry", "DxC500", true, false, 3, "LFT", "Liver Function Tests" },
                    { new Guid("10000001-0000-0000-0000-000000000006"), "Chemistry", "DxC500", true, false, 3, "MAGNESIUM", "Magnesium" },
                    { new Guid("10000001-0000-0000-0000-000000000007"), "Chemistry", "DxC500", true, false, 3, "CALCIUM", "Calcium" },
                    { new Guid("10000001-0000-0000-0000-000000000008"), "Chemistry", "DxC500", true, false, 1, "FBG", "Fasting Blood Glucose" },
                    { new Guid("10000001-0000-0000-0000-000000000009"), "Chemistry", "DxC500", true, false, 1, "RBG", "Random Blood Glucose" },
                    { new Guid("10000001-0000-0000-0000-000000000010"), "Chemistry", "DxC500", true, false, 3, "LIPID", "Lipid Profile" },
                    { new Guid("10000001-0000-0000-0000-000000000011"), "Immunology", "CobasE411", true, false, 4, "TFT", "Thyroid Function Tests (TSH, T3, T4)" },
                    { new Guid("10000001-0000-0000-0000-000000000012"), "Immunology", "CobasE411", true, false, 4, "VIT_B12", "Vitamin B12" },
                    { new Guid("10000001-0000-0000-0000-000000000013"), "Immunology", "CobasE411", true, false, 4, "FOLATE", "Folate / Folic Acid" },
                    { new Guid("10000001-0000-0000-0000-000000000014"), "Immunology", "CobasE411", true, false, 4, "HBA1C", "Glycosylated Haemoglobin (HbA1c)" },
                    { new Guid("10000001-0000-0000-0000-000000000015"), "Serology", null, true, true, 6, "TYPHOID", "Typhoid IgG / IgM" },
                    { new Guid("10000001-0000-0000-0000-000000000016"), "Serology", null, true, true, 6, "WIDAL", "Widal Test" },
                    { new Guid("10000001-0000-0000-0000-000000000017"), "Urinalysis", null, true, true, 2, "URINE_RE", "Urine Routine Examination" },
                    { new Guid("10000001-0000-0000-0000-000000000018"), "Serology", null, true, true, 3, "HBsAg", "Hepatitis B Surface Antigen" },
                    { new Guid("10000001-0000-0000-0000-000000000019"), "Serology", null, true, true, 1, "HIV", "HIV Screening Test" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_LabOrderItemId",
                table: "LabResults",
                column: "LabOrderItemId",
                unique: true,
                filter: "[LabOrderItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrderItems_LabOrderId",
                table: "LabOrderItems",
                column: "LabOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrderItems_LabTestCatalogId",
                table: "LabOrderItems",
                column: "LabTestCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrderItems_TenantId_AccessionNumber",
                table: "LabOrderItems",
                columns: new[] { "TenantId", "AccessionNumber" },
                unique: true,
                filter: "[AccessionNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_BillId",
                table: "LabOrders",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_ConsultationId",
                table: "LabOrders",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_OrderingDoctorUserId",
                table: "LabOrders",
                column: "OrderingDoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_PatientId",
                table: "LabOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_TenantId_CreatedAt",
                table: "LabOrders",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_TenantId_PatientId",
                table: "LabOrders",
                columns: new[] { "TenantId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_TenantId_Status",
                table: "LabOrders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LabTestCatalog_TestCode",
                table: "LabTestCatalog",
                column: "TestCode",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LabResults_LabOrderItems_LabOrderItemId",
                table: "LabResults",
                column: "LabOrderItemId",
                principalTable: "LabOrderItems",
                principalColumn: "LabOrderItemId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LabResults_LabOrderItems_LabOrderItemId",
                table: "LabResults");

            migrationBuilder.DropTable(
                name: "LabOrderItems");

            migrationBuilder.DropTable(
                name: "LabOrders");

            migrationBuilder.DropTable(
                name: "LabTestCatalog");

            migrationBuilder.DropIndex(
                name: "IX_LabResults_LabOrderItemId",
                table: "LabResults");

            migrationBuilder.DropColumn(
                name: "LabOrderItemId",
                table: "LabResults");
        }
    }
}
