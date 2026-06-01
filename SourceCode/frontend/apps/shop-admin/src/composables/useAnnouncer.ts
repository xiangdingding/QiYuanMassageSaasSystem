/**
 * 全店公告播报（Web SpeechSynthesis）。
 *
 * 用于"叫号"这种主动通知场景：盲人技师在休息区候场不盯屏幕，必须主动出声。
 * 与读屏软件相互独立 —— 读屏只在用户焦点移动时朗读，叫号是广播式的，必须用 TTS。
 *
 * 中文用 zh-CN 语音；首次调用时浏览器可能因策略阻挡（需要用户已有交互），
 * 此处不抛错只静默；店内点过一次按钮后就常态可用。
 */

let cachedVoice: SpeechSynthesisVoice | null | undefined;

function pickChineseVoice(): SpeechSynthesisVoice | null {
  if (cachedVoice !== undefined) return cachedVoice;
  if (typeof window === 'undefined' || !window.speechSynthesis) {
    cachedVoice = null;
    return null;
  }
  const voices = window.speechSynthesis.getVoices();
  const v = voices.find((x) => x.lang.toLowerCase().startsWith('zh')) ?? null;
  cachedVoice = v;
  return v;
}

export function useAnnouncer() {
  function isSupported(): boolean {
    return typeof window !== 'undefined' && 'speechSynthesis' in window;
  }

  /// 朗读一段中文。`repeat` 控制连读次数，叫号场景常用 2 次确保休息区都听清。
  function speak(text: string, repeat = 1) {
    if (!isSupported() || !text) return;
    try {
      // 抢断上一条未读完的，避免叫号被堆积
      window.speechSynthesis.cancel();
      for (let i = 0; i < repeat; i++) {
        const utter = new SpeechSynthesisUtterance(text);
        utter.lang = 'zh-CN';
        utter.rate = 0.95;
        utter.pitch = 1;
        utter.volume = 1;
        const v = pickChineseVoice();
        if (v) utter.voice = v;
        window.speechSynthesis.speak(utter);
      }
    } catch { /* TTS 失败不影响业务流程 */ }
  }

  /// voices 在某些浏览器是异步加载的，提前预热避免首次叫号无声。
  function prewarm() {
    if (!isSupported()) return;
    if (window.speechSynthesis.getVoices().length > 0) {
      pickChineseVoice();
      return;
    }
    window.speechSynthesis.onvoiceschanged = () => {
      cachedVoice = undefined;
      pickChineseVoice();
    };
  }

  return { speak, prewarm, isSupported };
}
