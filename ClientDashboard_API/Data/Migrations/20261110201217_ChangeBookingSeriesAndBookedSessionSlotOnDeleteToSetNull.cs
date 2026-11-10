using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class ChangeBookingSeriesAndBookedSessionSlotOnDeleteToSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookedSessionsSlots_BookingSeries_BookingSeriesId",
                table: "BookedSessionsSlots");

            migrationBuilder.AddForeignKey(
                name: "FK_BookedSessionsSlots_BookingSeries_BookingSeriesId",
                table: "BookedSessionsSlots",
                column: "BookingSeriesId",
                principalTable: "BookingSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookedSessionsSlots_BookingSeries_BookingSeriesId",
                table: "BookedSessionsSlots");

            migrationBuilder.AddForeignKey(
                name: "FK_BookedSessionsSlots_BookingSeries_BookingSeriesId",
                table: "BookedSessionsSlots",
                column: "BookingSeriesId",
                principalTable: "BookingSeries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
