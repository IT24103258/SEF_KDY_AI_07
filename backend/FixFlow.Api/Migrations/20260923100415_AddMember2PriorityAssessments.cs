using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMember2PriorityAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriorityAssessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetCriticality = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ImpactLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LikelihoodLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RiskScore = table.Column<int>(type: "integer", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecommendedResponseWindow = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResponseTimeHours = table.Column<int>(type: "integer", nullable: false),
                    ResolutionTimeHours = table.Column<int>(type: "integer", nullable: false),
                    EscalationFlag = table.Column<bool>(type: "boolean", nullable: false),
                    EscalationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Explanation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ContributingFactorsJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AssessedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriorityAssessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriorityAssessment_MaintenanceRequests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "MaintenanceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriorityAssessment_RequestId",
                table: "PriorityAssessment",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriorityAssessment");
        }
    }
}
