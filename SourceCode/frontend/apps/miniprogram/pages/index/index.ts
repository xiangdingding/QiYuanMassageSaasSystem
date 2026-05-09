interface StoredUser {
  username: string;
  realName?: string;
  storeName?: string;
  storeId?: number;
  role: string;
}

Page({
  data: {
    userName: '',
    storeName: '',
    storeId: 0,
    role: ''
  },
  onShow() {
    const user = wx.getStorageSync('user') as StoredUser | undefined;
    if (user) {
      this.setData({
        userName: user.realName || user.username,
        storeName: user.storeName || '',
        storeId: user.storeId || 0,
        role: user.role
      });
    } else {
      this.setData({ userName: '', storeName: '', storeId: 0, role: '' });
    }
  },
  goHome() {
    wx.switchTab({ url: '/pages/tech/home/home' });
  },
  goPerformance() {
    wx.switchTab({ url: '/pages/tech/performance/performance' });
  },
  goBook() {
    const { storeId, storeName } = this.data;
    if (!storeId) {
      wx.showToast({ title: '请先扫描门店二维码或登录', icon: 'none' });
      return;
    }
    wx.navigateTo({
      url: `/pages/customer/book/book?storeId=${storeId}&storeName=${encodeURIComponent(storeName)}`
    });
  },
  goMyAppointments() {
    wx.navigateTo({ url: '/pages/customer/my/my' });
  },
  goLogin() {
    wx.navigateTo({ url: '/pages/auth/login/login' });
  }
});
