using FixFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.Name).IsUnique();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.HasOne(u => u.Role)
               .WithMany(r => r.Users)
               .HasForeignKey(u => u.RoleId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Name).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Building).HasMaxLength(100);
    }
}

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        builder.Property(a => a.AssetCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(a => a.AssetCode).IsUnique();
        builder.HasOne(a => a.Location)
               .WithMany(l => l.Assets)
               .HasForeignKey(a => a.LocationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MaintenanceRequestConfiguration : IEntityTypeConfiguration<MaintenanceRequest>
{
    public void Configure(EntityTypeBuilder<MaintenanceRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RequestNumber).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.RequestNumber).IsUnique();
        builder.Property(r => r.Title).IsRequired().HasMaxLength(200);

        builder.HasOne(r => r.Requester)
               .WithMany(u => u.MaintenanceRequests)
               .HasForeignKey(r => r.RequesterId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Location)
               .WithMany(l => l.MaintenanceRequests)
               .HasForeignKey(r => r.LocationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Asset)
               .WithMany(a => a.MaintenanceRequests)
               .HasForeignKey(r => r.AssetId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.Category)
               .WithMany(c => c.MaintenanceRequests)
               .HasForeignKey(r => r.CategoryId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class IssueCategoryConfiguration : IEntityTypeConfiguration<IssueCategory>
{
    public void Configure(EntityTypeBuilder<IssueCategory> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
    }
}

public class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.EmployeeId).IsRequired().HasMaxLength(50);
        builder.HasOne(t => t.User)
               .WithOne()
               .HasForeignKey<Technician>(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SkillConfiguration : IEntityTypeConfiguration<Skill>
{
    public void Configure(EntityTypeBuilder<Skill> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.HasOne(n => n.User)
               .WithMany(u => u.Notifications)
               .HasForeignKey(n => n.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SLAConfigurationConfiguration : IEntityTypeConfiguration<SLAConfiguration>
{
    public void Configure(EntityTypeBuilder<SLAConfiguration> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.PriorityLevel).IsRequired().HasMaxLength(50);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasOne(a => a.User)
               .WithMany()
               .HasForeignKey(a => a.UserId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AgentWorkflowConfiguration : IEntityTypeConfiguration<AgentWorkflow>
{
    public void Configure(EntityTypeBuilder<AgentWorkflow> builder)
    {
        builder.HasKey(w => w.Id);
        builder.HasOne(w => w.Request)
               .WithMany(r => r.AgentWorkflows)
               .HasForeignKey(w => w.RequestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AgentStepConfiguration : IEntityTypeConfiguration<AgentStep>
{
    public void Configure(EntityTypeBuilder<AgentStep> builder)
    {
        builder.HasKey(s => s.Id);
        builder.HasOne(s => s.Workflow)
               .WithMany(w => w.Steps)
               .HasForeignKey(s => s.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AgentToolCallConfiguration : IEntityTypeConfiguration<AgentToolCall>
{
    public void Configure(EntityTypeBuilder<AgentToolCall> builder)
    {
        builder.HasKey(tc => tc.Id);
        builder.HasOne(tc => tc.Step)
               .WithMany(s => s.ToolCalls)
               .HasForeignKey(tc => tc.StepId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApprovalActionConfiguration : IEntityTypeConfiguration<ApprovalAction>
{
    public void Configure(EntityTypeBuilder<ApprovalAction> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasOne(a => a.Workflow)
               .WithMany(w => w.ApprovalActions)
               .HasForeignKey(a => a.WorkflowId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Approver)
               .WithMany()
               .HasForeignKey(a => a.ApproverId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
