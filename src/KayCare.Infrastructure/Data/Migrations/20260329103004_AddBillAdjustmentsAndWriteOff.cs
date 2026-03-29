using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillAdjustmentsAndWriteOff : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AdjustmentTotal",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WriteOffAmount",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "WriteOffReason",
                table: "Bills",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceDue",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                computedColumnSql: "[TotalAmount] + [AdjustmentTotal] - [DiscountAmount] - [WriteOffAmount] - [PaidAmount]",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldComputedColumnSql: "[TotalAmount] - [DiscountAmount] - [PaidAmount]",
                oldStored: true);

            migrationBuilder.CreateTable(
                name: "BillAdjustments",
                columns: table => new
                {
                    BillAdjustmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    BillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdjustedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdjustedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillAdjustments", x => x.BillAdjustmentId);
                    table.ForeignKey(
                        name: "FK_BillAdjustments_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "BillId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillAdjustments_Users_AdjustedByUserId",
                        column: x => x.AdjustedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillAdjustments_AdjustedByUserId",
                table: "BillAdjustments",
                column: "AdjustedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BillAdjustments_BillId",
                table: "BillAdjustments",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_BillAdjustments_TenantId_BillId",
                table: "BillAdjustments",
                columns: new[] { "TenantId", "BillId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillAdjustments");

            migrationBuilder.DropColumn(
                name: "AdjustmentTotal",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "WriteOffAmount",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "WriteOffReason",
                table: "Bills");

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceDue",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                computedColumnSql: "[TotalAmount] - [DiscountAmount] - [PaidAmount]",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldComputedColumnSql: "[TotalAmount] + [AdjustmentTotal] - [DiscountAmount] - [WriteOffAmount] - [PaidAmount]",
                oldStored: true);
        }
    }
}
