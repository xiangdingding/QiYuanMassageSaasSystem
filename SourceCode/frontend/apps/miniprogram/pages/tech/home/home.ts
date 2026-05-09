Page({
  data: {
    stateText: '空闲',
    todayRounds: 0,
    nextRoom: ''
  },
  speakStatus() {
    // 预留：P5 接入 getAccessibilityContext 或系统 TTS
    const { stateText, todayRounds, nextRoom } = this.data;
    wx.showToast({
      title: `当前${stateText}，今日已上钟${todayRounds}次${nextRoom ? `，下一间${nextRoom}` : ''}`,
      icon: 'none',
      duration: 3000
    });
  }
});
