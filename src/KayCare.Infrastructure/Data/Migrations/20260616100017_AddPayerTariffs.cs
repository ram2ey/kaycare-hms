using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPayerTariffs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayerTariffs",
                columns: table => new
                {
                    PayerTariffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCatalogItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TariffCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TariffPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayerTariffs", x => x.PayerTariffId);
                    table.ForeignKey(
                        name: "FK_PayerTariffs_Payers_PayerId",
                        column: x => x.PayerId,
                        principalTable: "Payers",
                        principalColumn: "PayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayerTariffs_ServiceCatalogItems_ServiceCatalogItemId",
                        column: x => x.ServiceCatalogItemId,
                        principalTable: "ServiceCatalogItems",
                        principalColumn: "ServiceCatalogItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayerTariffs_PayerId",
                table: "PayerTariffs",
                column: "PayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PayerTariffs_ServiceCatalogItemId",
                table: "PayerTariffs",
                column: "ServiceCatalogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PayerTariffs_TenantId_PayerId_ServiceCatalogItemId",
                table: "PayerTariffs",
                columns: new[] { "TenantId", "PayerId", "ServiceCatalogItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayerTariffs");
        }
    }
}
