using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class ServiceItem : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public decimal MemberPrice { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>显示排序：值越小越靠前；并列时按 Code 字母序。</summary>
    public int Sort { get; set; }

    /// <summary>初级技师做该服务的价格；null 表示一价通吃（取 Price）。</summary>
    public decimal? PriceJunior { get; set; }
    /// <summary>高级（老师傅）做该服务的价格；null 表示一价通吃。</summary>
    public decimal? PriceMaster { get; set; }

    public ICollection<CommissionRule> CommissionRules { get; set; } = new List<CommissionRule>();
}
