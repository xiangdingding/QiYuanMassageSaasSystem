/**
 * 技师小程序「我的计时房」：
 * - 列出本店所有计时房 + 当前 session 状态
 * - 空闲 → 开台（填客户姓名即可，可空）
 * - 计时中 → 「通知前台结账」（只弹提示，不调接口；收钱在收银 PosView）
 */
import { timedRoomsApi, type RoomDto, type TimedRoomSessionDto } from '../../../utils/api';

interface RoomRow {
  id: number;
  roomNo: string;
  hourlyRate: number;
  hourlyRateText: string;       // "60.00 元/时"，便于读屏播报
  isOpen: boolean;              // 是否有 Open session
  sessionId: number | null;
  elapsedMinutes: number;
  customerName: string;
  statusText: string;
  ariaLabel: string;
}

Page({
  data: {
    rows: [] as RoomRow[],
    loading: false,
    storeId: 0,
    startVisible: false,
    startRoomId: 0,
    startRoomNo: '',
    startCustomerName: '',
    startRemark: '',
    starting: false
  },

  onShow() { this.refresh(); },

  async refresh() {
    const user = wx.getStorageSync('user');
    if (!user || !user.storeId) {
      wx.showToast({ title: '未登录或未绑定门店', icon: 'none' });
      return;
    }
    this.setData({ loading: true, storeId: user.storeId });
    try {
      const [rooms, sessions] = await Promise.all([
        timedRoomsApi.list(user.storeId),
        timedRoomsApi.sessions(user.storeId)
      ]);
      const timed = rooms.filter((r: RoomDto) => r.isTimedRoom && r.isActive);
      const rows: RoomRow[] = timed.map((r: RoomDto) => {
        const open = sessions.find((s: TimedRoomSessionDto) => s.roomId === r.id && s.status === 'Open');
        const customer = open?.customerName || open?.memberName || '散客';
        const minutes = open?.elapsedMinutes ?? 0;
        return {
          id: r.id,
          roomNo: r.roomNo,
          hourlyRate: r.hourlyRate,
          hourlyRateText: `${r.hourlyRate.toFixed(2)} 元每小时`,
          isOpen: !!open,
          sessionId: open?.id ?? null,
          elapsedMinutes: minutes,
          customerName: customer,
          statusText: open ? `计时中 ${minutes} 分钟` : '空闲',
          ariaLabel: open
            ? `${r.roomNo} 号计时房，${r.hourlyRate.toFixed(2)} 元每小时，已计时 ${minutes} 分钟，客人 ${customer}`
            : `${r.roomNo} 号计时房，${r.hourlyRate.toFixed(2)} 元每小时，当前空闲`
        };
      });
      this.setData({ rows });
    } catch {
      /* api 已 toast */
    } finally {
      this.setData({ loading: false });
    }
  },

  onTapStart(e: WechatMiniprogram.BaseEvent) {
    const id = Number(e.currentTarget.dataset.id);
    const row = this.data.rows.find((r) => r.id === id);
    if (!row) return;
    this.setData({
      startVisible: true,
      startRoomId: row.id,
      startRoomNo: row.roomNo,
      startCustomerName: '',
      startRemark: ''
    });
  },

  onStartCancel() {
    this.setData({ startVisible: false });
  },

  onStartCustomerInput(e: WechatMiniprogram.Input) {
    this.setData({ startCustomerName: e.detail.value });
  },

  onStartRemarkInput(e: WechatMiniprogram.Input) {
    this.setData({ startRemark: e.detail.value });
  },

  async onStartConfirm() {
    if (this.data.starting) return;
    this.setData({ starting: true });
    try {
      await timedRoomsApi.start(this.data.startRoomId, {
        customerName: this.data.startCustomerName.trim() || null,
        remark: this.data.startRemark.trim() || null
      });
      wx.showToast({ title: `${this.data.startRoomNo} 已开台`, icon: 'success' });
      this.setData({ startVisible: false });
      await this.refresh();
    } catch {
      /* api 已 toast */
    } finally {
      this.setData({ starting: false });
    }
  },

  /**
   * 通知前台结账：不调接口，只本地提示。
   * 计时结束 + 收钱是收银职责，避免技师小程序里再放支付方式选择。
   */
  onTapNotifyCashier(e: WechatMiniprogram.BaseEvent) {
    const id = Number(e.currentTarget.dataset.id);
    const row = this.data.rows.find((r) => r.id === id);
    if (!row) return;
    wx.showModal({
      title: '请到前台结账',
      content: `${row.roomNo} 号房当前已计时 ${row.elapsedMinutes} 分钟，请客人到前台结账，收银员收钱后会自动结束计时。`,
      showCancel: false,
      confirmText: '我知道了'
    });
  }
});
