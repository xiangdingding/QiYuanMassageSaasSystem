using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MassageSaas.Api.Json;

/// <summary>
/// 全系统时间统一为北京时间（UTC+8，中国无夏令时，用固定偏移）。
/// 存储层一律用 UTC（DateTime.UtcNow）；本转换器只作用于 API 边界：
/// - 输出（Write）：把 UTC 转成北京时间，写成不带偏移的 ISO 串 "yyyy-MM-ddTHH:mm:ss"，
///   各端按本地解析后展示的就是北京时间数字。
/// - 输入（Read）：带 Z/偏移的按真实瞬间取 UTC；不带偏移的视为北京时间，减 8 小时存 UTC。
/// </summary>
public sealed class BeijingDateTimeConverter : JsonConverter<DateTime>
{
    internal static readonly TimeSpan Offset = TimeSpan.FromHours(8);
    private const string Format = "yyyy-MM-ddTHH:mm:ss";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s)) return default;
        if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return default;
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,                                              // 带 Z
            DateTimeKind.Local => dt.ToUniversalTime(),                          // 带 +08:00 等偏移
            _ => DateTime.SpecifyKind(dt - Offset, DateTimeKind.Utc)             // 无偏移：视为北京时间
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        writer.WriteStringValue((utc + Offset).ToString(Format, CultureInfo.InvariantCulture));
    }
}

/// <summary>可空 DateTime 的北京时间转换器（System.Text.Json 不会自动复用 DateTime 的转换器）。</summary>
public sealed class BeijingNullableDateTimeConverter : JsonConverter<DateTime?>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        var s = reader.GetString();
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (!DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return null;
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt - BeijingDateTimeConverter.Offset, DateTimeKind.Utc)
        };
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        var v = value.Value;
        var utc = v.Kind == DateTimeKind.Local
            ? v.ToUniversalTime()
            : DateTime.SpecifyKind(v, DateTimeKind.Utc);
        writer.WriteStringValue((utc + BeijingDateTimeConverter.Offset).ToString(Format, CultureInfo.InvariantCulture));
    }
}
