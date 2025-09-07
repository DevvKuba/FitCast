using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeperateClientAndWorkoutData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Data");

            migrationBuilder.CreateTable(
                name: "Client",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CurrentBlockSession = table.Column<int>(type: "int", nullable: false),
                    TotalBlockSessions = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Client", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Workouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WorkoutTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrentBlockSession = table.Column<int>(type: "int", nullable: false),
                    TotalBlockSessions = table.Column<int>(type: "int", nullable: true),
                    ExerciseCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workouts_Client_ClientName",
                        column: x => x.ClientName,
                        principalTable: "Client",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_ClientName",
                table: "Workouts",
                column: "ClientName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Workouts");

            migrationBuilder.DropTable(
                name: "Client");

            migrationBuilder.CreateTable(
                name: "Data",
                columns: table => new
                {
                    Title = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CurrentBlockSession = table.Column<int>(type: "int", nullable: false),
                    ExerciseCount = table.Column<int>(type: "int", nullable: false),
                    SessionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalBlockSessions = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Data", x => x.Title);
                });
        }
    }
}
