using System.Collections.Generic;
using System.Windows;
using MassageSaas.Shared.Schedules;

namespace MassageSaas.Cs.Views;

/// <summary>请假详情：只读展示一条请假单的全部信息（含上午/下午、天数、登记/审批人时间）。</summary>
public partial class LeaveDetailWindow : Window
{
    public LeaveDetailWindow(LeaveRequestDto leave)
    {
        InitializeComponent();
        DataContext = new Vm(leave);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    public sealed class Vm
    {
        private readonly LeaveRequestDto _l;
        public Vm(LeaveRequestDto l) => _l = l;

        public string UserName => _l.UserName;
        public string TypeLabel => Map(_l.Type, TypeMap);
        public string StartText => $"{_l.FromDate:yyyy-MM-dd}　{Half(_l.StartHalf)}";
        public string EndText => $"{_l.ToDate:yyyy-MM-dd}　{Half(_l.EndHalf)}";
        public string DaysText => $"{_l.Days:0.#} 天";
        public string Reason => string.IsNullOrWhiteSpace(_l.Reason) ? "—" : _l.Reason!;
        public string StatusLabel => Map(_l.Status, StatusMap);
        public string CreatedAtText => _l.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
        public string ApproverName => string.IsNullOrWhiteSpace(_l.ApproverName) ? "—" : _l.ApproverName!;
        public string ApprovedAtText => _l.ApprovedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—";

        private static string Half(string h) => h == "Morning" ? "上午" : h == "Afternoon" ? "下午" : h;
        private static string Map(string k, IReadOnlyDictionary<string, string> m) => m.TryGetValue(k, out var v) ? v : k;

        private static readonly Dictionary<string, string> TypeMap = new()
        { ["Sick"] = "病假", ["Personal"] = "事假", ["Annual"] = "年假", ["Training"] = "培训" };
        private static readonly Dictionary<string, string> StatusMap = new()
        { ["Pending"] = "待审批", ["Approved"] = "已通过", ["Rejected"] = "已驳回", ["Cancelled"] = "已撤销" };
    }
}
