import { api } from '../../../utils/api';

interface ReviewDto {
  id: number;
  rating: number;
  tags: string | null;
  comment: string | null;
  memberName: string | null;
  createdAt: string;
}

interface ViewItem {
  id: number;
  stars: string;
  rating: number;
  tags: string;
  comment: string;
  memberLabel: string;
  timeText: string;
  ariaLabel: string;
}

const formatTime = (iso: string): string => {
  const d = new Date(iso);
  const pad = (n: number): string => (n < 10 ? `0${n}` : `${n}`);
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

Page({
  data: {
    items: [] as ViewItem[],
    loading: false,
    empty: false,
    avgText: ''
  },
  onShow() {
    this.load();
  },
  onPullDownRefresh() {
    this.load().finally(() => wx.stopPullDownRefresh());
  },
  async load() {
    this.setData({ loading: true });
    try {
      const list = await api.get<ReviewDto[]>('/reviews/me');
      const items: ViewItem[] = list.map((r) => {
        const stars = '★'.repeat(r.rating) + '☆'.repeat(5 - r.rating);
        const memberLabel = r.memberName || '匿名顾客';
        const tags = r.tags || '';
        const comment = r.comment || '';
        const timeText = formatTime(r.createdAt);
        return {
          id: r.id,
          stars,
          rating: r.rating,
          tags,
          comment,
          memberLabel,
          timeText,
          ariaLabel: `${memberLabel}评价${r.rating}星，${tags ? '标签：' + tags + '，' : ''}${comment || '无评论'}，${timeText}`
        };
      });
      const avg = list.length
        ? (list.reduce((a, b) => a + b.rating, 0) / list.length).toFixed(2)
        : '—';
      this.setData({
        items,
        empty: items.length === 0,
        avgText: list.length ? `平均 ${avg} 星，共 ${list.length} 条` : ''
      });
    } finally {
      this.setData({ loading: false });
    }
  }
});
