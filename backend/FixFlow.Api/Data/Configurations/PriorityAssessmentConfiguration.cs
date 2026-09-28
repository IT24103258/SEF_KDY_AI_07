using FixFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Data.Configurations;

public class PriorityAssessmentConfiguration : IEntityTypeConfiguration<PriorityAssessment>
{
    public void Configure(EntityTypeBuilder<PriorityAssessment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RequestId)
               .IsRequired();

        builder.HasIndex(p => p.RequestId);

        // Purely additive foreign key from the new PriorityAssessments table pointing to MaintenanceRequests.
        // This does NOT alter the MaintenanceRequests table in any way.
        builder.HasOne<MaintenanceRequest>()
               .WithMany()
               .HasForeignKey(p => p.RequestId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.AssetCriticality)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.ImpactLevel)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.LikelihoodLevel)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.RiskScore)
               .IsRequired();

        builder.Property(p => p.RiskLevel)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.Priority)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(p => p.RecommendedResponseWindow)
               .HasMaxLength(100);

        builder.Property(p => p.ResponseTimeHours)
               .IsRequired();

        builder.Property(p => p.ResolutionTimeHours)
               .IsRequired();

        builder.Property(p => p.EscalationFlag)
               .IsRequired();

        builder.Property(p => p.EscalationReason)
               .HasMaxLength(500);

        builder.Property(p => p.Explanation)
               .HasMaxLength(2000);

        builder.Property(p => p.ContributingFactorsJson)
               .HasMaxLength(4000);

        builder.Property(p => p.AssessedBy)
               .HasMaxLength(100);

        builder.Property(p => p.Status)
               .HasMaxLength(50);
    }
}
