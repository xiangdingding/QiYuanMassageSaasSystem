using MassageSaas.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MassageSaas.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<Plan> Plans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<PaymentOrder> PaymentOrders { get; }
    DbSet<Store> Stores { get; }
    DbSet<User> Users { get; }
    DbSet<Member> Members { get; }
    DbSet<ServiceItem> ServiceItems { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<TechnicianQueue> TechnicianQueues { get; }
    DbSet<CommissionRule> CommissionRules { get; }
    DbSet<MemberRechargeRecord> MemberRechargeRecords { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Room> Rooms { get; }
    DbSet<DayClose> DayCloses { get; }
    DbSet<MemberPackage> MemberPackages { get; }
    DbSet<ServicePackage> ServicePackages { get; }
    DbSet<ServicePackageItem> ServicePackageItems { get; }
    DbSet<Voucher> Vouchers { get; }
    DbSet<ServiceReview> ServiceReviews { get; }
    DbSet<StaffSchedule> StaffSchedules { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }
    DbSet<SalaryProfile> SalaryProfiles { get; }
    DbSet<PayrollPeriod> PayrollPeriods { get; }
    DbSet<PayrollItem> PayrollItems { get; }
    DbSet<PayrollAdjustment> PayrollAdjustments { get; }
    DbSet<NotificationOutbox> NotificationOutbox { get; }
    DbSet<ServiceComplaint> ServiceComplaints { get; }
    DbSet<StaffTransfer> StaffTransfers { get; }
    DbSet<TimedRoomSession> TimedRoomSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
