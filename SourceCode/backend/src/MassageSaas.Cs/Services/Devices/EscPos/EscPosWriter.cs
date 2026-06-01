using System.Collections.Generic;
using System.Text;

namespace MassageSaas.Cs.Services.Devices.EscPos;

/// <summary>
/// 把 <see cref="ReceiptDocument"/> 排版成 ESC/POS 字节流。
/// 中文按 GBK 等编码写入；右对齐金额按"GBK 字节数 = 显示列宽"（ASCII 1 列、汉字 2 列）计算填充。
/// </summary>
public sealed class EscPosWriter
{
    // ESC/POS 指令
    private static readonly byte[] Init = { 0x1B, 0x40 };           // ESC @ 初始化
    private static readonly byte[] ChineseMode = { 0x1C, 0x26 };    // FS & 进入汉字模式
    private static readonly byte[] AlignLeft = { 0x1B, 0x61, 0x00 };
    private static readonly byte[] AlignCenter = { 0x1B, 0x61, 0x01 };
    private static readonly byte[] SizeNormal = { 0x1D, 0x21, 0x00 };
    private static readonly byte[] SizeDouble = { 0x1D, 0x21, 0x11 }; // 倍宽倍高
    private static readonly byte[] BoldOn = { 0x1B, 0x45, 0x01 };
    private static readonly byte[] BoldOff = { 0x1B, 0x45, 0x00 };
    private static readonly byte[] FeedAndCut = { 0x1D, 0x56, 0x00 }; // GS V 0 全切
    private static readonly byte[] DrawerKick = { 0x1B, 0x70, 0x00, 0x19, 0xFA }; // ESC p 踢钱箱

    private readonly Encoding _encoding;
    private readonly int _charsPerLine;

    public EscPosWriter(Encoding encoding, int charsPerLine)
    {
        _encoding = encoding;
        _charsPerLine = charsPerLine < 16 ? 32 : charsPerLine;
    }

    /// <summary>把一张小票排成完整字节流。</summary>
    public byte[] BuildReceipt(ReceiptDocument doc, bool cut)
    {
        var buf = new List<byte>(512);
        buf.AddRange(Init);
        buf.AddRange(ChineseMode);

        // 店名：居中、倍号、加粗
        buf.AddRange(AlignCenter);
        buf.AddRange(SizeDouble);
        buf.AddRange(BoldOn);
        Line(buf, doc.StoreName);
        buf.AddRange(BoldOff);
        buf.AddRange(SizeNormal);
        buf.AddRange(AlignLeft);

        Line(buf, $"订单：{doc.OrderNo}");
        Line(buf, $"时间：{doc.PrintedAt:yyyy-MM-dd HH:mm:ss}");
        Separator(buf);

        foreach (var i in doc.Items)
        {
            var nameLine = $"{i.ServiceName} ×{i.Quantity}次";
            if (i.PaidViaPunchCard) nameLine += "  [次卡]";
            Line(buf, nameLine);
            Line(buf, TwoColumns($"  {i.TechnicianName}", $"¥{i.Amount:F2}"));
        }

        Separator(buf);
        if (doc.Discount > 0)
            Line(buf, TwoColumns("优惠", $"-¥{doc.Discount:F2}"));
        buf.AddRange(BoldOn);
        Line(buf, TwoColumns("合计", $"¥{doc.Total:F2}"));
        buf.AddRange(BoldOff);
        Line(buf, TwoColumns($"实收（{doc.PayMethod}）", $"¥{doc.Paid:F2}"));
        if (doc.PunchCardUsedCount > 0)
            Line(buf, TwoColumns("消费次数（次卡）", $"{doc.PunchCardUsedCount} 次"));
        if (doc.Change > 0)
            Line(buf, TwoColumns("找零", $"¥{doc.Change:F2}"));

        buf.Add(0x0A);
        buf.AddRange(AlignCenter);
        Line(buf, "谢谢惠顾，欢迎下次光临");
        buf.AddRange(AlignLeft);

        // 走纸让内容越过刀口
        buf.AddRange(new byte[] { 0x0A, 0x0A, 0x0A, 0x0A });
        if (cut) buf.AddRange(FeedAndCut);

        return buf.ToArray();
    }

    /// <summary>单独的踢钱箱指令。</summary>
    public byte[] BuildDrawerKick() => (byte[])DrawerKick.Clone();

    private void Line(List<byte> buf, string text)
    {
        buf.AddRange(_encoding.GetBytes(text));
        buf.Add(0x0A);
    }

    private void Separator(List<byte> buf) => Line(buf, new string('-', _charsPerLine));

    /// <summary>左右两栏对齐到一行：左侧靠左、右侧靠右，中间补空格。</summary>
    private string TwoColumns(string left, string right)
    {
        var leftWidth = _encoding.GetByteCount(left);
        var rightWidth = _encoding.GetByteCount(right);
        var gap = _charsPerLine - leftWidth - rightWidth;
        return gap > 0 ? left + new string(' ', gap) + right : left + " " + right;
    }
}
