using LuminaTutors.Domain.Entities.Communication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.Communication;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("NotificationId").UseIdentityColumn();

        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Body).IsRequired();
        b.Property(x => x.NotificationType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Channel).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.TargetAudience).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.IsScheduled).HasDefaultValue(false);

        b.HasIndex(x => x.SchoolId).HasDatabaseName("IX_Notifications_SchoolId");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SentBy).WithMany()
            .HasForeignKey(x => x.SentByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TargetClass).WithMany()
            .HasForeignKey(x => x.TargetClassId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TargetGradeLevel).WithMany()
            .HasForeignKey(x => x.TargetGradeLevelId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationRecipientConfiguration : IEntityTypeConfiguration<NotificationRecipient>
{
    public void Configure(EntityTypeBuilder<NotificationRecipient> b)
    {
        b.ToTable("NotificationRecipients");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("RecipientId").UseIdentityColumn();

        b.Property(x => x.IsRead).HasDefaultValue(false);
        b.Property(x => x.DeliveryStatus).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => new { x.NotificationId, x.UserId })
            .IsUnique().HasDatabaseName("UQ_NotificationRecipients");
        b.HasIndex(x => new { x.UserId, x.IsRead })
            .HasDatabaseName("IX_NotificationRecipients_UserId_IsRead");

        b.HasOne(x => x.Notification).WithMany(n => n.Recipients)
            .HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.User).WithMany()
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("Conversations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ConversationId").UseIdentityColumn();

        b.Property(x => x.ConversationName).HasMaxLength(200);
        b.Property(x => x.ConversationType).HasConversion<string>().HasMaxLength(30);

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedBy).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> b)
    {
        b.ToTable("ConversationParticipants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("ParticipantId").UseIdentityColumn();

        b.Property(x => x.IsAdmin).HasDefaultValue(false);

        b.HasIndex(x => new { x.ConversationId, x.UserId })
            .IsUnique().HasDatabaseName("UQ_ConversationParticipants");

        b.HasOne(x => x.Conversation).WithMany(c => c.Participants)
            .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.User).WithMany(u => u.ConversationParticipants)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("Messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("MessageId").UseIdentityColumn();

        b.Property(x => x.MessageText).HasMaxLength(4000);
        b.Property(x => x.AttachmentUrl).HasMaxLength(500);
        b.Property(x => x.AttachmentType).HasMaxLength(20);
        b.Property(x => x.IsDeleted).HasDefaultValue(false);

        b.HasIndex(x => x.ConversationId).HasDatabaseName("IX_Messages_ConversationId");
        b.HasIndex(x => x.SentAt).HasDatabaseName("IX_Messages_SentAt");

        b.HasOne(x => x.Conversation).WithMany(c => c.Messages)
            .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Sender).WithMany(u => u.SentMessages)
            .HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class NewsBoardConfiguration : IEntityTypeConfiguration<NewsBoard>
{
    public void Configure(EntityTypeBuilder<NewsBoard> b)
    {
        b.ToTable("NewsBoards");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("NewsId").UseIdentityColumn();

        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.CoverImageUrl).HasMaxLength(500);
        b.Property(x => x.Scope).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.IsPinned).HasDefaultValue(false);
        b.Property(x => x.IsPublished).HasDefaultValue(false);

        b.HasIndex(x => new { x.SchoolId, x.IsPublished })
            .HasDatabaseName("IX_NewsBoards_School_Published");

        b.HasOne(x => x.School).WithMany()
            .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TargetClass).WithMany()
            .HasForeignKey(x => x.TargetClassId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TargetGradeLevel).WithMany()
            .HasForeignKey(x => x.TargetGradeLevelId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.PublishedBy).WithMany()
            .HasForeignKey(x => x.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
