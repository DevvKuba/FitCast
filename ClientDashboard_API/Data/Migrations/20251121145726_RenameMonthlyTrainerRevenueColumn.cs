using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameMonthlyTrainerRevenueColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyTrainerRevenue_Trainers_TrainerId",
                table: "MonthlyTrainerRevenue");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MonthlyTrainerRevenue",
                table: "MonthlyTrainerRevenue");

            migrationBuilder.RenameTable(
                name: "MonthlyTrainerRevenue",
                newName: "MonthlyTrainerRevenues");

            migrationBuilder.RenameIndex(
                name: "IX_MonthlyTrainerRevenue_TrainerId",
                table: "MonthlyTrainerRevenues",
                newName: "IX_MonthlyTrainerRevenues_TrainerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MonthlyTrainerRevenues",
                table: "MonthlyTrainerRevenues",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyTrainerRevenues_Trainers_TrainerId",
                table: "MonthlyTrainerRevenues",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyTrainerRevenues_Trainers_TrainerId",
                table: "MonthlyTrainerRevenues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MonthlyTrainerRevenues",
                table: "MonthlyTrainerRevenues");

            migrationBuilder.RenameTable(
                name: "MonthlyTrainerRevenues",
                newName: "MonthlyTrainerRevenue");

            migrationBuilder.RenameIndex(
                name: "IX_MonthlyTrainerRevenues_TrainerId",
                table: "MonthlyTrainerRevenue",
                newName: "IX_MonthlyTrainerRevenue_TrainerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MonthlyTrainerRevenue",
                table: "MonthlyTrainerRevenue",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyTrainerRevenue_Trainers_TrainerId",
                table: "MonthlyTrainerRevenue",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
