using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutSessionTimes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateTime",
                table: "Workouts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateTime",
                table: "Workouts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDateTime",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "StartDateTime",
                table: "Workouts");
        }
    }
}
