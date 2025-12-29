using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvailabilityService.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemovedRecurrencePattern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleCalendarIntegrations");

            migrationBuilder.DropIndex(
                name: "IX_WorkingHours_ServiceId",
                table: "WorkingHours");

            migrationBuilder.DropIndex(
                name: "IX_TimeBlocks_ExternalEventId",
                table: "TimeBlocks");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "WorkingHours");

            migrationBuilder.DropColumn(
                name: "ExternalEventId",
                table: "TimeBlocks");

            migrationBuilder.DropColumn(
                name: "RecurrencePattern",
                table: "TimeBlocks");

            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceId",
                table: "TimeBlocks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.CreateIndex(
                name: "IX_TimeBlocks_RecurrenceId",
                table: "TimeBlocks",
                column: "RecurrenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimeBlocks_RecurrenceId",
                table: "TimeBlocks");

            migrationBuilder.DropColumn(
                name: "RecurrenceId",
                table: "TimeBlocks");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceId",
                table: "WorkingHours",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalEventId",
                table: "TimeBlocks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecurrencePattern",
                table: "TimeBlocks",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "GoogleCalendarIntegrations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    AutoSyncEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CalendarIdsToSync = table.Column<string[]>(type: "text[]", nullable: true),
                    ConnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DisconnectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisconnectionReason = table.Column<string>(type: "text", nullable: true),
                    GoogleCalendarId = table.Column<string>(type: "text", nullable: true),
                    GoogleUserEmail = table.Column<string>(type: "text", nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncError = table.Column<string>(type: "text", nullable: true),
                    LastSyncStatus = table.Column<int>(type: "integer", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SyncIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WebhookChannelId = table.Column<string>(type: "text", nullable: true),
                    WebhookEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WebhookExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookLastReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WebhookResourceId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleCalendarIntegrations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkingHours_ServiceId",
                table: "WorkingHours",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeBlocks_ExternalEventId",
                table: "TimeBlocks",
                column: "ExternalEventId");
        }
    }
}
