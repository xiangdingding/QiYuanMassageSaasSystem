function yuanToReadable(amount: number): string {
  // 避免小数点引起朗读歧义："328元5角" 优于 "¥328.5"
  const yuan = Math.floor(amount);
  const jiao = Math.round((amount - yuan) * 10);
  return jiao === 0 ? `${yuan} 元` : `${yuan} 元 ${jiao} 角`;
}

Page({
  data: {
    todayAmount: 0,
    todayCommission: 0,
    monthAmount: 0,
    todayAmountText: '0 元',
    todayCommissionText: '0 元',
    monthAmountText: '0 元'
  },
  onShow() {
    // P5：替换为真实 API
    const today = 328.5;
    const todayCommission = 98.5;
    const month = 8450;
    this.setData({
      todayAmount: today,
      todayCommission,
      monthAmount: month,
      todayAmountText: yuanToReadable(today),
      todayCommissionText: yuanToReadable(todayCommission),
      monthAmountText: yuanToReadable(month)
    });
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
