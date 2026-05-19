import { api, requestSubscribe } from '../../../utils/api';

interface AppointmentResp {
  id: number;
  storeName: string;
  expectedArriveAt: string;
  status: string;
}

interface ServiceItem {
  id: number;
  name: string;
  durationMinutes: number;
  price: number;
  memberPrice: number;
  description?: string;
}

interface Technician {
  id: number;
  name: string;
  level: string;
  isBlind: boolean;
  specialties?: string;
}

const pad = (n: number): string => (n < 10 ? `0${n}` : `${n}`);
const todayStr = (): string => {
  const d = new Date();
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
};
const defaultTimeStr = (): string => {
  const d = new Date(Date.now() + 60 * 60 * 1000);
  return `${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

const LEVEL_LABEL: Record<string, string> = {
  Junior: '初级',
  Senior: '中级',
  Master: '高级'
};

Page({
  data: {
    storeId: 0,
    storeName: '',
    form: {
      customerName: '',
      customerPhone: '',
      date: todayStr(),
      time: defaultTimeStr(),
      remark: ''
    },
    partyOptions: ['1 人', '2 人', '3 人', '4 人', '5 人及以上'],
    partyIndex: 0,
    // 服务项：第 0 项为"到店再选"，其后对应 services
    services: [] as ServiceItem[],
    serviceLabels: ['到店再选'],
    serviceIndex: 0,
    // 技师：第 0 项为"不指定"，其后对应 technicians
    technicians: [] as Technician[],
    techLabels: ['不指定技师（听轮号安排）'],
    techIndex: 0,
    loading: false,
    error: ''
  },
  onLoad(query: Record<string, string | undefined>) {
    const storeId = Number(query.storeId ?? 0);
    const storeName = query.storeName ?? '';
    this.setData({ storeId, storeName });
    // 记住最近到访门店，供"我的预约"页绑定会员卡时定位租户
    if (storeId) {
      wx.setStorageSync('lastStore', { id: storeId, name: storeName });
      this.loadStorefront(storeId);
    }
  },
  /** 拉取门店服务菜单与技师列表，失败不阻断（仍可只填基本信息预约）。 */
  async loadStorefront(storeId: number) {
    try {
      const [services, technicians] = await Promise.all([
        api.get<ServiceItem[]>('/storefront/services', { storeId }),
        api.get<Technician[]>('/storefront/technicians', { storeId })
      ]);
      this.setData({
        services,
        serviceLabels: ['到店再选'].concat(
          services.map((s) => `${s.name}　${s.durationMinutes}分钟　¥${s.price}`)
        ),
        technicians,
        techLabels: ['不指定技师（听轮号安排）'].concat(
          technicians.map((t) => {
            const lv = LEVEL_LABEL[t.level] ?? t.level;
            return `${t.name}（${lv}${t.isBlind ? '·盲人技师' : ''}）`;
          })
        )
      });
    } catch {
      // 菜单拉取失败：保留默认项，顾客仍可下单
    }
  },
  onName(e: WechatMiniprogram.Input) {
    this.setData({ 'form.customerName': e.detail.value });
  },
  onPhone(e: WechatMiniprogram.Input) {
    this.setData({ 'form.customerPhone': e.detail.value });
  },
  onService(e: WechatMiniprogram.PickerChange) {
    this.setData({ serviceIndex: Number(e.detail.value) });
  },
  onTech(e: WechatMiniprogram.PickerChange) {
    this.setData({ techIndex: Number(e.detail.value) });
  },
  onDate(e: WechatMiniprogram.PickerChange) {
    this.setData({ 'form.date': e.detail.value as string });
  },
  onTime(e: WechatMiniprogram.PickerChange) {
    this.setData({ 'form.time': e.detail.value as string });
  },
  onParty(e: WechatMiniprogram.PickerChange) {
    this.setData({ partyIndex: Number(e.detail.value) });
  },
  onRemark(e: WechatMiniprogram.Input) {
    this.setData({ 'form.remark': e.detail.value });
  },
  async submit() {
    const { storeId, form, partyIndex, services, serviceIndex, technicians, techIndex } = this.data;
    if (!storeId) { this.setData({ error: '门店未指定，请从首页门店列表进入' }); return; }
    if (!form.customerName || !form.customerPhone) {
      this.setData({ error: '请填写姓名与手机号' });
      return;
    }
    if (!/^1\d{10}$/.test(form.customerPhone)) {
      this.setData({ error: '手机号格式不正确' });
      return;
    }
    const arriveLocal = new Date(`${form.date}T${form.time}:00`);
    if (Number.isNaN(arriveLocal.getTime()) || arriveLocal.getTime() < Date.now() - 5 * 60 * 1000) {
      this.setData({ error: '到店时间不能早于当前时间' });
      return;
    }

    // 申请"预约提醒"订阅授权——须在首个 await 前，否则微信拦截
    await requestSubscribe(['AppointmentReminder']);

    this.setData({ loading: true, error: '' });
    try {
      const partySize = partyIndex >= 4 ? 5 : partyIndex + 1;
      const openId = wx.getStorageSync('openId') || undefined;
      const serviceId = serviceIndex > 0 ? services[serviceIndex - 1].id : undefined;
      const preferredTechnicianId = techIndex > 0 ? technicians[techIndex - 1].id : undefined;
      const resp = await api.post<AppointmentResp>('/appointments', {
        storeId,
        serviceId,
        preferredTechnicianId,
        customerName: form.customerName,
        customerPhone: form.customerPhone,
        customerOpenId: openId,
        expectedArriveAt: arriveLocal.toISOString(),
        partySize,
        remark: form.remark || undefined
      }, { noAuth: true });
      wx.showModal({
        title: '预约成功',
        content: `${resp.storeName}\n预计到店：${form.date} ${form.time}\n请按时到店签到。`,
        showCancel: false,
        confirmText: '好的',
        success: () => wx.navigateBack({ delta: 1 })
      });
    } catch (e: unknown) {
      this.setData({ error: e instanceof Error ? e.message : '提交失败' });
    } finally {
      this.setData({ loading: false });
    }
  }
});
