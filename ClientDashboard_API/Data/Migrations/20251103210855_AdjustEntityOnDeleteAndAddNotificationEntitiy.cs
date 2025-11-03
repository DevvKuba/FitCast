using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdjustEntityOnDeleteAndAddNotificationEntitiy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_Trainer_TrainerId",
                table: "Client");

            migrationBuilder.DropForeignKey(
                name: "FK_Workouts_Client_ClientId",
                table: "Workouts");

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "Workouts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainerId = table.Column<int>(type: "int", nullable: true),
                    ClientId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReminderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentThrough = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_Client_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Client",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notification_Trainer_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "Trainer",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notification_ClientId",
                table: "Notification",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_TrainerId",
                table: "Notification",
                column: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Client_Trainer_TrainerId",
                table: "Client",
                column: "TrainerId",
                principalTable: "Trainer",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Workouts_Client_ClientId",
                table: "Workouts",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Client_Trainer_TrainerId",
                table: "Client");

            migrationBuilder.DropForeignKey(
                name: "FK_Workouts_Client_ClientId",
                table: "Workouts");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.AlterColumn<int>(
                name: "ClientId",
                table: "Workouts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Client_Trainer_TrainerId",
                table: "Client",
                column: "TrainerId",
                principalTable: "Trainer",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Workouts_Client_ClientId",
                table: "Workouts",
                column: "ClientId",
                principalTable: "Client",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
