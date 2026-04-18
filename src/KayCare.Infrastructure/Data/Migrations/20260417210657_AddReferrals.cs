using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReferrals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    ReferralId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ReferralNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsultationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReferringDoctorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferredToDoctorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReferredToDepartment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReferralType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExternalFacility = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Urgency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResponseNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.ReferralId);
                    table.ForeignKey(
                        name: "FK_Referrals_Consultations_ConsultationId",
                        column: x => x.ConsultationId,
                        principalTable: "Consultations",
                        principalColumn: "ConsultationId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Referrals_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ReferredToDoctorUserId",
                        column: x => x.ReferredToDoctorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Referrals_Users_ReferringDoctorUserId",
                        column: x => x.ReferringDoctorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ConsultationId",
                table: "Referrals",
                column: "ConsultationId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_PatientId",
                table: "Referrals",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferredToDoctorUserId",
                table: "Referrals",
                column: "ReferredToDoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferringDoctorUserId",
                table: "Referrals",
                column: "ReferringDoctorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_TenantId_PatientId_Status",
                table: "Referrals",
                columns: new[] { "TenantId", "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_TenantId_ReferralNumber",
                table: "Referrals",
                columns: new[] { "TenantId", "ReferralNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_TenantId_ReferredToDoctorUserId_Status",
                table: "Referrals",
                columns: new[] { "TenantId", "ReferredToDoctorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_TenantId_ReferringDoctorUserId",
                table: "Referrals",
                columns: new[] { "TenantId", "ReferringDoctorUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Referrals");
        }
    }
}
