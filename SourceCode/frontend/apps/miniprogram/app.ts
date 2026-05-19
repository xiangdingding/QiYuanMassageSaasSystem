interface SessionResp {
  openId: string;
}

App<IAppOption>({
  globalData: {
    apiBase: 'http://localhost:5000/api'
  },
  onLaunch() {
    // 无障碍自检：首次进入提醒用户启用屏幕阅读器（仅首次）
    wx.getStorage({
      key: 'a11y_notified',
      success: () => {},
      fail: () => {
        wx.setStorageSync('a11y_notified', true);
      }
    });
    this.ensureWeChatOpenId();
  },
  /**
   * 静默换取微信 OpenId 并缓存。预约下单、绑定会员卡都要带上它，
   * 绑定后才能收到充值到账 / 生日 / 会员卡到期等订阅消息。
   * OpenId 对同一小程序稳定不变，取到一次即长期缓存。
   * 这里用裸 wx.request：失败不弹 toast，openId 缺失只影响通知绑定，不打扰用户。
   */
  ensureWeChatOpenId() {
    if (wx.getStorageSync('openId')) return;
    wx.login({
      success: (res) => {
        if (!res.code) return;
        wx.request({
          url: `${this.globalData.apiBase}/wechat/session`,
          method: 'POST',
          data: { code: res.code },
          header: { 'Content-Type': 'application/json' },
          success: (r) => {
            if (r.statusCode >= 200 && r.statusCode < 300) {
              const openId = (r.data as unknown as SessionResp | undefined)?.openId;
              if (openId) wx.setStorageSync('openId', openId);
            }
          }
        });
      }
    });
  }
});

interface IAppOption {
  globalData: {
    apiBase: string;
    token?: string;
  };
  ensureWeChatOpenId(): void;
}
