using MassageSaas.Domain.Common;

namespace MassageSaas.Domain.Entities;

public class User : BaseEntity, ITenantScoped
{
    public long? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public long? StoreId { get; set; }
    public Store? Store { get; set; }

    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? RealName { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    public int? EmployeeNo { get; set; }
    public bool IsBlind { get; set; }
    public DateTime? LastLoginAt { get; set; }

    /// <summary>盲人按摩师证号（民政/残联颁发，行业资质核查需要）。</summary>
    public string? BlindCertNo { get; set; }
    /// <summary>技师等级，影响服务定价。</summary>
    public TechnicianLevel TechnicianLevel { get; set; } = TechnicianLevel.Senior;
    /// <summary>当日上钟数上限。0 = 不限制；超出后排队叫号会跳过该技师。</summary>
    public int MaxRoundsPerDay { get; set; }
    /// <summary>专长，逗号分隔（如 "肩颈,足疗,头疗"），便于派单。</summary>
    public string? Specialties { get; set; }

    /// <summary>身份证号（18 位中国大陆居民身份证）。</summary>
    public string? IdCardNo { get; set; }
    /// <summary>出生日期，可由身份证号 6..13 位自动推导。</summary>
    public DateTime? BirthDate { get; set; }
    /// <summary>紧急联系人姓名。</summary>
    public string? EmergencyContactName { get; set; }
    /// <summary>紧急联系人电话。</summary>
    public string? EmergencyContactPhone { get; set; }
    /// <summary>入职日期。</summary>
    public DateTime? HireDate { get; set; }
    /// <summary>离职日期；null 表示在职。</summary>
    public DateTime? TerminationDate { get; set; }
}
