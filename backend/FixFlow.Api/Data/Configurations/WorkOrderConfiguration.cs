using FixFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Data.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.WorkOrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(w => w.WorkOrderNumber)
            .IsUnique();

        builder.Property(w => w.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(w => w.Request)
            .WithMany()
            .HasForeignKey(w => w.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Technician)
            .WithMany()
            .HasForeignKey(w => w.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Location)
            .WithMany()
            .HasForeignKey(w => w.LocationId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(w => w.ApprovedBy)
            .WithMany()
            .HasForeignKey(w => w.ApprovedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(w => w.TechnicianId);
        builder.HasIndex(w => w.ScheduledStartTime);
        builder.HasIndex(w => w.ScheduledEndTime);
        builder.HasIndex(w => w.Status);
        builder.HasIndex(w => w.Priority);
        builder.HasIndex(w => w.RequestId);
        builder.HasIndex(w => new { w.TechnicianId, w.ScheduledStartTime, w.ScheduledEndTime });
    }
}

public class WorkOrderStatusHistoryConfiguration : IEntityTypeConfiguration<WorkOrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<WorkOrderStatusHistory> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(h => h.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(h => h.WorkOrder)
            .WithMany(w => w.StatusHistories)
            .HasForeignKey(h => h.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(h => h.WorkOrderId);
    }
}

public class WorkNoteConfiguration : IEntityTypeConfiguration<WorkNote>
{
    public void Configure(EntityTypeBuilder<WorkNote> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.NoteText)
            .IsRequired();

        builder.HasOne(n => n.WorkOrder)
            .WithMany(w => w.Notes)
            .HasForeignKey(n => n.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Author)
            .WithMany()
            .HasForeignKey(n => n.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.WorkOrderId);
    }
}

public class CompletionEvidenceConfiguration : IEntityTypeConfiguration<CompletionEvidence>
{
    public void Configure(EntityTypeBuilder<CompletionEvidence> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FileKey)
            .HasMaxLength(255);

        builder.Property(e => e.SignerName)
            .HasMaxLength(150);

        builder.HasOne(e => e.WorkOrder)
            .WithMany(w => w.Evidence)
            .HasForeignKey(e => e.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.UploadedBy)
            .WithMany()
            .HasForeignKey(e => e.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.WorkOrderId);
    }
}

public class ScheduleProposalConfiguration : IEntityTypeConfiguration<ScheduleProposal>
{
    public void Configure(EntityTypeBuilder<ScheduleProposal> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Priority)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(p => p.WorkOrder)
            .WithMany(w => w.Proposals)
            .HasForeignKey(p => p.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Request)
            .WithMany()
            .HasForeignKey(p => p.RequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Technician)
            .WithMany()
            .HasForeignKey(p => p.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.RequestId);
        builder.HasIndex(p => p.TechnicianId);
        builder.HasIndex(p => p.ProposedStartTime);
    }
}

public class BusinessHoursConfiguration : IEntityTypeConfiguration<BusinessHours>
{
    public void Configure(EntityTypeBuilder<BusinessHours> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.DayName)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(b => b.DayOfWeek)
            .IsUnique();
    }
}
