<script lang="ts">
export default { name: 'MyPayrollView' };
</script>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import {
  NavBar as VanNavBar, PullRefresh as VanPullRefresh, Empty as VanEmpty,
  Collapse as VanCollapse, CollapseItem as VanCollapseItem,
  Cell as VanCell, CellGroup as VanCellGroup, Tag as VanTag
} from 'vant';
import { payrollApi, type PayrollItemDto } from '@/api/modules';

const list = ref<PayrollItemDto[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const active = ref<string | number>('');

function fmt(n?: number | null): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

async function load() {
  loading.value = true;
  try {
    list.value = await payrollApi.me(12);
    // 默认展开最新一期
    if (list.value.length) active.value = list.value[0].id;
  } catch {
    /* http 拦截器已提示 */
  } finally {
    loading.value = false;
    refreshing.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="qy-page payroll">
    <van-nav-bar title="我的工资" left-text="返回" left-arrow @click-left="$router.back()" />

    <van-pull-refresh v-model="refreshing" @refresh="load">
      <van-empty v-if="!loading && list.length === 0" description="暂无工资记录" />

      <van-collapse v-else v-model="active" accordion>
        <van-collapse-item v-for="p in list" :key="p.id" :name="p.id">
          <template #title>
            <div class="ci-title">
              <span class="ci-net qy-money">¥{{ fmt(p.netTotal) }}</span>
              <span class="ci-round">{{ p.servedRoundCount }} 钟</span>
            </div>
          </template>

          <van-cell-group inset>
            <van-cell title="底薪" :value="`¥ ${fmt(p.baseSalary)}`" />
            <van-cell title="服务提成" :value="`¥ ${fmt(p.commissionTotal)}`" />
            <van-cell v-if="p.referralCommissionTotal" title="推荐提成" :value="`¥ ${fmt(p.referralCommissionTotal)}`" />
            <van-cell v-if="p.tipsTotal" title="小费" :value="`¥ ${fmt(p.tipsTotal)}`" />
            <van-cell v-if="p.overtimeAmount" title="加班费" :value="`¥ ${fmt(p.overtimeAmount)}（${p.overtimeHours}时）`" />
            <van-cell v-if="p.attendanceBonus" title="全勤奖" :value="`¥ ${fmt(p.attendanceBonus)}`" />
            <van-cell v-if="p.adjustmentTotal" title="调整合计"
              :value="`${p.adjustmentTotal >= 0 ? '+' : ''}¥ ${fmt(p.adjustmentTotal)}`" />
            <van-cell title="出勤 / 请假" :value="`${p.scheduledDays} 天 / ${p.leaveDays} 天`" />
            <van-cell title="实发合计">
              <template #value><b class="qy-money net">¥ {{ fmt(p.netTotal) }}</b></template>
            </van-cell>
          </van-cell-group>

          <div v-if="p.adjustments.length" class="adj">
            <p class="adj-h">明细调整</p>
            <div v-for="a in p.adjustments" :key="a.id" class="adj-row">
              <van-tag :type="a.amount >= 0 ? 'success' : 'danger'">{{ a.amount >= 0 ? '加' : '减' }}</van-tag>
              <span class="adj-reason">{{ a.reason }}</span>
              <span class="adj-amt qy-money">{{ a.amount >= 0 ? '+' : '' }}¥{{ fmt(a.amount) }}</span>
            </div>
          </div>
          <p v-if="p.remark" class="remark">备注：{{ p.remark }}</p>
        </van-collapse-item>
      </van-collapse>

      <p v-if="list.length" class="tip">仅展示最近若干期工资，按门店发薪周期生成。</p>
    </van-pull-refresh>
  </div>
</template>

<style scoped>
.ci-title { display: flex; align-items: baseline; gap: 12px; }
.ci-net { font-size: 18px; font-weight: 700; color: var(--qy-brand); }
.ci-round { color: #98a2b3; font-size: 13px; }
.net { color: var(--qy-brand); font-size: 16px; }
.adj { margin: 12px 16px 0; }
.adj-h { color: #8a94a6; font-size: 13px; margin: 0 0 8px; }
.adj-row { display: flex; align-items: center; gap: 8px; padding: 6px 0; font-size: 14px; }
.adj-reason { flex: 1; color: #4b5563; }
.adj-amt { font-weight: 600; }
.remark { margin: 10px 16px 0; color: #8a94a6; font-size: 13px; }
.tip { color: #b0b8c4; font-size: 12px; text-align: center; padding: 14px 24px 20px; }
</style>
