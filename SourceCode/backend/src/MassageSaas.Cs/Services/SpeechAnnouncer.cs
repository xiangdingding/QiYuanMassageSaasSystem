using System.Speech.Synthesis;

namespace MassageSaas.Cs.Services;

/// <summary>
/// 语音播报。盲人收银员/店长依赖这个朗读关键事件（结账成功、找零、叫号、转钟）。
/// 走 System.Speech.Synthesis（SAPI），无需联网；与系统读屏（NVDA/Narrator）共存。
/// </summary>
public interface ISpeechAnnouncer
{
    /// <summary>用户在设置里关闭后整个组件静音。</summary>
    bool Enabled { get; set; }
    void Say(string text);
    void SayAsync(string text);
}

public sealed class SpeechAnnouncer : ISpeechAnnouncer, IDisposable
{
    private readonly SpeechSynthesizer? _synth;

    public SpeechAnnouncer()
    {
        try
        {
            _synth = new SpeechSynthesizer { Rate = 1, Volume = 90 };
            _synth.SetOutputToDefaultAudioDevice();
            // 优先选中文音色（系统至少装有"Microsoft Huihui"等）
            var zh = _synth.GetInstalledVoices()
                .FirstOrDefault(v => v.VoiceInfo.Culture?.Name?.StartsWith("zh") == true);
            if (zh is not null) _synth.SelectVoice(zh.VoiceInfo.Name);
        }
        catch
        {
            _synth = null; // 系统无 TTS 引擎时降级为静默
        }
    }

    public bool Enabled { get; set; } = true;

    public void Say(string text)
    {
        if (!Enabled || _synth is null || string.IsNullOrWhiteSpace(text)) return;
        try { _synth.Speak(text); } catch { /* 忽略播报失败 */ }
    }

    public void SayAsync(string text)
    {
        if (!Enabled || _synth is null || string.IsNullOrWhiteSpace(text)) return;
        try
        {
            _synth.SpeakAsyncCancelAll();
            _synth.SpeakAsync(text);
        }
        catch { /* 忽略 */ }
    }

    public void Dispose() => _synth?.Dispose();
}
