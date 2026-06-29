using ClosedXML.Excel;

namespace MassageSaas.Api.Reports;

/// <summary>收益报表汇总「按支付方式」一行。</summary>
public record RevenuePayRow(string Method, decimal Amount);

/// <summary>收益报表「分期明细」一行（按日或按月）。</summary>
public record RevenuePeriodRow(string Period, int OrderCount, decimal Revenue, int Rounds);

/// <summary>收益报表（汇总）数据。营业额 = 已完成订单实收 + 计时房已结算房费。</summary>
public record RevenueReportData(
    string StoreName,
    string PeriodLabel,
    DateTime GeneratedAt,
    decimal Revenue,
    int OrderCount,
    int Rounds,
    int RefundCount,
    decimal RefundAmount,
    int RechargeCount,
    decimal RechargeAmount,
    IReadOnlyList<RevenuePayRow> PayRows,
    string PeriodColumnTitle,
    IReadOnlyList<RevenuePeriodRow> Periods)
{
    public decimal NetIncome => Revenue - RefundAmount;
}

/// <summary>收益明细一行：一笔收入交易（服务订单 / 计时房）。</summary>
public record RevenueDetailRow(
    DateTime Time,
    string Type,
    string DocNo,
    string Customer,
    string? Phone,
    string Items,
    string Technician,
    int Rounds,
    decimal Amount,
    string PayMethod,
    string Operator,
    string? Remark);

public record RevenueDetailData(
    string StoreName,
    string PeriodLabel,
    DateTime GeneratedAt,
    IReadOnlyList<RevenueDetailRow> Rows);

/// <summary>用 ClosedXML 生成收益报表 / 收益明细两类 .xlsx，供 BS/CS 两端下载。</summary>
public static class ReportExcelBuilder
{
    private const string Brand = "2D6A4F";       // 品牌深绿（文字）
    private const string TitleBg = "D9EAD3";     // 标题浅绿底
    private const string BandBg = "C6E0B4";      // 区块标题 / 合计 中绿底
    private const string HeaderBg = "A9D08E";    // 列表头 较深绿底
    private const string DataBg = "EBF1DE";      // 数据行 极浅绿底
    private const string GridLine = "A9D08E";    // 表格线（淡绿）
    private const string MoneyFmt = "#,##0.00";

    public static byte[] BuildRevenueReport(RevenueReportData d)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("收益报表");
        ws.Style.Font.FontName = "微软雅黑";
        ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        const int W = 4;   // 整表最大列宽（分期明细 4 列）
        var r = 1;

