using FixFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Data.Configurations;

/// <summary>
/// EF Core configuration for RequestClassification.
/// This file is automatically discovered by ApplyConfigurationsFromAssembly(...)
/// in FixFlowDbContext — no changes to DbContext or Program.cs are needed.
/// </summary>
public class RequestClassificationConfiguration : IEntityTypeConfiguration<RequestClassification>
{
    public void Configure(EntityTypeBuilder<RequestClassification> builder)
    {
        // ── Primary key (inherited from BaseEntity) ───────────────────────────
        builder.HasKey(rc => rc.Id);

        // ── Required fields & max lengths ────────────────────────────────────
        builder.Property(rc => rc.Category)
               .IsRequired()
               .HasMaxLength(100);

        // ── ConfidenceScore stored as decimal(4,3):
        //    precision=4 means 4 significant digits total,
        //    scale=3 means 3 digits after the decimal point.
        //    This stores values like 0.975 or 1.000 exactly.
        builder.Property(rc => rc.ConfidenceScore)
               .HasColumnType("decimal(4,3)");

        // ── Relationship: many classifications belong to one MaintenanceRequest.
        //    CascadeDelete: if the request is deleted, its classifications go too.
        builder.HasOne(rc => rc.MaintenanceRequest)
               .WithMany()                          // MaintenanceRequest has no nav back to classifications — keeps the shared entity clean
               .HasForeignKey(rc => rc.MaintenanceRequestId)
               .OnDelete(DeleteBehavior.Cascade);

        // ── Relationship: optional link to the manager who overrode the AI result.
        //    SetNull: if that user is deleted, we keep the classification row but
        //    clear the reference rather than cascade-deleting audit history.
        builder.HasOne(rc => rc.OverriddenByUser)
               .WithMany()
               .HasForeignKey(rc => rc.OverriddenByUserId)
               .OnDelete(DeleteBehavior.SetNull);

        // ── Index on MaintenanceRequestId for fast "fetch all classifications
        //    for a given request" queries (used by GET /api/requests/{id}/classifications).
        builder.HasIndex(rc => rc.MaintenanceRequestId);
    }
}
