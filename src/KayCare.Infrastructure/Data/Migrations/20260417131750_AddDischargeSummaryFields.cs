using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCare.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDischargeSummaryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttendingPhysicianNotes",
                table: "Admissions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DischargeCondition",
                table: "Admissions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DischargeMedications",
                table: "Admissions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalDiagnosis",
                table: "Admissions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUpInstructions",
                table: "Admissions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProceduresPerformed",
                table: "Admissions",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreatmentSummary",
                table: "Admissions",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendingPhysicianNotes",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "DischargeCondition",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "DischargeMedications",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "FinalDiagnosis",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "FollowUpInstructions",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "ProceduresPerformed",
                table: "Admissions");

            migrationBuilder.DropColumn(
                name: "TreatmentSummary",
                table: "Admissions");
        }
    }
}
