using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediCloud.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class LabReferenceRangesAndReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultReferenceRange",
                table: "LabTestCatalog",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultUnit",
                table: "LabTestCatalog",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualResultFlag",
                table: "LabOrderItems",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualResultReferenceRange",
                table: "LabOrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualResultUnit",
                table: "LabOrderItems",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000001"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000002"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { "0-20", "mm/hr" });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000003"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000004"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000005"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000006"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { "0.70-1.10", "mmol/L" });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000007"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { "2.10-2.55", "mmol/L" });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000008"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { "3.9-5.6", "mmol/L" });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000009"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { "3.9-7.8", "mmol/L" });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000010"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000011"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000012"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { "197-866", "pg/mL" });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000013"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { "4.6-18.7", "ng/mL" });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000014"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { "4.0-5.6", "%" });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000015"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000016"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000017"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000018"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "LabTestCatalog",
                keyColumn: "LabTestCatalogId",
                keyValue: new Guid("10000001-0000-0000-0000-000000000019"),
                columns: new[] { "DefaultReferenceRange", "DefaultUnit" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultReferenceRange",
                table: "LabTestCatalog");

            migrationBuilder.DropColumn(
                name: "DefaultUnit",
                table: "LabTestCatalog");

            migrationBuilder.DropColumn(
                name: "ManualResultFlag",
                table: "LabOrderItems");

            migrationBuilder.DropColumn(
                name: "ManualResultReferenceRange",
                table: "LabOrderItems");

            migrationBuilder.DropColumn(
                name: "ManualResultUnit",
                table: "LabOrderItems");
        }
    }
}
