import { api } from '../../../utils/api';

interface MyQueueDto {
  id: number | null;
  technicianId: number;
  state: string;
  queuePosition: number;
  todayRoundCount: number;
  enteredAt: string | null;
  lastCalledAt: string | null;
  currentRoomNo: string | null;
  currentOrderId: number | null;
  currentServiceName: string | null;
  currentCustomerName: string | null;
  currentCustomerGender: string | null;
  currentCustomerPreferences: string | null;
  currentCustomerHealth: string | null;
  currentCustomerHasNotes: boolean;
}

const STATE_TEXT: Record<string, string> = {
  Idle: '空闲',
  OnDuty: '上钟中',
  Resting: '休息中',
  OffDuty: '下班'
};

Page({
  data: {
    state: 'OffDuty',
    stateText: '未登录',
    todayRounds: 0,
    currentRoomNo: '',
    currentServiceName: '',
    stateLoaded: false,
    changing: false,
    customerName: '',
    customerGender: '',
    customerPreferences: '',
    customerHealth: '',
    hasBriefing: false,
    briefingAria: '',
    acknowledgedOrderId: 0,
    acknowledged: false
  },
  onShow() {
    this.refresh();
  },
  async refresh() {
    const user = wx.getStorageSync('user');
    if (!user) {
      this.setData({
        state: 'OffDuty', stateText: '未登录', todayRounds: 0,
        currentRoomNo: '', currentServiceName: '', stateLoaded: false
      });
      return;
    }
    try {
      const r = await api.get<MyQueueDto>('/queue/me');
      const briefingAria = r.currentCustomerHasNotes
        ? `上钟前必读：${r.currentCustomerName || '本位'}${r.currentCustomerGender ? '，' + r.currentCustomerGender : ''}。`
          + (r.currentCustomerHealth ? `健康注意：${r.currentCustomerHealth}。` : '')
          + (r.currentCustomerPreferences ? `偏好：${r.currentCustomerPreferences}。` : '')
        : '';
      const sameOrder = r.currentOrderId !== null && r.currentOrderId === this.data.acknowledgedOrderId;
      this.setData({
        state: r.state,
        stateText: STATE_TEXT[r.state] ?? r.state,
        todayRounds: r.todayRoundCount,
        currentRoomNo: r.currentRoomNo ?? '',
        currentServiceName: r.currentServiceName ?? '',
        stateLoaded: true,
        customerName: r.currentCustomerName ?? '',
        customerGender: r.currentCustomerGender ?? '',
        customerPreferences: r.currentCustomerPreferences ?? '',
        customerHealth: r.currentCustomerHealth ?? '',
        hasBriefing: r.currentCustomerHasNotes,
        briefingAria,
        // 切换到新订单时重置已确认状态；同一订单刷新保留已确认
        acknowledgedOrderId: sameOrder ? this.data.acknowledgedOrderId : (r.currentOrderId ?? 0),
        acknowledged: sameOrder ? this.data.acknowledged : false
      });
      // 有客户必读且尚未确认 → 主动语音提醒
      if (r.currentCustomerHasNotes && !sameOrder) {
        wx.showToast({ title: '有上钟前必读事项，请先查看', icon: 'none', duration: 3500 });
        wx.vibrateShort({ type: 'medium' });
      }
    } catch {
      this.setData({ stateLoaded: true });
    }
  },
  speakStatus() {
    const { stateText, todayRounds, currentRoomNo, currentServiceName } = this.data;
    const room = currentRoomNo ? `，当前在${currentRoomNo}号房做${currentServiceName || '服务'}` : '，目前没有在服务的客人';
    wx.showToast({
      title: `当前${stateText}，今日已上钟${todayRounds}次${room}`,
      icon: 'none',
      duration: 4000
    });
  },
  speakBriefing() {
    if (!this.data.hasBriefing) return;
    wx.showToast({ title: this.data.briefingAria, icon: 'none', duration: 6000 });
  },
  ackBriefing() {
    this.setData({ acknowledged: true });
    wx.showToast({ title: '已确认，可以进房间', icon: 'success', duration: 2000 });
  },
  goOnDuty()  { this.changeState('OnDuty', '已开始上钟，进入排队'); },
  goRest()    { this.changeState('Resting', '已切换到休息'); },
  goOff()     { this.changeState('OffDuty', '已下班'); },
  goReviews() {
    wx.navigateTo({ url: '/pages/tech/reviews/reviews' });
  },
  goPayroll() {
    wx.navigateTo({ url: '/pages/tech/payroll/payroll' });
  },
  goTimedRooms() {
    wx.navigateTo({ url: '/pages/tech/timed-rooms/timed-rooms' });
  },
  async changeState(state: string, ok: string) {
    if (this.data.changing) return;
    this.setData({ changing: true });
    try {
      await api.post('/queue/me/state', { state });
      wx.showToast({ title: ok, icon: 'none', duration: 2500 });
      await this.refresh();
    } catch {
      /* api 已 toast */
    } finally {
      this.setData({ changing: false });
    }
  }
});
