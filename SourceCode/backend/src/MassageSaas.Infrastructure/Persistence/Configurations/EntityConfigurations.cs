using MassageSaas.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MassageSaas.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.ContactPhone).HasMaxLength(32).IsRequired();
        b.Property(x => x.ContactName).HasMaxLength(64);
        b.Property(x => x.ReferralRewardPercent).HasPrecision(5, 2);
        b.HasIndex(x => x.ContactPhone);
        b.HasIndex(x => x.Status);
        b.HasOne(x => x.CurrentPlan).WithMany().HasForeignKey(x => x.CurrentPlanId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> b)
    {
        b.ToTable("plans");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(32).IsRequired();
        b.Property(x => x.Name).HasMaxLength(64).IsRequired();
        b.Property(x => x.AnnualPrice).HasPrecision(18, 2);
        b.Property(x => x.FeatureJson).HasColumnType("json");
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasOne(x => x.Tenant).WithMany(t => t.Subscriptions).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PaymentOrder).WithMany().HasForeignKey(x => x.PaymentOrderId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.EndAt });
    }
}

public class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
{
    public void Configure(EntityTypeBuilder<PaymentOrder> b)
    {
        b.ToTable("payment_orders");
        b.HasKey(x => x.Id);
        b.Property(x => x.OrderNo).HasMaxLength(64).IsRequired();
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.ThirdPartyTransactionNo).HasMaxLength(128);
        b.Property(x => x.RawCallbackPayload).HasColumnType("text");
        b.HasIndex(x => x.OrderNo).IsUnique();
        b.HasIndex(x => x.ThirdPartyTransactionNo);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Plan).WithMany().HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> b)
    {
        b.ToTable("stores");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Address).HasMaxLength(500);
        b.Property(x => x.Phone).HasMaxLength(32);
        b.Ignore(x => x.IsHeadquarters);
        b.HasOne(x => x.Tenant).WithMany(t => t.Stores).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ParentStore).WithMany(p => p.Branches).HasForeignKey(x => x.ParentStoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.TenantId, x.ParentStoreId });
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Username).HasMaxLength(64).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        b.Property(x => x.RealName).HasMaxLength(64);
        b.Property(x => x.Phone).HasMaxLength(32);
        b.Property(x => x.BlindCertNo).HasMaxLength(64);
        b.Property(x => x.Specialties).HasMaxLength(200);
        b.HasIndex(x => new { x.TenantId, x.Username }).IsUnique();
        b.HasIndex(x => x.Phone);
        b.HasOne(x => x.Tenant).WithMany(t => t.Users).HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Store).WithMany(s => s.Users).HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> b)
    {
        b.ToTable("members");
        b.HasKey(x => x.Id);
        b.Property(x => x.CardNo).HasMaxLength(32).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(32).IsRequired();
        b.Property(x => x.Name).HasMaxLength(64);
        b.Property(x => x.Gender).HasMaxLength(8);
        b.Property(x => x.Balance).HasPrecision(18, 2);
        b.Property(x => x.TotalRecharge).HasPrecision(18, 2);
        b.Property(x => x.TotalConsumed).HasPrecision(18, 2);
        b.Property(x => x.Discount).HasPrecision(5, 4);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.Property(x => x.PreferenceNotes).HasMaxLength(500);
        b.Property(x => x.HealthNotes).HasMaxLength(1000);
        b.Property(x => x.CloseReason).HasMaxLength(200);
        b.Property(x => x.ReferralRewardEarned).HasPrecision(18, 2);
        b.HasIndex(x => new { x.TenantId, x.CardNo }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Phone });
        b.HasIndex(x => x.ReferredByMemberId);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReferredByMember).WithMany().HasForeignKey(x => x.ReferredByMemberId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ServiceItemConfiguration : IEntityTypeConfiguration<ServiceItem>
{
    public void Configure(EntityTypeBuilder<ServiceItem> b)
    {
        b.ToTable("service_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(32).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.MemberPrice).HasPrecision(18, 2);
        b.Property(x => x.PriceJunior).HasPrecision(18, 2);
        b.Property(x => x.PriceMaster).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders");
        b.HasKey(x => x.Id);
        b.Property(x => x.OrderNo).HasMaxLength(64).IsRequired();
        b.Property(x => x.Total).HasPrecision(18, 2);
        b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        b.Property(x => x.PaidAmount).HasPrecision(18, 2);
        b.Property(x => x.TipAmount).HasPrecision(18, 2);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.Property(x => x.OfflineCacheKey).HasMaxLength(64);
        b.Property(x => x.ReopenReason).HasMaxLength(200);
        b.HasIndex(x => new { x.TenantId, x.OrderNo }).IsUnique();
        b.HasIndex(x => new { x.StoreId, x.CreatedAt });
        b.HasIndex(x => x.OfflineCacheKey);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.CashierUser).WithMany().HasForeignKey(x => x.CashierUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Voucher).WithMany().HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("order_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.ServiceName).HasMaxLength(200).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.ItemTotal).HasPrecision(18, 2);
        b.Property(x => x.CommissionAmount).HasPrecision(18, 2);
        b.Property(x => x.TipAmount).HasPrecision(18, 2);
        b.Property(x => x.RoomNoSnapshot).HasMaxLength(32);
        b.Property(x => x.TransferReason).HasMaxLength(200);
        b.Property(x => x.MergedGroupKey).HasMaxLength(36);
        b.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Technician).WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.MemberPackage).WithMany().HasForeignKey(x => x.MemberPackageId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.TechnicianId, x.CreatedAt });
        b.HasIndex(x => x.RoomId);
    }
}

