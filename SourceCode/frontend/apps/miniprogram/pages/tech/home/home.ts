import { api } from '../../../utils/api';

interface QueueRow {
  technicianId: number;
  state: string;
  todayRoundCount: number;
  queuePosition: number;
}

const STATE_TEXT: Record<string, string> = {
  Idle: '空闲',
  OnDuty: '上钟中',
  Resting: '休息中',
  OffDuty: '下班'
};

Page({
  data: {
    stateText: '未登录',
    todayRounds: 0,
    nextRoom: '',
    loading: false
  },
  onShow() {
    this.refresh();
  },
  async refresh() {
    const user = wx.getStorageSync('user');
    if (!user) {
      this.setData({ stateText: '未登录', todayRounds: 0, nextRoom: '' });
      return;
    }
    this.setData({ loading: true });
    try {
      const queue = await api.get<QueueRow>('/queue/me').catch((): QueueRow | null => null);
      this.setData({
        stateText: queue ? (STATE_TEXT[queue.state] ?? queue.state) : '未排班',
        todayRounds: queue?.todayRoundCount ?? 0,
        // 下一个房间号当前由收银前端推送给技师，小程序仅展示语音播报状态
        nextRoom: ''
      });
    } finally {
      this.setData({ loading: false });
    }
  },
  speakStatus() {
    const { stateText, todayRounds, nextRoom } = this.data;
    wx.showToast({
      title: `当前${stateText}，今日已上钟${todayRounds}次${nextRoom ? `，下一间${nextRoom}` : ''}`,
      icon: 'none',
      duration: 3000
    });
  }
});
