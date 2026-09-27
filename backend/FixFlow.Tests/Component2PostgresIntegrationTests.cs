using FixFlow.Api.Data;
using FixFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FixFlow.Tests;

/// <summary>
/// Issue 6 — a HONEST, real PostgreSQL integration test for Component 2.
///
/// The rest of the Component 2 suite runs on the EF Core InMemory provider and is named
/// accordingly. This class is the only one that touches a live PostgreSQL server, and it is
/// GUARDED: it does nothing unless the FIXFLOW_INTEGRATION_CONNECTION environment variable
/// supplies a connection string. That keeps `dotnet test` green on machines without a database
/// while still providing a real round-trip check when one is available.
///
/// SAFETY: this test is strictly READ-ONLY. It never inserts, updates, deletes, migrates,
/// truncates, or drops anything. It only opens a connection and counts rows, so it can be run
/// against the shared `fixflow_db` without risking any component's data.
/// </summary>
public class Component2PostgresIntegrationTests
{
    private const string ConnectionEnvVar = "FIXFLOW_INTEGRATION_CONNECTION";

    [Fact]
    public async Task PostgreSQL_ReadOnlyConnectivity_CountsPriorityAssessments()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionEnvVar);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Not configured → skip silently. This is intentional and documented above;
            // the InMemory suite provides the behavioural coverage in CI.
            return;
        }

        var options = new DbContextOptionsBuilder<FixFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new FixFlowDbContext(options);

        // Read-only: prove the Npgsql provider can reach the database and query the
        // Component 2 table. No writes of any kind.
        var canConnect = await context.Database.CanConnectAsync();
        Assert.True(canConnect, "Configured PostgreSQL connection string could not reach the database.");

        var assessmentCount = await context.Set<PriorityAssessment>().CountAsync();
        Assert.True(assessmentCount >= 0);
    }
}
