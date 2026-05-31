using LuminaTutors.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations;

internal sealed class AiTutorSessionConfiguration : IEntityTypeConfiguration<AiTutorSession>
{
    public void Configure(EntityTypeBuilder<AiTutorSession> builder)
    {
        builder.ToTable("AiTutorSessions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.AdminNote).HasMaxLength(1000);

        builder.HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.SchoolId, s.StudentId });
        builder.HasIndex(s => s.IsFlagged);
    }
}

internal sealed class AiTutorMessageConfiguration : IEntityTypeConfiguration<AiTutorMessage>
{
    public void Configure(EntityTypeBuilder<AiTutorMessage> builder)
    {
        builder.ToTable("AiTutorMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Content).HasMaxLength(4000).IsRequired();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(m => m.SessionId);
    }
}
