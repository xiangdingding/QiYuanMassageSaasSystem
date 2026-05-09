import { api, yuanToReadable } from '../../../utils/api';

interface PerfResp {
  todayAmount: number;
  todayCommission: number;
  monthAmount: number;
}

Page({
  data: {
    todayAmount: 0,
    todayCommission: 0,
    monthAmount: 0,
    todayAmountText: '0 元',
    todayCommissionText: '0 元',
    monthAmountText: '0 元',
    loading: false
  },
  onShow() {
    this.refresh();
  },
  async refresh() {
    const user = wx.getStorageSync('user');
    if (!user) {
      this.setData({
        todayAmountText: '未登录',
        todayCommissionText: '未登录',
        monthAmountText: '未登录'
      });
      return;
    }
    this.setData({ loading: true });
    try {
      const r = await api.get<PerfResp>('/reports/me/performance');
      this.setData({
        todayAmount: r.todayAmount,
        todayCommission: r.todayCommission,
        monthAmount: r.monthAmount,
        todayAmountText: yuanToReadable(r.todayAmount),
        todayCommissionText: yuanToReadable(r.todayCommission),
        monthAmountText: yuanToReadable(r.monthAmount)
      });
    } finally {
      this.setData({ loading: false });
    }
  },
  speakAll() {
    const { todayAmountText, todayCommissionText, monthAmountText } = this.data;
    wx.showToast({
      title: `今日${todayAmountText}，提成${todayCommissionText}，本月${monthAmountText}`,
      icon: 'none',
      duration: 4000
    });
  }
});
