using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetCustomTrainerDailyRevenueClusteredIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainerDailyRevenues_TrainerId",
                table: "TrainerDailyRevenues");

            // SQL Server allows only one clustered index per table, and the PK on Id is
            // clustered by default. Drop it and recreate it as nonclustered so clustering
            // can be relocated to (TrainerId, AsOfDate) below.
            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainerDailyRevenues",
                table: "TrainerDailyRevenues");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainerDailyRevenues",
                table: "TrainerDailyRevenues",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerDailyRevenues_TrainerId_AsOfDate",
                table: "TrainerDailyRevenues",
                columns: new[] { "TrainerId", "AsOfDate" },
                unique: true)
                .Annotation("SqlServer:Clustered", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainerDailyRevenues_TrainerId_AsOfDate",
                table: "TrainerDailyRevenues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainerDailyRevenues",
                table: "TrainerDailyRevenues");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainerDailyRevenues",
                table: "TrainerDailyRevenues",
                column: "Id")
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainerDailyRevenues_TrainerId",
                table: "TrainerDailyRevenues",
                column: "TrainerId");
        }
    }
}
