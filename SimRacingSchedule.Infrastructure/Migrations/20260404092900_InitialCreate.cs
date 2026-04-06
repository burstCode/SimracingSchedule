using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimRacingSchedule.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid"),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100),
                    Patronymic = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20),
                    Position = table.Column<string>(type: "character varying(100)", maxLength: 100),
                    Role = table.Column<int>(type: "integer"),
                    IsActive = table.Column<bool>(type: "boolean", defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Employees", x => x.Id);
                });

            _ = migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid"),
                    EmployeeId = table.Column<Guid>(type: "uuid"),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone"),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone"),
                    Type = table.Column<int>(type: "integer"),
                    Status = table.Column<int>(type: "integer", defaultValue: 0),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Shifts", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_Shifts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            _ = migrationBuilder.CreateTable(
                name: "ShiftExchangeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid"),
                    RequesterId = table.Column<Guid>(type: "uuid"),
                    TargetId = table.Column<Guid>(type: "uuid"),
                    RequesterShiftId = table.Column<Guid>(type: "uuid"),
                    TargetShiftId = table.Column<Guid>(type: "uuid"),
                    Status = table.Column<int>(type: "integer", defaultValue: 0),
                    RequestMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResponseMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone"),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_ShiftExchangeRequests", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_ShiftExchangeRequests_Employees_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    _ = table.ForeignKey(
                        name: "FK_ShiftExchangeRequests_Employees_TargetId",
                        column: x => x.TargetId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    _ = table.ForeignKey(
                        name: "FK_ShiftExchangeRequests_Shifts_RequesterShiftId",
                        column: x => x.RequesterShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    _ = table.ForeignKey(
                        name: "FK_ShiftExchangeRequests_Shifts_TargetShiftId",
                        column: x => x.TargetShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            _ = migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "IX_Employees_PhoneNumber",
                table: "Employees",
                column: "PhoneNumber",
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "IX_ShiftExchangeRequests_RequesterId_TargetId_Status",
                table: "ShiftExchangeRequests",
                columns: new[] { "RequesterId", "TargetId", "Status" });

            _ = migrationBuilder.CreateIndex(
                name: "IX_ShiftExchangeRequests_RequesterShiftId",
                table: "ShiftExchangeRequests",
                column: "RequesterShiftId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_ShiftExchangeRequests_Status",
                table: "ShiftExchangeRequests",
                column: "Status");

            _ = migrationBuilder.CreateIndex(
                name: "IX_ShiftExchangeRequests_TargetId",
                table: "ShiftExchangeRequests",
                column: "TargetId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_ShiftExchangeRequests_TargetShiftId",
                table: "ShiftExchangeRequests",
                column: "TargetShiftId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_Shifts_EmployeeId_StartTime",
                table: "Shifts",
                columns: new[] { "EmployeeId", "StartTime" });

            _ = migrationBuilder.CreateIndex(
                name: "IX_Shifts_Status",
                table: "Shifts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "ShiftExchangeRequests");

            _ = migrationBuilder.DropTable(
                name: "Shifts");

            _ = migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
