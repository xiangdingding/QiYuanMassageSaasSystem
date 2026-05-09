Page({
  data: {
    userName: '',
    storeName: ''
  },
  onShow() {
    const user = wx.getStorageSync('user');
    if (user) {
      this.setData({
        userName: user.realName || user.username,
        storeName: user.storeName || ''
      });
    }
  },
  goHome() {
    wx.switchTab({ url: '/pages/tech/home/home' });
  },
  goPerformance() {
    wx.switchTab({ url: '/pages/tech/performance/performance' });
  },
  goLogin() {
    // TODO P5：登录页
    wx.showToast({ title: 'P5 阶段实现', icon: 'none' });
  }
});
