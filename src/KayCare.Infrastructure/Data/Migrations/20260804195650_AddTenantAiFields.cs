using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiMonthlyQuota",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.AddColumn<DateTime>(
                name: "AiQuotaResetDate",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "AiRequestsThisMonth",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AllowedAiTiers",
                table: "Tenants",
                type: "text",
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<string>(
                name: "CustomOpenRouterKey",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAiEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiMonthlyQuota",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AiQuotaResetDate",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AiRequestsThisMonth",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "AllowedAiTiers",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CustomOpenRouterKey",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "IsAiEnabled",
                table: "Tenants");
        }
    }
}