public class MemberPackageConfiguration : IEntityTypeConfiguration<MemberPackage>
{
    public void Configure(EntityTypeBuilder<MemberPackage> b)
    {
        b.ToTable("member_packages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(100).IsRequired();
        b.Property(x => x.PaidAmount).HasPrecision(18, 2);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.MemberId, x.Status });
    }
}

public class ServicePackageConfiguration : IEntityTypeConfiguration<ServicePackage>
{
    public void Configure(EntityTypeBuilder<ServicePackage> b)
    {
        b.ToTable("service_packages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(32).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.MemberPrice).HasPrecision(18, 2);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
    }
}

public class ServicePackageItemConfiguration : IEntityTypeConfiguration<ServicePackageItem>
{
    public void Configure(EntityTypeBuilder<ServicePackageItem> b)
    {
        b.ToTable("service_package_items");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Package).WithMany(p => p.Items).HasForeignKey(x => x.PackageId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VoucherConfiguration : IEntityTypeConfiguration<Voucher>
{
    public void Configure(EntityTypeBuilder<Voucher> b)
    {
        b.ToTable("vouchers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(64).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.FaceValue).HasPrecision(18, 2);
        b.Property(x => x.MinOrderAmount).HasPrecision(18, 2);
        b.Property(x => x.DiscountPercent).HasPrecision(5, 4);
        b.Property(x => x.Platform).HasMaxLength(64);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        b.HasIndex(x => x.Status);
    }
}

public class ServiceReviewConfiguration : IEntityTypeConfiguration<ServiceReview>
{
    public void Configure(EntityTypeBuilder<ServiceReview> b)
    {
        b.ToTable("service_reviews");
        b.HasKey(x => x.Id);
        b.Property(x => x.Tags).HasMaxLength(200);
        b.Property(x => x.Comment).HasMaxLength(1000);
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Technician).WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.TechnicianId, x.CreatedAt });
    }
}

public class StaffScheduleConfiguration : IEntityTypeConfiguration<StaffSchedule>
{
    public void Configure(EntityTypeBuilder<StaffSchedule> b)
    {
        b.ToTable("staff_schedules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Remark).HasMaxLength(200);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.StoreId, x.WorkDate, x.UserId }).IsUnique();
    }
}

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> b)
    {
        b.ToTable("leave_requests");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ApproverUser).WithMany().HasForeignKey(x => x.ApproverUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.UserId, x.FromDate });
    }
}

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> b)
    {
        b.ToTable("inventory_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(64).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(16);
        b.Property(x => x.Quantity).HasPrecision(18, 3);
        b.Property(x => x.MinQuantity).HasPrecision(18, 3);
        b.Property(x => x.UnitCost).HasPrecision(18, 2);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.StoreId, x.Code }).IsUnique();
    }
}

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> b)
    {
        b.ToTable("inventory_movements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Delta).HasPrecision(18, 3);
        b.Property(x => x.QuantityAfter).HasPrecision(18, 3);
        b.Property(x => x.Remark).HasMaxLength(200);
        b.HasOne(x => x.Item).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.ItemId, x.CreatedAt });
    }
}

