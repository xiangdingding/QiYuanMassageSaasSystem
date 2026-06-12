using System.Net;
using System.Text.Json;
using System.Windows;
using Refit;

namespace MassageSaas.Cs.Services;

public static class ErrorReporter
{
    public static void Show(Exception ex)
    {
        var (title, msg) = Parse(ex);
        // 登录失效（401）：不只弹提示，确定后还要登出并跳回登录界面
        if (ex is ApiException { StatusCode: HttpStatusCode.Unauthorized })
        {
            MassageSaas.Cs.App.HandleSessionExpired(msg);
            return;
        }
        MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public static (string title, string message) Parse(Exception ex)
    {
        if (ex is ApiException api)
        {
            var serverMsg = TryReadServerMessage(api.Content);
            if (api.StatusCode == HttpStatusCode.Unauthorized)
                return ("登录已失效", serverMsg ?? "请重新登录");
            if (api.StatusCode == HttpStatusCode.Forbidden)
                return ("权限不足", serverMsg ?? "无权访问该接口或订阅已到期");
            // 业务冲突（409，如"该日已存在该员工的排班"）：与 BS 一致，直接展示后端提示，标题用中性"提示"
            if (api.StatusCode == HttpStatusCode.Conflict && serverMsg is not null)
                return ("提示", serverMsg);
            return ($"请求失败 ({(int)api.StatusCode})", serverMsg ?? api.ReasonPhrase ?? api.Message);
        }
        return ("出错", ex.Message);
    }

    private static string? TryReadServerMessage(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;
        try
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.TryGetProperty("message", out var m)) return m.GetString();
            if (doc.RootElement.TryGetProperty("title", out var t)) return t.GetString();
        }
        catch { /* not json */ }
        return null;
    }
}
