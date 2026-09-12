using FixFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FixFlow.Api.Data;

/*
================================================================================
FIXFLOW SHARED FOUNDATION DBCONTEXT
================================================================================
IMPORTANT FOR ALL 4 MEMBERS:
Students must NOT modify FixFlowDbContext.cs to register their entities.
The shared DbContext contains only shared DbSets.

Student-owned entities must add their own IEntityTypeConfiguration<T> file under:
backend/FixFlow.Api/Data/Configurations/

Examples:
- Data/Configurations/RequestClassificationConfiguration.cs (Member 1)
- Data/Configurations/PriorityAssessmentConfiguration.cs (Member 2)
- Data/Configurations/AssignmentConfiguration.cs (Member 3)
- Data/Configurations/WorkOrderConfiguration.cs (Member 4)

These configurations are automatically discovered using ApplyConfigurationsFromAssembly(...).
================================================================================
*/

public class FixFlowDbContext : DbContext
{
    public FixFlowDbContext(DbContextOptions<FixFlowDbContext> options) : base(options)
    {
    }

    // Shared Foundation DbSets Only
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<IssueCategory> IssueCategories => Set<IssueCategory>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AgentWorkflow> AgentWorkflows => Set<AgentWorkflow>();
    public DbSet<AgentStep> AgentSteps => Set<AgentStep>();
    public DbSet<AgentToolCall> AgentToolCalls => Set<AgentToolCall>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SLAConfiguration> SLAConfigurations => Set<SLAConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Automatically discovers and applies entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FixFlowDbContext).Assembly);
    }
}
