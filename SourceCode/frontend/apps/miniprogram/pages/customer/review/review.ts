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

/** 常用标签：点选多选，不够再手动补充。 */
const TAG_OPTIONS = ['手法专业', '力度合适', '态度热情', '环境整洁', '服务周到', '准时守约', '性价比高', '耐心细致'];

/** 列表项 + 本地填写状态（满意度/标签/评论），每张卡独立。 */
interface ReviewCard extends ReviewableItemDto {
  dateText: string;
  rating: number;       // 0 = 未选
  ratingText: string;   // 朗读用："未评分" / "非常满意"
  tagChips: { label: string; on: boolean }[];  // 常用标签点选态
  tags: string;         // 手动补充的其他标签
  comment: string;
  submitting: boolean;
}

const formatDate = (iso: string | null): string => {
  if (!iso) return '';
  const d = new Date(iso);
  const pad = (n: number): string => (n < 10 ? `0${n}` : `${n}`);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

/** 满意度档位 → 评分（1-5）。底层仍存整数，UI 只呈现文字，便于读屏与无歧义。 */
const RATING_OPTIONS = [
  { value: 5, label: '非常满意' },
  { value: 4, label: '满意' },
  { value: 3, label: '一般' },
  { value: 2, label: '不满意' },
  { value: 1, label: '非常不满意' }
];
const RATING_LABELS: Record<number, string> = RATING_OPTIONS.reduce(
  (m, o) => ((m[o.value] = o.label), m),
  {} as Record<number, string>
);

Page({
  data: {
    ratingOptions: RATING_OPTIONS,
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
        tagChips: TAG_OPTIONS.map((label) => ({ label, on: false })),
        tags: '',
        comment: '',
        submitting: false
      }));
      this.setData({ items, empty: items.length === 0 });
    } finally {
      this.setData({ loading: false });
    }
  },
  /** 选满意度档位（对应评分 1-5）。 */
  setRating(e: WechatMiniprogram.TouchEvent) {
    const index = Number(e.currentTarget.dataset.index);
    const value = Number(e.currentTarget.dataset.value);
    const items = this.data.items.slice();
    items[index].rating = value;
    items[index].ratingText = RATING_LABELS[value] ?? '未评分';
    this.setData({ items });
  },
  /** 点选/取消常用标签。 */
  toggleTag(e: WechatMiniprogram.TouchEvent) {
    const index = Number(e.currentTarget.dataset.index);
    const ci = Number(e.currentTarget.dataset.ci);
    const items = this.data.items.slice();
    const chips = items[index].tagChips.slice();
    chips[ci] = { ...chips[ci], on: !chips[ci].on };
    items[index].tagChips = chips;
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
    // 点选标签 + 手动补充，合并为逗号分隔
    const picked = item.tagChips.filter((c) => c.on).map((c) => c.label);
    const manual = item.tags.split(/[,，]/).map((s) => s.trim()).filter(Boolean);
    const allTags = [...picked, ...manual];
    try {
      await api.post('/reviews',
        {
          orderId: item.orderId,
          orderItemId: item.orderItemId,
          rating: item.rating,
          tags: allTags.length > 0 ? allTags.join(',') : null,
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
