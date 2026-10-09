using FixFlow.Api.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FixFlow.Api.Migrations
{
    /// <summary>
    /// Repair migration: adds the missing "RequestedStartTime" and "RequestedEndTime"
    /// columns to the "ScheduleProposal" table.
    ///
    /// These properties exist on the ScheduleProposal entity and in the EF Core model
    /// snapshot, but migration 20261004191803_AddRequestedTimesToScheduleProposal was
    /// committed with empty Up()/Down() methods, so the columns were never created in
    /// the database. This caused PostgreSQL error 42703 (column does not exist) on
    /// every query materializing ScheduleProposal (e.g. GET /api/work-orders).
    ///
    /// The SQL is idempotent (ADD COLUMN IF NOT EXISTS) so this migration is safe to
    /// run on databases where the columns already exist. Because the model snapshot
    /// already contains these properties, this migration is hand-written — automatic
    /// scaffolding would detect no model diff and produce an empty migration.
    /// </summary>
    [DbContext(typeof(FixFlowDbContext))]
    [Migration("20261009103000_AddMissingRequestedTimeColumnsToScheduleProposal")]
    public partial class AddMissingRequestedTimeColumnsToScheduleProposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The default matches the DateTime.MinValue sentinel that WorkOrderService
            // already uses for rows created before these columns existed.
            migrationBuilder.Sql(
                @"ALTER TABLE ""ScheduleProposal""
ADD COLUMN IF NOT EXISTS ""RequestedStartTime"" timestamp with time zone NOT NULL DEFAULT '0001-01-01 00:00:00+00'");

            migrationBuilder.Sql(
                @"ALTER TABLE ""ScheduleProposal""
ADD COLUMN IF NOT EXISTS ""RequestedEndTime"" timestamp with time zone NOT NULL DEFAULT '0001-01-01 00:00:00+00'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""ScheduleProposal"" DROP COLUMN IF EXISTS ""RequestedStartTime""");

            migrationBuilder.Sql(@"ALTER TABLE ""ScheduleProposal"" DROP COLUMN IF EXISTS ""RequestedEndTime""");
        }
    }
}
