using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientDashboard_API.Data.Migrations
{
    /// <inheritdoc />
    public partial class BackfillNotificationRecipientStatusesAsRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO NotificationRecipientStatuses (UserId, NotificationId, IsRead, ReadAt)
                SELECT n.TrainerId, n.Id, 1, n.SentAt
                FROM Notifications n
                WHERE n.TrainerId IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM NotificationRecipientStatuses s
                      WHERE s.NotificationId = n.Id
                        AND s.UserId = n.TrainerId
                  );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO NotificationRecipientStatuses (UserId, NotificationId, IsRead, ReadAt)
                SELECT n.ClientId, n.Id, 1, n.SentAt
                FROM Notifications n
                WHERE n.ClientId IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM NotificationRecipientStatuses s
                      WHERE s.NotificationId = n.Id
                        AND s.UserId = n.ClientId
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
