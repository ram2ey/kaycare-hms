using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Admissions",
                columns: table => new
                {
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    AdmissionNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmittingDoctorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedDischargeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDischargeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdmissionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DiagnosisOnAdmission = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DischargeNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DischargeType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admissions", x => x.AdmissionId);
                    table.ForeignKey(
                        name: "FK_Admissions_Beds_BedId",
                        column: x => x.BedId,
                        principalTable: "Beds",
                        principalColumn: "BedId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Admissions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Admissions_Users_AdmittingDoctorUserId",
                        column: x => x.AdmittingDoctorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Admissions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Admissions_Wards_WardId",
                        column: x => x.WardId,
                        principalTable: "Wards",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdmissionTransfers",
                columns: table => new
                {
                    AdmissionTransferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromBedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromWardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToBedId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ToWardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TransferredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdmissionTransfers", x => x.AdmissionTransferId);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "AdmissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Beds_FromBedId",
                        column: x => x.FromBedId,
                        principalTable: "Beds",
                        principalColumn: "BedId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Beds_ToBedId",
                        column: x => x.ToBedId,
                        principalTable: "Beds",
                        principalColumn: "BedId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Users_TransferredByUserId",
                        column: x => x.TransferredByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Wards_FromWardId",
                        column: x => x.FromWardId,
                        principalTable: "Wards",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdmissionTransfers_Wards_ToWardId",
                        column: x => x.ToWardId,
                        principalTable: "Wards",
                        principalColumn: "WardId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_AdmittingDoctorUserId",
                table: "Admissions",
                column: "AdmittingDoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_BedId",
                table: "Admissions",
                column: "BedId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_CreatedByUserId",
                table: "Admissions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_PatientId",
                table: "Admissions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_TenantId_AdmissionNumber",
                table: "Admissions",
                columns: new[] { "TenantId", "AdmissionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_TenantId_BedId_Status",
                table: "Admissions",
                columns: new[] { "TenantId", "BedId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_TenantId_PatientId_Status",
                table: "Admissions",
                columns: new[] { "TenantId", "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Admissions_WardId",
                table: "Admissions",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_AdmissionId",
                table: "AdmissionTransfers",
                column: "AdmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_FromBedId",
                table: "AdmissionTransfers",
                column: "FromBedId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_FromWardId",
                table: "AdmissionTransfers",
                column: "FromWardId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_ToBedId",
                table: "AdmissionTransfers",
                column: "ToBedId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_ToWardId",
                table: "AdmissionTransfers",
                column: "ToWardId");

            migrationBuilder.CreateIndex(
                name: "IX_AdmissionTransfers_TransferredByUserId",
                table: "AdmissionTransfers",
                column: "TransferredByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdmissionTransfers");

            migrationBuilder.DropTable(
                name: "Admissions");
        }
    }
}
