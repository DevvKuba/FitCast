using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackFillTrainerDailyRevenueSessionsTodayProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Reconstruct SessionsToday for rows created before the column existed
            // (they default to 0). Sessions = RevenueToday / AverageSessionPrice,
            // matching how the value is otherwise derived. Only touch un-backfilled
            // rows so any already-correct records from the daily job are left intact.
            migrationBuilder.Sql(@"
                UPDATE TrainerDailyRevenues
                SET SessionsToday = CAST(ROUND(RevenueToday / AverageSessionPrice, 0) AS int)
                WHERE SessionsToday = 0
                  AND AverageSessionPrice > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data backfill; the original per-row values cannot be distinguished
            // from legitimately recalculated ones, so there is nothing safe to revert.
        }
    }
}
