using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInpatientBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdmissionId",
                table: "Bills",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InpatientCharges",
                columns: table => new
                {
                    InpatientChargeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChargeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InpatientCharges", x => x.InpatientChargeId);
                    table.ForeignKey(
                        name: "FK_InpatientCharges_Admissions_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "Admissions",
                        principalColumn: "AdmissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InpatientCharges_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_TenantId_AdmissionId",
                table: "Bills",
                columns: new[] { "TenantId", "AdmissionId" });

            migrationBuilder.CreateIndex(
                name: "IX_InpatientCharges_AdmissionId",
                table: "InpatientCharges",
                column: "AdmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_InpatientCharges_CreatedByUserId",
                table: "InpatientCharges",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InpatientCharges_TenantId_AdmissionId_ChargeDate",
                table: "InpatientCharges",
                columns: new[] { "TenantId", "AdmissionId", "ChargeDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InpatientCharges");

            migrationBuilder.DropIndex(
                name: "IX_Bills_TenantId_AdmissionId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "AdmissionId",
                table: "Bills");
        }
    }
}
