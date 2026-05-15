import { api, yuanToReadable } from '../../../utils/api';

interface AdjustmentDto {
  id: number;
  kind: string;
  amount: number;
  reason: string;
}

interface PayrollItemDto {
  id: number;
  baseSalary: number;
  commissionTotal: number;
  tipsTotal: number;
  overtimeHours: number;
  overtimeAmount: number;
  attendanceBonus: number;
  adjustmentTotal: number;
  netTotal: number;
  servedRoundCount: number;
  scheduledDays: number;
  leaveDays: number;
  remark: string | null;
  adjustments: AdjustmentDto[];
  // 服务端没直接给月份，需要从 /payroll/me 调整：先用 createdAt 推断不靠谱，所以
  // 这里直接展示净额；月份信息暂时不展示（如果后端 DTO 调整再加）。
}

interface ViewItem {
  id: number;
  ariaLabel: string;
  baseText: string;
  commissionText: string;
  overtimeText: string;
  attendanceText: string;
  adjustmentText: string;
  tipsText: string;
  netText: string;
  netReadable: string;
  rounds: number;
  scheduled: number;
  leave: number;
  remark: string;
  adjustments: { id: number; line: string }[];
}

Page({
  data: {
    items: [] as ViewItem[],
    loading: false,
    empty: false
  },
  onShow() {
    this.load();
  },
  onPullDownRefresh() {
    this.load().finally(() => wx.stopPullDownRefresh());
  },
  async load() {
    const user = wx.getStorageSync('user');
    if (!user) {
      this.setData({ items: [], empty: true });
      return;
    }
    this.setData({ loading: true });
    try {
      const list = await api.get<PayrollItemDto[]>('/payroll/me', { take: 6 });
      const items: ViewItem[] = list.map((p) => {
        const netReadable = yuanToReadable(p.netTotal);
        const tipsLine = p.tipsTotal > 0 ? `，本月小费 ${yuanToReadable(p.tipsTotal)}（不计入工资）` : '';
        const adjLine = p.adjustmentTotal !== 0
          ? `，调整 ${p.adjustmentTotal >= 0 ? '加' : '减'} ${yuanToReadable(Math.abs(p.adjustmentTotal))}`
          : '';
        const overtimeLine = p.overtimeAmount > 0
          ? `，加班 ${p.overtimeHours} 小时合 ${yuanToReadable(p.overtimeAmount)}`
          : '';
        const attendanceLine = p.attendanceBonus > 0
          ? `，满勤奖 ${yuanToReadable(p.attendanceBonus)}`
          : '';
        return {
          id: p.id,
          ariaLabel: `工资单：底薪 ${yuanToReadable(p.baseSalary)}，提成 ${yuanToReadable(p.commissionTotal)}${overtimeLine}${attendanceLine}${adjLine}${tipsLine}，到手 ${netReadable}，本月服务 ${p.servedRoundCount} 钟`,
          baseText: `底薪 ¥${p.baseSalary.toFixed(2)}`,
          commissionText: `提成 ¥${p.commissionTotal.toFixed(2)}`,
          overtimeText: p.overtimeAmount > 0
            ? `加班 ${p.overtimeHours}h ¥${p.overtimeAmount.toFixed(2)}`
            : '',
          attendanceText: p.attendanceBonus > 0 ? `满勤 ¥${p.attendanceBonus.toFixed(2)}` : '',
          adjustmentText: p.adjustmentTotal !== 0
            ? `调整 ${p.adjustmentTotal >= 0 ? '+' : '-'}¥${Math.abs(p.adjustmentTotal).toFixed(2)}`
            : '',
          tipsText: p.tipsTotal > 0 ? `小费 ¥${p.tipsTotal.toFixed(2)}` : '',
          netText: `¥${p.netTotal.toFixed(2)}`,
          netReadable,
          rounds: p.servedRoundCount,
          scheduled: p.scheduledDays,
          leave: p.leaveDays,
          remark: p.remark ?? '',
          adjustments: p.adjustments.map((a) => ({
            id: a.id,
            line: `${a.kind === 'Bonus' ? '奖金' : '扣款'} ${a.kind === 'Bonus' ? '+' : '-'}¥${a.amount.toFixed(2)}：${a.reason}`
          }))
        };
      });
      this.setData({ items, empty: items.length === 0 });
    } finally {
      this.setData({ loading: false });
    }
  },
  speak(e: WechatMiniprogram.TouchEvent) {
    const id = Number(e.currentTarget.dataset.id);
    const item = this.data.items.find((x) => x.id === id);
    if (!item) return;
    wx.showToast({ title: item.ariaLabel, icon: 'none', duration: 5000 });
  }
});
