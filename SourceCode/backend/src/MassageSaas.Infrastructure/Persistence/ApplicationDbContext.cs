using System.Linq.Expressions;
using System.Reflection;
using MassageSaas.Application.Abstractions;
using MassageSaas.Domain.Common;
using MassageSaas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ITenantContext TenantContext { get; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        TenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberType> MemberTypes => Set<MemberType>();
    public DbSet<ServiceItem> ServiceItems => Set<ServiceItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<TechnicianQueue> TechnicianQueues => Set<TechnicianQueue>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();
    public DbSet<MemberRechargeRecord> MemberRechargeRecords => Set<MemberRechargeRecord>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<DayClose> DayCloses => Set<DayClose>();
    public DbSet<MemberPackage> MemberPackages => Set<MemberPackage>();
    public DbSet<ServicePackage> ServicePackages => Set<ServicePackage>();
    public DbSet<ServicePackageItem> ServicePackageItems => Set<ServicePackageItem>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<ServiceReview> ServiceReviews => Set<ServiceReview>();
    public DbSet<StaffSchedule> StaffSchedules => Set<StaffSchedule>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<SalaryProfile> SalaryProfiles => Set<SalaryProfile>();
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollItem> PayrollItems => Set<PayrollItem>();
    public DbSet<PayrollAdjustment> PayrollAdjustments => Set<PayrollAdjustment>();
    public DbSet<StaffReferralRecord> StaffReferralRecords => Set<StaffReferralRecord>();
    public DbSet<NotificationOutbox> NotificationOutbox => Set<NotificationOutbox>();
    public DbSet<ServiceComplaint> ServiceComplaints => Set<ServiceComplaint>();
    public DbSet<StaffTransfer> StaffTransfers => Set<StaffTransfer>();
    public DbSet<TimedRoomSession> TimedRoomSessions => Set<TimedRoomSession>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();
    public DbSet<BusinessConsultation> BusinessConsultations => Set<BusinessConsultation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        var tenantContextProp = typeof(ApplicationDbContext).GetProperty(nameof(TenantContext))!;
        var currentTenantIdProp = typeof(ITenantContext).GetProperty(nameof(ITenantContext.TenantId))!;
        var bypassProp = typeof(ITenantContext).GetProperty(nameof(ITenantContext.IsFilterBypassed))!;
        var isDeletedProp = typeof(BaseEntity).GetProperty(nameof(BaseEntity.IsDeleted))!;
        var tenantIdEntityProp = typeof(ITenantScoped).GetProperty(nameof(ITenantScoped.TenantId))!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            var parameter = Expression.Parameter(clr, "e");

            var isTenantScoped = typeof(ITenantScoped).IsAssignableFrom(clr);
            var hasSoftDelete = typeof(BaseEntity).IsAssignableFrom(clr);

            Expression? body = null;

            if (isTenantScoped)
            {
                var ctxInstance = Expression.MakeMemberAccess(Expression.Constant(this), tenantContextProp);
                var currentTid = Expression.MakeMemberAccess(ctxInstance, currentTenantIdProp);
                var bypass = Expression.MakeMemberAccess(ctxInstance, bypassProp);
                var entityTid = Expression.Property(parameter, nameof(ITenantScoped.TenantId));
                var tenantMatch = Expression.Equal(entityTid, currentTid);
                body = Expression.OrElse(bypass, tenantMatch);
            }

            if (hasSoftDelete)
            {
                var notDeleted = Expression.Equal(
                    Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                    Expression.Constant(false));
                body = body is null ? notDeleted : Expression.AndAlso(body, notDeleted);
            }

            if (body is not null)
            {
                var lambda = Expression.Lambda(body, parameter);
                modelBuilder.Entity(clr).HasQueryFilter(lambda);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == null && TenantContext.TenantId != null)
            {
                entry.Entity.TenantId = TenantContext.TenantId;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
