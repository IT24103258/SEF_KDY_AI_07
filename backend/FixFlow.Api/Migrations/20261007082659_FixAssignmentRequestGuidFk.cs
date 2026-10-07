using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixAssignmentRequestGuidFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "Assignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestId",
                table: "Assignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
