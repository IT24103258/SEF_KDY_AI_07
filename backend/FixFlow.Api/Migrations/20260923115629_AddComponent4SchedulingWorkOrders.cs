using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddComponent4SchedulingWorkOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessHours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    DayName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CloseTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    IsWorkingDay = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessHours", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrder",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScheduledStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduledEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    ActualStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SLADeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovalComments = table.Column<string>(type: "text", nullable: false),
                    ConflictDetected = table.Column<bool>(type: "boolean", nullable: false),
                    ConflictDetailsJson = table.Column<string>(type: "text", nullable: false),
                    AiDecisionSummary = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrder_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkOrder_MaintenanceRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "MaintenanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrder_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkOrder_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CompletionEvidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    Caption = table.Column<string>(type: "text", nullable: false),
                    UploadedById = table.Column<Guid>(type: "uuid", nullable: false),
                    SignatureDataUrl = table.Column<string>(type: "text", nullable: false),
                    SignerName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompletionEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompletionEvidence_Users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompletionEvidence_WorkOrder_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleProposal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicianId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProposedEndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SlaDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConflictDetected = table.Column<bool>(type: "boolean", nullable: false),
                    ConflictDetailsJson = table.Column<string>(type: "text", nullable: false),
                    DecisionSummary = table.Column<string>(type: "text", nullable: false),
                    ValidationDetailsJson = table.Column<string>(type: "text", nullable: false),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleProposal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleProposal_MaintenanceRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "MaintenanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleProposal_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleProposal_WorkOrder_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkNote",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteText = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkNote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkNote_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkNote_WorkOrder_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrderStatusHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangedById = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkOrderStatusHistory_Users_ChangedById",
                        column: x => x.ChangedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_WorkOrderStatusHistory_WorkOrder_WorkOrderId",
                        column: x => x.WorkOrderId,
                        principalTable: "WorkOrder",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessHours_DayOfWeek",
                table: "BusinessHours",
                column: "DayOfWeek",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompletionEvidence_UploadedById",
                table: "CompletionEvidence",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_CompletionEvidence_WorkOrderId",
                table: "CompletionEvidence",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleProposal_ProposedStartTime",
                table: "ScheduleProposal",
                column: "ProposedStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleProposal_RequestId",
                table: "ScheduleProposal",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleProposal_TechnicianId",
                table: "ScheduleProposal",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleProposal_WorkOrderId",
                table: "ScheduleProposal",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkNote_AuthorId",
                table: "WorkNote",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkNote_WorkOrderId",
                table: "WorkNote",
                column: "WorkOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_ApprovedById",
                table: "WorkOrder",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_LocationId",
                table: "WorkOrder",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_Priority",
                table: "WorkOrder",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_RequestId",
                table: "WorkOrder",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_ScheduledEndTime",
                table: "WorkOrder",
                column: "ScheduledEndTime");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_ScheduledStartTime",
                table: "WorkOrder",
                column: "ScheduledStartTime");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_Status",
                table: "WorkOrder",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_TechnicianId",
                table: "WorkOrder",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_TechnicianId_ScheduledStartTime_ScheduledEndTime",
                table: "WorkOrder",
                columns: new[] { "TechnicianId", "ScheduledStartTime", "ScheduledEndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrder_WorkOrderNumber",
                table: "WorkOrder",
                column: "WorkOrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderStatusHistory_ChangedById",
                table: "WorkOrderStatusHistory",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderStatusHistory_WorkOrderId",
                table: "WorkOrderStatusHistory",
                column: "WorkOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessHours");

            migrationBuilder.DropTable(
                name: "CompletionEvidence");

            migrationBuilder.DropTable(
                name: "ScheduleProposal");

            migrationBuilder.DropTable(
                name: "WorkNote");

            migrationBuilder.DropTable(
                name: "WorkOrderStatusHistory");

            migrationBuilder.DropTable(
                name: "WorkOrder");
        }
    }
}
