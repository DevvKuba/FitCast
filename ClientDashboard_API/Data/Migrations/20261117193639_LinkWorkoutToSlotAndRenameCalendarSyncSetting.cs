using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkWorkoutToSlotAndRenameCalendarSyncSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AutoWorkoutRetrieval",
                table: "Trainers",
                newName: "CalendarSyncEnabled");

            // calendar sync is on by default - existing trainers had the old Hevy setting off, so switch them on
            migrationBuilder.Sql("UPDATE Trainers SET CalendarSyncEnabled = 1");

            // clean start for the new nightly sync: sessions that already finished are marked Completed (1),
            // so the first run doesn't count them in bulk as workouts. Scheduled = 0.
            migrationBuilder.Sql("UPDATE BookedSessionsSlots SET Status = 1 WHERE Status = 0 AND EndDateTime < SYSUTCDATETIME()");

            migrationBuilder.AddColumn<int>(
                name: "BookedSessionSlotId",
                table: "Workouts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workouts_BookedSessionSlotId",
                table: "Workouts",
                column: "BookedSessionSlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workouts_BookedSessionsSlots_BookedSessionSlotId",
                table: "Workouts",
                column: "BookedSessionSlotId",
                principalTable: "BookedSessionsSlots",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // the two data updates in Up aren't reversed: the old setting's values and which slots were Scheduled are not kept

            migrationBuilder.DropForeignKey(
                name: "FK_Workouts_BookedSessionsSlots_BookedSessionSlotId",
                table: "Workouts");

            migrationBuilder.DropIndex(
                name: "IX_Workouts_BookedSessionSlotId",
                table: "Workouts");

            migrationBuilder.DropColumn(
                name: "BookedSessionSlotId",
                table: "Workouts");

            migrationBuilder.RenameColumn(
                name: "CalendarSyncEnabled",
                table: "Trainers",
                newName: "AutoWorkoutRetrieval");
        }
    }
}
