using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimRacingSchedule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramUserSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramChatId = table.Column<long>(type: "bigint", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotificationMinutesBefore = table.Column<int>(type: "integer", nullable: false, defaultValue: 60),
                    NotifyShiftStart = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyShiftExchange = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyShiftReminder = table.Column<bool>(type: "boolean", nullable: false),
                    TimeZone = table.Column<string>(type: "text", nullable: true, defaultValue: "Europe/Moscow"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramUserSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUserSettings_EmployeeId",
                table: "TelegramUserSettings",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUserSettings_TelegramChatId",
                table: "TelegramUserSettings",
                column: "TelegramChatId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramUserSettings");
        }
    }
}
