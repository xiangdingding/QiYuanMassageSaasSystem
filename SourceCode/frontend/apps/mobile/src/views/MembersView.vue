<script lang="ts">
export default { name: 'MembersView' };
</script>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import {
  Search as VanSearch, List as VanList, PullRefresh as VanPullRefresh, Empty as VanEmpty,
  Tag as VanTag, Popup as VanPopup, Cell as VanCell, CellGroup as VanCellGroup, Divider as VanDivider
} from 'vant';
import { membersApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Member } from '@/api/types';

const appStore = useAppStore();

const keyword = ref('');
const list = ref<Member[]>([]);
const loading = ref(false);
const finished = ref(false);
const refreshing = ref(false);
const page = ref(1);
const pageSize = 20;

const detail = ref<Member | null>(null);
const showDetail = ref(false);

function fmt(n?: number | null): string {
  return (n ?? 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

async function onLoad() {
  if (!appStore.activeStoreId) {
    finished.value = true;
    return;
  }
  loading.value = true;
  try {
    const res = await membersApi.list({
      page: page.value,
      pageSize,
      keyword: keyword.value.trim() || undefined,
      storeId: appStore.activeStoreId
    });
    list.value.push(...res.items);
    page.value += 1;
    if (list.value.length >= res.total || res.items.length === 0) finished.value = true;
  } catch {
    finished.value = true;
  } finally {
    loading.value = false;
  }
}

function reset() {
  list.value = [];
  page.value = 1;
  finished.value = false;
}

async function onRefresh() {
  reset();
  await onLoad();
  refreshing.value = false;
}

function onSearch() {
  reset();
  onLoad();
}

async function openDetail(m: Member) {
  detail.value = m;
  showDetail.value = true;
  // 拉取最新明细（余额可能变动）
  try {
    detail.value = await membersApi.get(m.id);
  } catch {
    /* 用列表数据兜底 */
  }
}

onMounted(() => {
  if (!appStore.stores.length) appStore.loadStores().catch(() => undefined);
});
</script>

<template>
  <div class="qy-page members">
    <div class="search-bar">
      <van-search
        v-model="keyword"
        placeholder="搜索姓名 / 手机号 / 卡号"
        @search="onSearch"
        @clear="onSearch"
      />
    </div>

    <van-pull-refresh v-model="refreshing" @refresh="onRefresh">
      <van-empty v-if="finished && list.length === 0" description="暂无会员" />
      <van-list
        v-else
        v-model:loading="loading"
        :finished="finished"
        finished-text="没有更多了"
        @load="onLoad"
      >
        <div v-for="m in list" :key="m.id" class="member-item" @click="openDetail(m)">
          <div class="mi-left">
            <div class="mi-name">
              {{ m.name || '未命名' }}
              <van-tag v-if="m.memberTypeName" plain type="primary">{{ m.memberTypeName }}</van-tag>
              <van-tag v-if="!m.isActive" type="default">已停用</van-tag>
            </div>
            <div class="mi-sub">{{ m.phone }} · 卡号 {{ m.cardNo }}</div>
          </div>
          <div class="mi-right">
            <template v-if="m.memberTypeKind === 'CountBased'">
              <b>{{ m.remainCount ?? 0 }}</b><span>剩余次数</span>
            </template>
            <template v-else>
              <b class="qy-money">¥{{ fmt(m.balance) }}</b><span>余额</span>
            </template>
          </div>
        </div>
      </van-list>
    </van-pull-refresh>

    <van-popup v-model:show="showDetail" position="bottom" round :style="{ maxHeight: '80%' }">
      <div v-if="detail" class="detail">
        <div class="detail-head">
          <div class="dh-name">{{ detail.name || '未命名' }}</div>
          <div class="dh-phone">{{ detail.phone }}</div>
        </div>
        <van-cell-group inset>
          <van-cell title="卡号" :value="detail.cardNo" />
          <van-cell title="会员类型" :value="detail.memberTypeName || '普通'" />
          <van-cell v-if="detail.memberTypeKind === 'CountBased'" title="剩余次数" :value="String(detail.remainCount ?? 0)" />
          <van-cell v-else title="卡内余额" :value="`¥ ${fmt(detail.balance)}`" />
          <van-cell title="累计充值" :value="`¥ ${fmt(detail.totalRecharge)}`" />
          <van-cell title="累计消费" :value="`¥ ${fmt(detail.totalConsumed)}`" />
          <van-cell v-if="detail.cardExpiresAt" title="卡到期"
            :value="`${detail.cardExpiresAt.slice(0,10)}${detail.cardDaysRemaining != null ? `（剩${detail.cardDaysRemaining}天）` : ''}`" />
          <van-cell title="状态" :value="detail.isActive ? '正常' : '已停用'" />
        </van-cell-group>
        <van-divider>仅查看。开卡/充值/退卡等写操作请在收银台或网页/桌面端办理。</van-divider>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.members { background: var(--qy-bg); }
.search-bar { position: sticky; top: 0; z-index: 2; }
.member-item {
  display: flex; align-items: center; justify-content: space-between;
  background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px;
}
.mi-name { font-size: 16px; font-weight: 600; display: flex; align-items: center; gap: 6px; }
.mi-sub { margin-top: 6px; color: #98a2b3; font-size: 13px; }
.mi-right { text-align: right; }
.mi-right b { display: block; font-size: 17px; color: var(--qy-brand); }
.mi-right span { font-size: 12px; color: #b0b8c4; }
.detail { padding: 20px 0 30px; }
.detail-head { text-align: center; margin-bottom: 14px; }
.dh-name { font-size: 20px; font-weight: 700; }
.dh-phone { color: #98a2b3; margin-top: 4px; }
</style>
