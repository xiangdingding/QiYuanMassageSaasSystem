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
    changing: false
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
      this.setData({
        state: r.state,
        stateText: STATE_TEXT[r.state] ?? r.state,
        todayRounds: r.todayRoundCount,
        currentRoomNo: r.currentRoomNo ?? '',
        currentServiceName: r.currentServiceName ?? '',
        stateLoaded: true
      });
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
  goOnDuty()  { this.changeState('OnDuty', '已开始上钟，进入排队'); },
  goRest()    { this.changeState('Resting', '已切换到休息'); },
  goOff()     { this.changeState('OffDuty', '已下班'); },
  goReviews() {
    wx.navigateTo({ url: '/pages/tech/reviews/reviews' });
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