public class SalaryProfileConfiguration : IEntityTypeConfiguration<SalaryProfile>
{
    public void Configure(EntityTypeBuilder<SalaryProfile> b)
    {
        b.ToTable("salary_profiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.BaseMonthly).HasPrecision(18, 2);
        b.Property(x => x.OvertimeHourRate).HasPrecision(18, 2);
        b.Property(x => x.AttendanceBonusAmount).HasPrecision(18, 2);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
    }
}

public class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> b)
    {
        b.ToTable("payroll_periods");
        b.HasKey(x => x.Id);
        b.Property(x => x.TotalAmount).HasPrecision(18, 2);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.StoreId, x.Year, x.Month }).IsUnique();
    }
}

public class PayrollItemConfiguration : IEntityTypeConfiguration<PayrollItem>
{
    public void Configure(EntityTypeBuilder<PayrollItem> b)
    {
        b.ToTable("payroll_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.BaseSalary).HasPrecision(18, 2);
        b.Property(x => x.CommissionTotal).HasPrecision(18, 2);
        b.Property(x => x.TipsTotal).HasPrecision(18, 2);
        b.Property(x => x.OvertimeHours).HasPrecision(8, 2);
        b.Property(x => x.OvertimeAmount).HasPrecision(18, 2);
        b.Property(x => x.AttendanceBonus).HasPrecision(18, 2);
        b.Property(x => x.AdjustmentTotal).HasPrecision(18, 2);
        b.Property(x => x.NetTotal).HasPrecision(18, 2);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasOne(x => x.Period).WithMany(p => p.Items).HasForeignKey(x => x.PeriodId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PeriodId, x.UserId }).IsUnique();
    }
}

public class StaffTransferConfiguration : IEntityTypeConfiguration<StaffTransfer>
{
    public void Configure(EntityTypeBuilder<StaffTransfer> b)
    {
        b.ToTable("staff_transfers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.FromStore).WithMany().HasForeignKey(x => x.FromStoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ToStore).WithMany().HasForeignKey(x => x.ToStoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.UserId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.ToStoreId });
    }
}

public class ServiceComplaintConfiguration : IEntityTypeConfiguration<ServiceComplaint>
{
    public void Configure(EntityTypeBuilder<ServiceComplaint> b)
    {
        b.ToTable("service_complaints");
        b.HasKey(x => x.Id);
        b.Property(x => x.Tags).HasMaxLength(200);
        b.Property(x => x.Comment).HasMaxLength(1000);
        b.Property(x => x.ResolutionNote).HasMaxLength(500);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.OrderItem).WithMany().HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.OriginalTechnician).WithMany().HasForeignKey(x => x.OriginalTechnicianId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ReassignedToTechnician).WithMany().HasForeignKey(x => x.ReassignedToTechnicianId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ResolvedByUser).WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.StoreId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.OriginalTechnicianId, x.CreatedAt });
    }
}

public class NotificationOutboxConfiguration : IEntityTypeConfiguration<NotificationOutbox>
{
    public void Configure(EntityTypeBuilder<NotificationOutbox> b)
    {
        b.ToTable("notification_outbox");
        b.HasKey(x => x.Id);
        b.Property(x => x.DedupKey).HasMaxLength(128).IsRequired();
        b.Property(x => x.RecipientOpenId).HasMaxLength(64);
        b.Property(x => x.RecipientPhone).HasMaxLength(32);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Body).HasMaxLength(500).IsRequired();
        b.Property(x => x.PayloadJson).HasColumnType("json");
        b.Property(x => x.ErrorMessage).HasMaxLength(500);
        b.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.DedupKey }).IsUnique();
        b.HasIndex(x => new { x.Status, x.ScheduledAt });
        b.HasIndex(x => new { x.TenantId, x.Kind, x.Status });
    }
}

