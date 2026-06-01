import { api, requestSubscribe } from '../../../utils/api';

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

interface BoundMemberDto {
  memberId: number;
  cardNo: string;
  name?: string;
  balance: number;
  level: string;
}

interface StorefrontPackage {
  id: number;
  title: string;
  kind: string;
  serviceName?: string;
  remainCount: number;
  totalCount: number;
  expiresAt?: string;
  status: string;
}

interface StorefrontMember extends BoundMemberDto {
  packages: StorefrontPackage[];
}

interface BoundView {
  cardNo: string;
  name: string;
  balanceText: string;
  levelText: string;
}

interface PackageView {
  id: number;
  title: string;
  kindText: string;
  serviceText: string;
  countText: string;
  expiryText: string;
}

const STATUS_MAP: Record<string, string> = {
  Pending: '待确认',
  Confirmed: '已确认',
  Arrived: '已到店',
  Completed: '已完成',
  Cancelled: '已取消',
  NoShow: '未到店'
};

const LEVEL_MAP: Record<string, string> = {
  Regular: '普通卡',
  Silver: '银卡',
  Gold: '金卡',
  Diamond: '钻石卡'
};

const formatArrive = (iso: string): string => {
  const d = new Date(iso);
  const pad = (n: number): string => (n < 10 ? `0${n}` : `${n}`);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
};

const toBoundView = (m: BoundMemberDto): BoundView => ({
  cardNo: m.cardNo,
  name: m.name || '未填写姓名',
  balanceText: `${m.balance.toFixed(2)} 元`,
  levelText: LEVEL_MAP[m.level] ?? m.level
});

const toPackageView = (p: StorefrontPackage): PackageView => {
  const isCounter = p.kind === 'Counter';
  const countText = isCounter
    ? `剩 ${p.remainCount} / ${p.totalCount} 次`
    : p.totalCount > 0
      ? `期限内剩 ${p.remainCount} 次`
      : '期限内不限次';
  return {
    id: p.id,
    title: p.title,
    kindText: isCounter ? '计次卡' : '期限卡',
    serviceText: p.serviceName || '不限服务项目',
    countText,
    expiryText: p.expiresAt ? `${p.expiresAt.slice(0, 10)} 到期` : '无到期日'
  };
};

Page({
  data: {
    phone: '',
    items: [] as ViewItem[],
    loading: false,
    empty: false,
    // 会员卡绑定
    storeName: '',
    storeId: 0,
    bound: null as BoundView | null,
    packages: [] as PackageView[],
    binding: false
  },
  onShow() {
    const lastStore = wx.getStorageSync('lastStore') as { id: number; name: string } | undefined;
    const cached = wx.getStorageSync('boundMember') as BoundMemberDto | undefined;
    this.setData({
      storeId: lastStore?.id ?? 0,
      storeName: lastStore?.name ?? '',
      bound: cached ? toBoundView(cached) : null
    });
    // 已绑定的，进页面就拉一次实时余额与套餐
    if (cached) this.refreshMember();
  },
  onPhone(e: WechatMiniprogram.Input) {
    this.setData({ phone: e.detail.value });
  },
  /** 按缓存的 openId 拉取会员卡实时余额与在用套餐。 */
  async refreshMember() {
    const openId = wx.getStorageSync('openId') as string | undefined;
    if (!openId) return;
    try {
      const m = await api.get<StorefrontMember>('/storefront/member', { openId });
      wx.setStorageSync('boundMember', {
        memberId: m.memberId, cardNo: m.cardNo, name: m.name, balance: m.balance, level: m.level
      });
      this.setData({
        bound: toBoundView(m),
        packages: (m.packages ?? []).map(toPackageView)
      });
    } catch (e: unknown) {
      // 拉取失败保留缓存展示，不打断页面
    }
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
  /**
   * 绑定会员卡：把当前微信账号与门店下手机号匹配的会员卡关联。
   * 绑定后充值到账、生日祝福、会员卡到期等通知会推到微信。
   */
  async bindMember() {
    if (!/^1\d{10}$/.test(this.data.phone)) {
      wx.showToast({ title: '请先输入会员卡预留手机号', icon: 'none' });
      return;
    }
    if (!this.data.storeId) {
      wx.showToast({ title: '请先扫门店二维码进入预约页一次', icon: 'none' });
      return;
    }
    const openId = wx.getStorageSync('openId') as string | undefined;
    if (!openId) {
      wx.showToast({ title: '微信登录中，请稍后重试', icon: 'none' });
      getApp<IAppOption>().ensureWeChatOpenId();
      return;
    }
    // 申请充值 / 生日 / 到期三类订阅授权——须在首个 await 前，否则微信拦截
    await requestSubscribe(['RechargeArrived', 'MemberBirthday', 'MemberPackageExpiring']);

    this.setData({ binding: true });
    try {
      const m = await api.post<BoundMemberDto>('/wechat/bind-member', {
        storeId: this.data.storeId,
        phone: this.data.phone,
        openId
      }, { noAuth: true });
      wx.setStorageSync('boundMember', m);
      this.setData({ bound: toBoundView(m) });
      wx.showToast({ title: '绑定成功', icon: 'success' });
      this.refreshMember();
    } catch (e: unknown) {
      // 错误已由 api 层 toast
    } finally {
      this.setData({ binding: false });
    }
  },
  unbind() {
    wx.removeStorageSync('boundMember');
    this.setData({ bound: null, packages: [] });
    wx.showToast({ title: '已解除显示', icon: 'none' });
  },
  /** 跳转服务评价页（对已完成、未评价的服务打分）。 */
  goReview() {
    wx.navigateTo({ url: '/pages/customer/review/review' });
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
