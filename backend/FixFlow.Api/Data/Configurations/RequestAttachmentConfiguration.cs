using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FixFlow.Api.Models;

namespace FixFlow.Api.Data.Configurations;

public class RequestAttachmentConfiguration : IEntityTypeConfiguration<RequestAttachment>
{
    public void Configure(EntityTypeBuilder<RequestAttachment> builder)
    {
        builder.Property(x => x.SecureUrl).IsRequired();
        builder.Property(x => x.PublicId).IsRequired().HasMaxLength(300);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(300);

        builder.HasOne(x => x.MaintenanceRequest)
            .WithMany()
            .HasForeignKey(x => x.MaintenanceRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MaintenanceRequestId);
    }
}