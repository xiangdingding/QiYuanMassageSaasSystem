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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
