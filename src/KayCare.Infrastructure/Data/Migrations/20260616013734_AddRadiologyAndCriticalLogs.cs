using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiologyAndCriticalLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CriticalReferenceRange",
                table: "LabTestCatalog",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CriticalCallLogId",
                table: "LabOrderItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCritical",
                table: "LabOrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CriticalCallLogs",
                columns: table => new
                {
                    CriticalCallLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    LabOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CalledByName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CalledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriticalCallLogs", x => x.CriticalCallLogId);
                    table.ForeignKey(
                        name: "FK_CriticalCallLogs_LabOrderItems_LabOrderItemId",
                        column: x => x.LabOrderItemId,
                        principalTable: "LabOrderItems",
                        principalColumn: "LabOrderItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImagingProcedures",
                columns: table => new
                {
                    ImagingProcedureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ProcedureCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProcedureName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Modality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BodyPart = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TatHours = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagingProcedures", x => x.ImagingProcedureId);
                });

            migrationBuilder.CreateTable(
                name: "RadiologyOrders",
                columns: table => new
                {
                    RadiologyOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrderingDoctorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClinicalIndication = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadiologyOrders", x => x.RadiologyOrderId);
                    table.ForeignKey(
                        name: "FK_RadiologyOrders_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "BillId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadiologyOrders_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadiologyOrders_Users_OrderingDoctorUserId",
                        column: x => x.OrderingDoctorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RadiologyOrderItems",
                columns: table => new
                {
                    RadiologyOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RadiologyOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImagingProcedureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcedureName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Modality = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BodyPart = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TatHours = table.Column<int>(type: "int", nullable: false),
                    AccessionNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AcquiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Impression = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Recommendations = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReportingDoctorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PacsStudyUid = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PacsViewerUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadiologyOrderItems", x => x.RadiologyOrderItemId);
                    table.ForeignKey(
                        name: "FK_RadiologyOrderItems_ImagingProcedures_ImagingProcedureId",
                        column: x => x.ImagingProcedureId,
                        principalTable: "ImagingProcedures",
                        principalColumn: "ImagingProcedureId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RadiologyOrderItems_RadiologyOrders_RadiologyOrderId",
                        column: x => x.RadiologyOrderId,
                        principalTable: "RadiologyOrders",
                        principalColumn: "RadiologyOrderId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ImagingProcedures",
                columns: new[] { "ImagingProcedureId", "BodyPart", "Department", "IsActive", "Modality", "ProcedureCode", "ProcedureName", "TatHours" },
                values: new object[,]
                {
                    { new Guid("20000001-0000-0000-0000-000000000001"), "Chest", "Radiology", true, "XR", "XR-CHEST-PA", "X-Ray Chest PA", 2 },
                    { new Guid("20000001-0000-0000-0000-000000000002"), "Abdomen", "Radiology", true, "XR", "XR-ABDOMEN", "X-Ray Abdomen", 2 },
                    { new Guid("20000001-0000-0000-0000-000000000003"), "Abdomen", "Radiology", true, "US", "US-ABDOMEN", "Ultrasound Abdomen", 4 },
                    { new Guid("20000001-0000-0000-0000-000000000004"), "Pelvis", "Radiology", true, "US", "US-PELVIS", "Ultrasound Pelvis", 4 },
                    { new Guid("20000001-0000-0000-0000-000000000005"), "Head", "Radiology", true, "CT", "CT-HEAD", "CT Head", 6 },
                    { new Guid("20000001-0000-0000-0000-000000000006"), "Chest", "Radiology", true, "CT", "CT-CHEST", "CT Chest", 6 },
                    { new Guid("20000001-0000-0000-0000-000000000007"), "Abdomen/Pelvis", "Radiology", true, "CT", "CT-ABDO-PELV", "CT Abdomen & Pelvis", 8 },
                    { new Guid("20000001-0000-0000-0000-000000000008"), "Brain", "Radiology", true, "MRI", "MRI-BRAIN", "MRI Brain", 12 },
                    { new Guid("20000001-0000-0000-0000-000000000009"), "Spine", "Radiology", true, "MRI", "MRI-SPINE", "MRI Spine", 12 },
                    { new Guid("20000001-0000-0000-0000-000000000010"), "Breast", "Radiology", true, "MG", "MAMMO-BI", "Mammography Bilateral", 8 }
                });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000001"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000002"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000003"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000004"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000005"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000006"),
                column: "CriticalReferenceRange",
                value: "0.40-1.60");

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000007"),
                column: "CriticalReferenceRange",
                value: "1.60-3.10");

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000008"),
                column: "CriticalReferenceRange",
                value: "2.2-25.0");

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000009"),
                column: "CriticalReferenceRange",
                value: "2.2-25.0");

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000010"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000011"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000012"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000013"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000014"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000015"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000016"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000017"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000018"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000019"),
                column: "CriticalReferenceRange",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_CriticalCallLogs_LabOrderItemId",
                table: "CriticalCallLogs",
                column: "LabOrderItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CriticalCallLogs_TenantId_LabOrderItemId",
                table: "CriticalCallLogs",
                columns: new[] { "TenantId", "LabOrderItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImagingProcedures_ProcedureCode",
                table: "ImagingProcedures",
                column: "ProcedureCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrderItems_ImagingProcedureId",
                table: "RadiologyOrderItems",
                column: "ImagingProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrderItems_RadiologyOrderId",
                table: "RadiologyOrderItems",
                column: "RadiologyOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrderItems_TenantId_AccessionNumber",
                table: "RadiologyOrderItems",
                columns: new[] { "TenantId", "AccessionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrderItems_TenantId_RadiologyOrderId",
                table: "RadiologyOrderItems",
                columns: new[] { "TenantId", "RadiologyOrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_BillId",
                table: "RadiologyOrders",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_OrderingDoctorUserId",
                table: "RadiologyOrders",
                column: "OrderingDoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_PatientId",
                table: "RadiologyOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_TenantId_CreatedAt",
                table: "RadiologyOrders",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_TenantId_PatientId",
                table: "RadiologyOrders",
                columns: new[] { "TenantId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyOrders_TenantId_Status",
                table: "RadiologyOrders",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CriticalCallLogs");

            migrationBuilder.DropTable(
                name: "RadiologyOrderItems");

            migrationBuilder.DropTable(
                name: "ImagingProcedures");

            migrationBuilder.DropTable(
                name: "RadiologyOrders");

            migrationBuilder.DropColumn(
                name: "CriticalReferenceRange",
                table: "LabTestCatalog");

            migrationBuilder.DropColumn(
                name: "CriticalCallLogId",
                table: "LabOrderItems");

            migrationBuilder.DropColumn(
                name: "IsCritical",
                table: "LabOrderItems");
        }
    }
}
