using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixAssignmentTechnicianFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<int>(type: "integer", nullable: false),
                    MaintenanceRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchScore = table.Column<double>(type: "double precision", nullable: false),
                    ReasoningSummary = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Recommended"),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assignments_MaintenanceRequests_MaintenanceRequestId",
                        column: x => x.MaintenanceRequestId,
                        principalTable: "MaintenanceRequests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Assignments_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_MaintenanceRequestId",
                table: "Assignments",
                column: "MaintenanceRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Assignments_TechnicianId",
                table: "Assignments",
                column: "TechnicianId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assignments");
        }
    }
}
