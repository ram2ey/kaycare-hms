using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNursingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicationAdministrations",
                columns: table => new
                {
                    MedicationAdministrationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AdministeredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdministeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoseGiven = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationAdministrations", x => x.MedicationAdministrationId);
                    table.ForeignKey(
                        name: "FK_MedicationAdministrations_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "AdmissionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationAdministrations_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationAdministrations_PrescriptionItems_PrescriptionItemId",
                        column: x => x.PrescriptionItemId,
                        principalTable: "PrescriptionItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationAdministrations_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "PrescriptionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MedicationAdministrations_Users_AdministeredByUserId",
                        column: x => x.AdministeredByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NursingNotes",
                columns: table => new
                {
                    NursingNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NursingNotes", x => x.NursingNoteId);
                    table.ForeignKey(
                        name: "FK_NursingNotes_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "AdmissionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NursingNotes_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NursingNotes_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VitalSigns",
                columns: table => new
                {
                    VitalSignsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConsultationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BloodPressureSystolic = table.Column<int>(type: "int", nullable: true),
                    BloodPressureDiastolic = table.Column<int>(type: "int", nullable: true),
                    PulseRate = table.Column<int>(type: "int", nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SpO2 = table.Column<int>(type: "int", nullable: true),
                    RespiratoryRate = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSigns", x => x.VitalSignsId);
                    table.ForeignKey(
                        name: "FK_VitalSigns_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "AdmissionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VitalSigns_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VitalSigns_Users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrations_AdministeredByUserId",
                table: "MedicationAdministrations",
                column: "AdministeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrations_AdmissionId",
                table: "MedicationAdministrations",
                column: "AdmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrations_PatientId",
                table: "MedicationAdministrations",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrations_PrescriptionId",
                table: "MedicationAdministrations",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrations_PrescriptionItemId",
                table: "MedicationAdministrations",
                column: "PrescriptionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrations_TenantId_AdmissionId",
                table: "MedicationAdministrations",
                columns: new[] { "TenantId", "AdmissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrations_TenantId_PatientId_AdministeredAt",
                table: "MedicationAdministrations",
                columns: new[] { "TenantId", "PatientId", "AdministeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrations_TenantId_PrescriptionItemId",
                table: "MedicationAdministrations",
                columns: new[] { "TenantId", "PrescriptionItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_NursingNotes_AdmissionId",
                table: "NursingNotes",
                column: "AdmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingNotes_AuthorId",
                table: "NursingNotes",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingNotes_PatientId",
                table: "NursingNotes",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_NursingNotes_TenantId_AdmissionId",
                table: "NursingNotes",
                columns: new[] { "TenantId", "AdmissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_NursingNotes_TenantId_PatientId_CreatedAt",
                table: "NursingNotes",
                columns: new[] { "TenantId", "PatientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_AdmissionId",
                table: "VitalSigns",
                column: "AdmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_PatientId",
                table: "VitalSigns",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_RecordedByUserId",
                table: "VitalSigns",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_TenantId_AdmissionId",
                table: "VitalSigns",
                columns: new[] { "TenantId", "AdmissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_VitalSigns_TenantId_PatientId_RecordedAt",
                table: "VitalSigns",
                columns: new[] { "TenantId", "PatientId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicationAdministrations");

            migrationBuilder.DropTable(
                name: "NursingNotes");

            migrationBuilder.DropTable(
                name: "VitalSigns");
        }
    }
}