        // 标题：浅绿底 + 深绿粗体，跨全宽居中
        ws.Cell(r, 1).Value = $"{d.StoreName} · 收益报表";
        var title = ws.Range(r, 1, r, W).Merge();
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 16;
        title.Style.Font.FontColor = XLColor.FromHtml("#" + Brand);
        title.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + TitleBg);
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(r).Height = 30;
        r++;

        ws.Cell(r, 1).Value = $"统计周期：{d.PeriodLabel}";
        ws.Range(r, 1, r, W).Merge();
        r++;
        ws.Cell(r, 1).Value = $"生成时间：{d.GeneratedAt:yyyy-MM-dd HH:mm}";
        var gen = ws.Range(r, 1, r, W).Merge();
        gen.Style.Font.FontColor = XLColor.Gray;
        r += 2;

        // —— 区块标题（合并居中、中绿底） ——
        void Band(string text, int width)
        {
            ws.Cell(r, 1).Value = text;
            var rg = ws.Range(r, 1, r, width).Merge();
            rg.Style.Font.Bold = true;
            rg.Style.Font.FontColor = XLColor.FromHtml("#" + Brand);
            rg.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + BandBg);
            rg.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;
        }
        // —— 列表头（较深绿底、粗体居中） ——
        void ColHeader(int width, params string[] titles)
        {
            for (var i = 0; i < titles.Length; i++) ws.Cell(r, i + 1).Value = titles[i];
            var rg = ws.Range(r, 1, r, width);
            rg.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + HeaderBg);
            rg.Style.Font.Bold = true;
            rg.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;
        }
        // —— 给一个区块（从 top 行到上一行）描淡绿网格线 ——
        void Frame(int top, int width)
        {
            var rg = ws.Range(top, 1, r - 1, width);
            rg.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            rg.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            rg.Style.Border.OutsideBorderColor = XLColor.FromHtml("#" + GridLine);
            rg.Style.Border.InsideBorderColor = XLColor.FromHtml("#" + GridLine);
        }

        // 关键指标块（名称 | 值，2 列）
        var metricsTop = r;
        Band("关键指标", 2);
        void Metric(string name, object value, bool money)
        {
            ws.Cell(r, 1).Value = name;
            var c = ws.Cell(r, 2);
            c.Value = XLCellValue.FromObject(value);
            c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (money) c.Style.NumberFormat.Format = MoneyFmt;
            ws.Range(r, 1, r, 2).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + DataBg);
            r++;
        }
        Metric("营业额（含计时房）", d.Revenue, true);
        Metric("订单数", d.OrderCount, false);
        Metric("钟数合计", d.Rounds, false);
        Metric("退款金额", d.RefundAmount, true);
        Metric("退款单数", d.RefundCount, false);
        Metric("充值入账", d.RechargeAmount, true);
        Metric("充值笔数", d.RechargeCount, false);
        Metric("净收入（营业额−退款）", d.NetIncome, true);
        Frame(metricsTop, 2);
        r++;

        // 按支付方式（支付方式 | 金额 | 占比，3 列）
        var payTop = r;
        Band("按支付方式", 3);
        ColHeader(3, "支付方式", "金额", "占比");
        foreach (var p in d.PayRows)
        {
            ws.Cell(r, 1).Value = p.Method;
            var amt = ws.Cell(r, 2);
            amt.Value = p.Amount;
            amt.Style.NumberFormat.Format = MoneyFmt;
            amt.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            var pct = ws.Cell(r, 3);
            pct.Value = d.Revenue > 0 ? p.Amount / d.Revenue : 0;
            pct.Style.NumberFormat.Format = "0.0%";
            pct.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Range(r, 1, r, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + DataBg);
            r++;
        }
        Frame(payTop, 3);
        r++;

        // 分期明细（周期 | 订单数 | 营业额 | 钟数，4 列）
        var detTop = r;
        Band("分期明细", 4);
        ColHeader(4, d.PeriodColumnTitle, "订单数", "营业额", "钟数");
        foreach (var p in d.Periods)
        {
            ws.Cell(r, 1).Value = p.Period;
            CenterInt(ws.Cell(r, 2), p.OrderCount);
            CenterMoney(ws.Cell(r, 3), p.Revenue);
            CenterInt(ws.Cell(r, 4), p.Rounds);
            ws.Range(r, 1, r, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + DataBg);
            r++;
        }
        // 合计行：中绿底、粗体
        ws.Cell(r, 1).Value = "合计";
        CenterInt(ws.Cell(r, 2), d.Periods.Sum(p => p.OrderCount));
        CenterMoney(ws.Cell(r, 3), d.Periods.Sum(p => p.Revenue));
        CenterInt(ws.Cell(r, 4), d.Periods.Sum(p => p.Rounds));
        var totalRow = ws.Range(r, 1, r, 4);
        totalRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + BandBg);
        totalRow.Style.Font.Bold = true;
        r++;
        Frame(detTop, 4);

        ws.Columns(1, W).AdjustToContents();
        if (ws.Column(1).Width < 20) ws.Column(1).Width = 20;
        if (ws.Column(2).Width < 12) ws.Column(2).Width = 12;
        if (ws.Column(3).Width < 12) ws.Column(3).Width = 12;
        if (ws.Column(4).Width < 10) ws.Column(4).Width = 10;

        return ToBytes(wb);
    }

    private static void CenterInt(IXLCell c, int value)
    {
        c.Value = value;
        c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void CenterMoney(IXLCell c, decimal value)
    {
        c.Value = value;
        c.Style.NumberFormat.Format = MoneyFmt;
        c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    public static byte[] BuildRevenueDetail(RevenueDetailData d)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("收益明细");
        ws.Style.Font.FontName = "微软雅黑";
        ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        string[] cols =
        {
            "序号", "时间", "类型", "单号", "客户", "手机号",
            "项目", "技师", "钟数", "金额", "支付方式", "收银/操作员", "备注"
        };
        var n = cols.Length;

        // 标题：浅绿底 + 深绿粗体，跨全宽居中
        ws.Cell(1, 1).Value = $"{d.StoreName} · 收益明细";
        var title = ws.Range(1, 1, 1, n).Merge();
        title.Style.Font.Bold = true;
        title.Style.Font.FontSize = 16;
        title.Style.Font.FontColor = XLColor.FromHtml("#" + Brand);
        title.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + TitleBg);
        title.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Row(1).Height = 30;

        ws.Cell(2, 1).Value = $"统计周期：{d.PeriodLabel}　生成时间：{d.GeneratedAt:yyyy-MM-dd HH:mm}";
        ws.Range(2, 1, 2, n).Merge();
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        var head = 4;
        for (var i = 0; i < n; i++) ws.Cell(head, i + 1).Value = cols[i];
        var headerRg = ws.Range(head, 1, head, n);
        headerRg.Style.Fill.BackgroundColor = XLColor.FromHtml("#" + HeaderBg);
        headerRg.Style.Font.Bold = true;
        headerRg.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var row = head + 1;
        var seq = 1;
        foreach (var x in d.Rows)
        {
            var c = 1;
            CenterInt(ws.Cell(row, c++), seq++);
            var t = ws.Cell(row, c++);
            t.Value = x.Time;
            t.Style.NumberFormat.Format = "yyyy-MM-dd HH:mm";
            t.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            CenterText(ws.Cell(row, c++), x.Type);
            ws.Cell(row, c++).Value = x.DocNo;
            ws.Cell(row, c++).Value = x.Customer;
            ws.Cell(row, c++).Value = x.Phone ?? "";
            ws.Cell(row, c++).Value = x.Items;
            CenterText(ws.Cell(row, c++), x.Technician);
            CenterInt(ws.Cell(row, c++), x.Rounds);
            CenterMoney(ws.Cell(row, c++), x.Amount);
            CenterText(ws.Cell(row, c++), x.PayMethod);
            CenterText(ws.Cell(row, c++), x.Operator);
            ws.Cell(row, c++).Value = x.Remark ?? "";
            ws.Range(row, 1, row, n).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + DataBg);
            row++;
        }

        // 合计行：金额合计，中绿底粗体
        ws.Cell(row, 1).Value = "合计";
        ws.Range(row, 1, row, 9).Merge();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        CenterMoney(ws.Cell(row, 10), d.Rows.Sum(x => x.Amount));
        ws.Cell(row, 10).Style.Font.Bold = true;
        ws.Range(row, 1, row, n).Style.Fill.BackgroundColor = XLColor.FromHtml("#" + BandBg);
        ws.Range(row, 1, row, n).Style.Font.Bold = true;

        var tableEnd = row;
        var table = ws.Range(head, 1, tableEnd, n);
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.OutsideBorderColor = XLColor.FromHtml("#" + GridLine);
        table.Style.Border.InsideBorderColor = XLColor.FromHtml("#" + GridLine);

        // 固定列宽（与列序一一对应），保证打开后无需拖动列即可看全所有内容。
        // 序号/时间/类型/单号/客户/手机号/项目/技师/钟数/金额/支付方式/收银操作员/备注
        double[] widths = { 7.71, 26.29, 26.71, 25.43, 16, 19.45, 36, 27.71, 14.86, 15.57, 15.57, 23.29, 65 };
        for (var i = 0; i < n; i++) ws.Column(i + 1).Width = widths[i];
        ws.Column(7).Style.Alignment.WrapText = true;    // 项目：长文本换行
        ws.Column(13).Style.Alignment.WrapText = true;   // 备注：长文本换行
        ws.Rows(head + 1, tableEnd).AdjustToContents();  // 换行后按内容补足行高
        ws.SheetView.FreezeRows(head);

        return ToBytes(wb);
    }

    private static void CenterText(IXLCell c, string value)
    {
        c.Value = value;
        c.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
