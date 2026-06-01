import { api } from '../../../utils/api';

interface ReviewableItemDto {
  orderId: number;
  orderItemId: number;
  orderNo: string;
  technicianId: number;
  technicianName: string;
  serviceName: string;
  completedAt: string | null;
}

/** 列表项 + 本地填写状态（星级/标签/评论），每张卡独立。 */
interface ReviewCard extends ReviewableItemDto {
  dateText: string;
  rating: number;       // 0 = 未选
  ratingText: string;   // 朗读用："未评分" / "5 星"
  tags: string;
  comment: string;
  submitting: boolean;
}

const formatDate = (iso: string | null): string => {
  if (!iso) return '';
  const d = new Date(iso);
  const pad = (n: number): string => (n < 10 ? `0${n}` : `${n}`);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

Page({
  data: {
    starList: [1, 2, 3, 4, 5],
    items: [] as ReviewCard[],
    loading: false,
    empty: false,
    // 未绑定会员卡：评价资格依赖 openId↔会员卡绑定
    noOpenId: false
  },
  onShow() {
    this.load();
  },
  /** 拉取本人会员卡名下"已完成、未评价"的订单项。 */
  async load() {
    const openId = wx.getStorageSync('openId') as string | undefined;
    if (!openId) {
      this.setData({ noOpenId: true, empty: false, items: [], loading: false });
      return;
    }
    this.setData({ loading: true, noOpenId: false });
    try {
      const list = await api.get<ReviewableItemDto[]>('/storefront/reviewable', { openId });
      const items: ReviewCard[] = list.map((it) => ({
        ...it,
        dateText: formatDate(it.completedAt),
        rating: 0,
        ratingText: '未评分',
        tags: '',
        comment: '',
        submitting: false
      }));
      this.setData({ items, empty: items.length === 0 });
    } finally {
      this.setData({ loading: false });
    }
  },
  /** 点星打分：1-5。 */
  setRating(e: WechatMiniprogram.TouchEvent) {
    const index = Number(e.currentTarget.dataset.index);
    const star = Number(e.currentTarget.dataset.star);
    const items = this.data.items.slice();
    items[index].rating = star;
    items[index].ratingText = `${star} 星`;
    this.setData({ items });
  },
  onTags(e: WechatMiniprogram.Input) {
    const index = Number(e.currentTarget.dataset.index);
    const items = this.data.items.slice();
    items[index].tags = e.detail.value;
    this.setData({ items });
  },
  onComment(e: WechatMiniprogram.Input) {
    const index = Number(e.currentTarget.dataset.index);
    const items = this.data.items.slice();
    items[index].comment = e.detail.value;
    this.setData({ items });
  },
  async submit(e: WechatMiniprogram.TouchEvent) {
    const index = Number(e.currentTarget.dataset.index);
    const item = this.data.items[index];
    if (!item) return;
    if (item.rating < 1) {
      wx.showToast({ title: '请先选择星级', icon: 'none' });
      return;
    }
    const openId = wx.getStorageSync('openId') as string | undefined;
    if (!openId) {
      this.setData({ noOpenId: true });
      return;
    }
    const items = this.data.items.slice();
    items[index].submitting = true;
    this.setData({ items });
    try {
      await api.post('/reviews',
        {
          orderId: item.orderId,
          orderItemId: item.orderItemId,
          rating: item.rating,
          tags: item.tags.trim() || null,
          comment: item.comment.trim() || null
        },
        { noAuth: true, query: { openId } });
      wx.showToast({ title: '评价成功，谢谢', icon: 'success' });
      // 提交成功的项从列表移除，剩余项继续可评
      const remain = this.data.items.filter((_, i) => i !== index);
      this.setData({ items: remain, empty: remain.length === 0 });
    } catch (err: unknown) {
      // 错误已由 api 层 toast；恢复按钮可重试
      const restore = this.data.items.slice();
      if (restore[index]) {
        restore[index].submitting = false;
        this.setData({ items: restore });
      }
    }
  },
  /** 去"我的"页绑定会员卡。 */
  goBind() {
    wx.navigateTo({ url: '/pages/customer/my/my' });
  }
});