public class PayrollAdjustmentConfiguration : IEntityTypeConfiguration<PayrollAdjustment>
{
    public void Configure(EntityTypeBuilder<PayrollAdjustment> b)
    {
        b.ToTable("payroll_adjustments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Reason).HasMaxLength(200).IsRequired();
        b.HasOne(x => x.Item).WithMany(i => i.Adjustments).HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("rooms");
        b.HasKey(x => x.Id);
        b.Property(x => x.RoomNo).HasMaxLength(32).IsRequired();
        b.Property(x => x.RoomType).HasMaxLength(32);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.Property(x => x.HourlyRate).HasPrecision(18, 2);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.TenantId, x.StoreId, x.RoomNo }).IsUnique();
    }
}

public class TimedRoomSessionConfiguration : IEntityTypeConfiguration<TimedRoomSession>
{
    public void Configure(EntityTypeBuilder<TimedRoomSession> b)
    {
        b.ToTable("timed_room_sessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.CustomerName).HasMaxLength(64);
        b.Property(x => x.HourlyRateSnapshot).HasPrecision(18, 2);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.StoreId, x.Status });
        b.HasIndex(x => new { x.RoomId, x.Status });
    }
}

public class DayCloseConfiguration : IEntityTypeConfiguration<DayClose>
{
    public void Configure(EntityTypeBuilder<DayClose> b)
    {
        b.ToTable("day_closes");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExpectedCash).HasPrecision(18, 2);
        b.Property(x => x.ActualCash).HasPrecision(18, 2);
        b.Property(x => x.Variance).HasPrecision(18, 2);
        b.Property(x => x.RevenueTotal).HasPrecision(18, 2);
        b.Property(x => x.CashAmount).HasPrecision(18, 2);
        b.Property(x => x.MemberCardAmount).HasPrecision(18, 2);
        b.Property(x => x.WechatAmount).HasPrecision(18, 2);
        b.Property(x => x.AlipayAmount).HasPrecision(18, 2);
        b.Property(x => x.BankCardAmount).HasPrecision(18, 2);
        b.Property(x => x.RechargeAmount).HasPrecision(18, 2);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.StoreId, x.BusinessDate }).IsUnique();
    }
}

public class TechnicianQueueConfiguration : IEntityTypeConfiguration<TechnicianQueue>
{
    public void Configure(EntityTypeBuilder<TechnicianQueue> b)
    {
        b.ToTable("technician_queue");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Technician).WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.StoreId, x.State, x.QueuePosition });
        b.HasIndex(x => new { x.TenantId, x.TechnicianId }).IsUnique();
    }
}

public class MemberRechargeRecordConfiguration : IEntityTypeConfiguration<MemberRechargeRecord>
{
    public void Configure(EntityTypeBuilder<MemberRechargeRecord> b)
    {
        b.ToTable("member_recharge_records");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.BonusAmount).HasPrecision(18, 2);
        b.Property(x => x.BalanceAfter).HasPrecision(18, 2);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.HasOne(x => x.Member).WithMany().HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperatorUser).WithMany().HasForeignKey(x => x.OperatorUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.CounterpartyMember).WithMany().HasForeignKey(x => x.CounterpartyMemberId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.MemberId, x.CreatedAt });
        b.HasIndex(x => x.Kind);
    }
}

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> b)
    {
        b.ToTable("appointments");
        b.HasKey(x => x.Id);
        b.Property(x => x.CustomerName).HasMaxLength(64).IsRequired();
        b.Property(x => x.CustomerPhone).HasMaxLength(32).IsRequired();
        b.Property(x => x.CustomerOpenId).HasMaxLength(64);
        b.Property(x => x.Remark).HasMaxLength(500);
        b.Property(x => x.CancelReason).HasMaxLength(200);
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Store).WithMany().HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Service).WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.PreferredTechnician).WithMany().HasForeignKey(x => x.PreferredTechnicianId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.StoreId, x.ExpectedArriveAt });
        b.HasIndex(x => new { x.TenantId, x.CustomerPhone });
        b.HasIndex(x => x.CustomerOpenId);
    }
}

public class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> b)
    {
        b.ToTable("commission_rules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 4);
        b.Property(x => x.TieredRulesJson).HasColumnType("json");
        b.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Service).WithMany(s => s.CommissionRules).HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Technician).WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.TenantId, x.ServiceId, x.TechnicianId });
    }
}
