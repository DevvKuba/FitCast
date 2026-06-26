using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTodayPropertyWithinTrainerDailyRevenueEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionsToday",
                table: "TrainerDailyRevenues",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionsToday",
                table: "TrainerDailyRevenues");
        }
    }
}
