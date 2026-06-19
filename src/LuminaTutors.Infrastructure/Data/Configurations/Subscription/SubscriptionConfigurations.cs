using LuminaTutors.Domain.Entities.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuminaTutors.Infrastructure.Data.Configurations.Subscription;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> b)
    {
        b.ToTable("SubscriptionPlans");
        b.HasKey(x => x.Id);

        b.Property(x => x.PlanCode).IsRequired().HasMaxLength(30);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.MonthlyPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.QuarterlyPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.YearlyPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => x.PlanCode).IsUnique().HasDatabaseName("UQ_SubscriptionPlans_Code");
    }
}

public class SubscriptionAddOnConfiguration : IEntityTypeConfiguration<SubscriptionAddOn>
{
    public void Configure(EntityTypeBuilder<SubscriptionAddOn> b)
    {
        b.ToTable("SubscriptionAddOns");
        b.HasKey(x => x.Id);

        b.Property(x => x.AddOnCode).IsRequired().HasMaxLength(30);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.Feature).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.MonthlyPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.QuarterlyPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.YearlyPrice).HasColumnType("decimal(18,2)");
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => x.AddOnCode).IsUnique().HasDatabaseName("UQ_SubscriptionAddOns_Code");
    }
}

public class SchoolSubscriptionConfiguration : IEntityTypeConfiguration<SchoolSubscription>
{
    public void Configure(EntityTypeBuilder<SchoolSubscription> b)
    {
        b.ToTable("SchoolSubscriptions");
        b.HasKey(x => x.Id);

        b.Property(x => x.BillingCycle).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.AutoRenew).HasDefaultValue(true);

        b.HasOne(x => x.Plan).WithMany(p => p.Subscriptions)
            .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.AddOns).WithOne(a => a.Subscription)
            .HasForeignKey(a => a.SubscriptionId).OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Orders).WithOne(o => o.Subscription)
            .HasForeignKey(o => o.SubscriptionId).OnDelete(DeleteBehavior.Restrict);

        // Một trường chỉ giữ một bản ghi đăng ký (tái sử dụng khi đăng ký lại / nâng cấp).
        b.HasIndex(x => x.SchoolId).IsUnique().HasDatabaseName("UQ_SchoolSubscriptions_School");
    }
}

public class SchoolSubscriptionAddOnConfiguration : IEntityTypeConfiguration<SchoolSubscriptionAddOn>
{
    public void Configure(EntityTypeBuilder<SchoolSubscriptionAddOn> b)
    {
        b.ToTable("SchoolSubscriptionAddOns");
        b.HasKey(x => x.Id);

        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasOne(x => x.AddOn).WithMany()
            .HasForeignKey(x => x.AddOnId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.SubscriptionId, x.AddOnId })
            .IsUnique().HasDatabaseName("UQ_SchoolSubscriptionAddOns_Sub_AddOn");
    }
}

public class SubscriptionOrderConfiguration : IEntityTypeConfiguration<SubscriptionOrder>
{
    public void Configure(EntityTypeBuilder<SubscriptionOrder> b)
    {
        b.ToTable("SubscriptionOrders");
        b.HasKey(x => x.Id);

        b.Property(x => x.OrderCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.OrderType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.BillingCycle).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.TransactionCode).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.HasOne(x => x.Plan).WithMany()
            .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Items).WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.SchoolId, x.OrderCode })
            .IsUnique().HasDatabaseName("UQ_SubscriptionOrders_Code_School");
        b.HasIndex(x => x.Status).HasDatabaseName("IX_SubscriptionOrders_Status");
    }
}

public class SubscriptionOrderItemConfiguration : IEntityTypeConfiguration<SubscriptionOrderItem>
{
    public void Configure(EntityTypeBuilder<SubscriptionOrderItem> b)
    {
        b.ToTable("SubscriptionOrderItems");
        b.HasKey(x => x.Id);

        b.Property(x => x.ItemType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Description).IsRequired().HasMaxLength(200);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
    }
}
