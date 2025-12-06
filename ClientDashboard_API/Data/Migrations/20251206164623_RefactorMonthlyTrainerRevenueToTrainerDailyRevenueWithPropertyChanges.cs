using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorMonthlyTrainerRevenueToTrainerDailyRevenueWithPropertyChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyTrainerRevenues");

            migrationBuilder.CreateTable(
                name: "TrainerDailyRevenues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<int>(type: "int", nullable: false),
                    MonthlyRevenueThusFar = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalSessionsThisMonth = table.Column<int>(type: "int", nullable: false),
                    ActiveClients = table.Column<int>(type: "int", nullable: false),
                    NewClientsThisMonth = table.Column<int>(type: "int", nullable: false),
                    AverageSessionPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AsOfDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainerDailyRevenues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainerDailyRevenues_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainerDailyRevenues_TrainerId",
                table: "TrainerDailyRevenues",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainerDailyRevenues");

            migrationBuilder.CreateTable(
                name: "MonthlyTrainerRevenues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<int>(type: "int", nullable: false),
                    ActiveClients = table.Column<int>(type: "int", nullable: false),
                    AverageSessionPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MonthlyRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewClients = table.Column<int>(type: "int", nullable: false),
                    TotalSessions = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyTrainerRevenues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyTrainerRevenues_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyTrainerRevenues_TrainerId",
                table: "MonthlyTrainerRevenues",
                column: "TrainerId");
        }
    }
}
