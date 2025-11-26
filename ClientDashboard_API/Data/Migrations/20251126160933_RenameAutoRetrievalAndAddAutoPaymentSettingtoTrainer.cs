using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameAutoRetrievalAndAddAutoPaymentSettingtoTrainer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AutoRetrieval",
                table: "Trainers",
                newName: "AutoWorkoutRetrieval");

            migrationBuilder.AddColumn<bool>(
                name: "AutoPaymentSetting",
                table: "Trainers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPaymentSetting",
                table: "Trainers");

            migrationBuilder.RenameColumn(
                name: "AutoWorkoutRetrieval",
                table: "Trainers",
                newName: "AutoRetrieval");
        }
    }
}
