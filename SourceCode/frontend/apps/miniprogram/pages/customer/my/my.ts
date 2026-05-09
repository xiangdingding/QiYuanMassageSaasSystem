import { api } from '../../../utils/api';

interface AppointmentDto {
  id: number;
  storeName: string;
  expectedArriveAt: string;
  partySize: number;
  status: string;
}

interface ViewItem {
  id: number;
  storeName: string;
  arriveText: string;
  partySize: number;
  status: string;
  statusText: string;
  cancellable: boolean;
}

const STATUS_MAP: Record<string, string> = {
  Pending: '待确认',
  Confirmed: '已确认',
  Arrived: '已到店',
  Completed: '已完成',
  Cancelled: '已取消',
  NoShow: '未到店'
};

const formatArrive = (iso: string): string => {
  const d = new Date(iso);
  const pad = (n: number): string => (n < 10 ? `0${n}` : `${n}`);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

Page({
  data: {
    phone: '',
    items: [] as ViewItem[],
    loading: false,
    empty: false
  },
  onPhone(e: WechatMiniprogram.Input) {
    this.setData({ phone: e.detail.value });
  },
  async query() {
    if (!/^1\d{10}$/.test(this.data.phone)) {
      wx.showToast({ title: '请输入正确手机号', icon: 'none' });
      return;
    }
    this.setData({ loading: true, empty: false });
    try {
      const list = await api.get<AppointmentDto[]>('/appointments/by-customer', { phone: this.data.phone });
      const items: ViewItem[] = list.map((a) => ({
        id: a.id,
        storeName: a.storeName,
        arriveText: formatArrive(a.expectedArriveAt),
        partySize: a.partySize,
        status: a.status,
        statusText: STATUS_MAP[a.status] ?? a.status,
        cancellable: a.status === 'Pending' || a.status === 'Confirmed'
      }));
      this.setData({ items, empty: items.length === 0 });
    } finally {
      this.setData({ loading: false });
    }
  },
  cancel(e: WechatMiniprogram.TouchEvent) {
    const id = Number(e.currentTarget.dataset.id);
    wx.showModal({
      title: '确认取消？',
      content: '取消后该预约时段会被释放',
      success: async (res) => {
        if (!res.confirm) return;
        try {
          await api.post(`/appointments/${id}/cancel`,
            { reason: '顾客主动取消' },
            { noAuth: true, query: { phone: this.data.phone } });
          wx.showToast({ title: '已取消', icon: 'none' });
          this.query();
        } catch (e: unknown) {
          // 错误已经被 api 层 toast
        }
      }
    });
  }
});
