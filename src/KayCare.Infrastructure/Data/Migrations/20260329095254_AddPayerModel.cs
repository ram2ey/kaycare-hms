using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NhisNumber",
                table: "Patients",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountReason",
                table: "Bills",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PayerId",
                table: "Bills",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceDue",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                computedColumnSql: "[TotalAmount] - [DiscountAmount] - [PaidAmount]",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldComputedColumnSql: "[TotalAmount] - [PaidAmount]",
                oldStored: true);

            migrationBuilder.CreateTable(
                name: "Payers",
                columns: table => new
                {
                    PayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payers", x => x.PayerId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_PayerId",
                table: "Bills",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Payers_TenantId_Name",
                table: "Payers",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payers_TenantId_Type",
                table: "Payers",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Payers_PayerId",
                table: "Bills",
                column: "PayerId",
                principalTable: "Payers",
                principalColumn: "PayerId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Payers_PayerId",
                table: "Bills");

            migrationBuilder.DropTable(
                name: "Payers");

            migrationBuilder.DropIndex(
                name: "IX_Bills_PayerId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "NhisNumber",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "DiscountReason",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "PayerId",
                table: "Bills");

            migrationBuilder.AlterColumn<decimal>(
                name: "BalanceDue",
                table: "Bills",
                type: "decimal(12,2)",
                nullable: false,
                computedColumnSql: "[TotalAmount] - [PaidAmount]",
                stored: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,2)",
                oldComputedColumnSql: "[TotalAmount] - [DiscountAmount] - [PaidAmount]",
                oldStored: true);
        }
    }
}
