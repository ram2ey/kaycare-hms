using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditNotesAndRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CreditNoteTotal",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceDue",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                computedColumnSql: "[TotalAmount] + [AdjustmentTotal] - [DiscountAmount] - [WriteOffAmount] - [CreditNoteTotal] - [PaidAmount]",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldComputedColumnSql: "[TotalAmount] + [AdjustmentTotal] - [DiscountAmount] - [WriteOffAmount] - [PaidAmount]",
                oldStored: true);

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                columns: table => new
                {
                    CreditNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CreditNoteNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Draft"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.CreditNoteId);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "BillId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CreditNotes_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    RefundId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    RefundNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProcessedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RefundMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.RefundId);
                    table.ForeignKey(
                        name: "FK_Refunds_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "BillId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_CreditNotes_CreditNoteId",
                        column: x => x.CreditNoteId,
                        principalTable: "CreditNotes",
                        principalColumn: "CreditNoteId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Refunds_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "PatientId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Refunds_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_ApprovedByUserId",
                table: "CreditNotes",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_BillId",
                table: "CreditNotes",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CreatedByUserId",
                table: "CreditNotes",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_PatientId",
                table: "CreditNotes",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_BillId",
                table: "CreditNotes",
                columns: new[] { "TenantId", "BillId" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_CreditNoteNumber",
                table: "CreditNotes",
                columns: new[] { "TenantId", "CreditNoteNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_Status",
                table: "CreditNotes",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_BillId",
                table: "Refunds",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_CreatedByUserId",
                table: "Refunds",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_CreditNoteId",
                table: "Refunds",
                column: "CreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PatientId",
                table: "Refunds",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_ProcessedByUserId",
                table: "Refunds",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_TenantId_BillId",
                table: "Refunds",
                columns: new[] { "TenantId", "BillId" });

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_TenantId_RefundNumber",
                table: "Refunds",
                columns: new[] { "TenantId", "RefundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_TenantId_Status",
                table: "Refunds",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "CreditNotes");

            migrationBuilder.DropColumn(
                name: "CreditNoteTotal",
                table: "Bills");

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceDue",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                computedColumnSql: "[TotalAmount] + [AdjustmentTotal] - [DiscountAmount] - [WriteOffAmount] - [PaidAmount]",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldComputedColumnSql: "[TotalAmount] + [AdjustmentTotal] - [DiscountAmount] - [WriteOffAmount] - [CreditNoteTotal] - [PaidAmount]",
                oldStored: true);
        }
    }
}
