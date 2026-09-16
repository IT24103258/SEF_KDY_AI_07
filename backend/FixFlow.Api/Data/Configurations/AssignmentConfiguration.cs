using FixFlow.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FixFlow.Api.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.MatchScore)
               .IsRequired();

        builder.Property(a => a.Status)
               .HasMaxLength(50)
               .HasDefaultValue("Recommended");

        builder.HasOne(a => a.Technician)
               .WithMany(t => t.Assignments)
               .HasForeignKey(a => a.TechnicianId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}