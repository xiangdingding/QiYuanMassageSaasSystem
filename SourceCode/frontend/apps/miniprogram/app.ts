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
  }
});

interface IAppOption {
  globalData: {
    apiBase: string;
    token?: string;
  };
}
