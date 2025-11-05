using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingDataEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "TrainerId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ClientChurnLabels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    AsOfDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ChurnedByDate = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientChurnLabels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientChurnLabels_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientDailyFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    AsOfDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SessionsIn7d = table.Column<int>(type: "int", nullable: false),
                    SessionsIn28d = table.Column<int>(type: "int", nullable: false),
                    DaysSinceLastSession = table.Column<int>(type: "int", nullable: false),
                    RemainingSessions = table.Column<int>(type: "int", nullable: false),
                    DailySteps = table.Column<int>(type: "int", nullable: false),
                    AverageSessionDuration = table.Column<double>(type: "float", nullable: false),
                    LifeTimeValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentlyActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientDailyFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientDailyFeatures_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyTrainerRevenue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<int>(type: "int", nullable: false),
                    MonthlyRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalSessions = table.Column<int>(type: "int", nullable: false),
                    ActiveClients = table.Column<int>(type: "int", nullable: false),
                    NewClients = table.Column<int>(type: "int", nullable: false),
                    AverageSessionPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyTrainerRevenue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyTrainerRevenue_Trainers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientChurnLabels_ClientId",
                table: "ClientChurnLabels",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientDailyFeatures_ClientId",
                table: "ClientDailyFeatures",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyTrainerRevenue_TrainerId",
                table: "MonthlyTrainerRevenue",
                column: "TrainerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientChurnLabels");

            migrationBuilder.DropTable(
                name: "ClientDailyFeatures");

            migrationBuilder.DropTable(
                name: "MonthlyTrainerRevenue");

            migrationBuilder.AlterColumn<int>(
                name: "TrainerId",
                table: "Payments",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
