using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHevyExerciseCountAndClientWeightColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExerciseCount",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "ExcludedNames",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "WorkoutRetrievalApiKey",
                table: "Trainers");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Clients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExerciseCount",
                table: "Workouts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExcludedNames",
                table: "Trainers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkoutRetrievalApiKey",
                table: "Trainers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "Clients",
                type: "float",
                nullable: true);
        }
    }
}
