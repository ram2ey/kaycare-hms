using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFullyDispensed",
                table: "PrescriptionItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "QuantityDispensed",
                table: "PrescriptionItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DispenseEvents",
                columns: table => new
                {
                    DispenseEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispensedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispensedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispenseEvents", x => x.DispenseEventId);
                    table.ForeignKey(
                        name: "FK_DispenseEvents_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "PrescriptionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispenseEvents_Users_DispensedByUserId",
                        column: x => x.DispensedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionTemplates",
                columns: table => new
                {
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsShared = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionTemplates", x => x.TemplateId);
                    table.ForeignKey(
                        name: "FK_PrescriptionTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DispenseEventItems",
                columns: table => new
                {
                    DispenseEventItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispenseEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrescriptionItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuantityDispensed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispenseEventItems", x => x.DispenseEventItemId);
                    table.ForeignKey(
                        name: "FK_DispenseEventItems_DispenseEvents_DispenseEventId",
                        column: x => x.DispenseEventId,
                        principalTable: "DispenseEvents",
                        principalColumn: "DispenseEventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispenseEventItems_PrescriptionItems_PrescriptionItemId",
                        column: x => x.PrescriptionItemId,
                        principalTable: "PrescriptionItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionTemplateItems",
                columns: table => new
                {
                    TemplateItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DosageForm = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Refills = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsControlledSubstance = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionTemplateItems", x => x.TemplateItemId);
                    table.ForeignKey(
                        name: "FK_PrescriptionTemplateItems_PrescriptionTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "PrescriptionTemplates",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DispenseEventItems_DispenseEventId",
                table: "DispenseEventItems",
                column: "DispenseEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DispenseEventItems_PrescriptionItemId",
                table: "DispenseEventItems",
                column: "PrescriptionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_DispenseEvents_DispensedByUserId",
                table: "DispenseEvents",
                column: "DispensedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispenseEvents_PrescriptionId",
                table: "DispenseEvents",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_DispenseEvents_TenantId_PrescriptionId",
                table: "DispenseEvents",
                columns: new[] { "TenantId", "PrescriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplateItems_TemplateId",
                table: "PrescriptionTemplateItems",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplates_CreatedByUserId",
                table: "PrescriptionTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplates_TenantId_CreatedByUserId",
                table: "PrescriptionTemplates",
                columns: new[] { "TenantId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionTemplates_TenantId_IsShared",
                table: "PrescriptionTemplates",
                columns: new[] { "TenantId", "IsShared" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DispenseEventItems");

            migrationBuilder.DropTable(
                name: "PrescriptionTemplateItems");

            migrationBuilder.DropTable(
                name: "DispenseEvents");

            migrationBuilder.DropTable(
                name: "PrescriptionTemplates");

            migrationBuilder.DropColumn(
                name: "IsFullyDispensed",
                table: "PrescriptionItems");

            migrationBuilder.DropColumn(
                name: "QuantityDispensed",
                table: "PrescriptionItems");
        }
    }
}
