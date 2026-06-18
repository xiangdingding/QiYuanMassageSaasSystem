<script lang="ts">
export default { name: 'MyReviewsView' };
</script>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import {
  NavBar as VanNavBar, PullRefresh as VanPullRefresh, Empty as VanEmpty,
  Rate as VanRate, Tag as VanTag
} from 'vant';
import { reviewsApi, type ServiceReviewDto } from '@/api/modules';

const list = ref<ServiceReviewDto[]>([]);
const loading = ref(false);
const refreshing = ref(false);

const avg = computed(() => {
  if (!list.value.length) return 0;
  const sum = list.value.reduce((s, r) => s + r.rating, 0);
  return Math.round((sum / list.value.length) * 10) / 10;
});

function parseTags(tags: string | null): string[] {
  if (!tags) return [];
  try {
    const arr = JSON.parse(tags);
    if (Array.isArray(arr)) return arr.map(String);
  } catch {
    /* 非 JSON 时按分隔符切 */
  }
  return tags.split(/[,，;；\s]+/).filter(Boolean);
}

async function load() {
  loading.value = true;
  try {
    list.value = await reviewsApi.me();
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
  <div class="qy-page reviews">
    <van-nav-bar title="我的评价" left-text="返回" left-arrow @click-left="$router.back()" />

    <van-pull-refresh v-model="refreshing" @refresh="load">
      <div v-if="list.length" class="summary qy-brand-bg">
        <div class="sm-score">{{ avg.toFixed(1) }}</div>
        <div class="sm-meta">
          <van-rate :model-value="avg" readonly allow-half size="16" color="#ffd21e" void-color="rgba(255,255,255,.5)" />
          <p>共 {{ list.length }} 条评价</p>
        </div>
      </div>

      <van-empty v-if="!loading && list.length === 0" description="还没有顾客评价" />

      <div v-for="r in list" :key="r.id" class="rv-item">
        <div class="rv-head">
          <van-rate :model-value="r.rating" readonly size="15" color="#ffb400" />
          <span class="rv-date">{{ r.createdAt.slice(0, 10) }}</span>
        </div>
        <div v-if="parseTags(r.tags).length" class="rv-tags">
          <van-tag v-for="(t, i) in parseTags(r.tags)" :key="i" plain type="primary">{{ t }}</van-tag>
        </div>
        <p v-if="r.comment" class="rv-comment">{{ r.comment }}</p>
        <p class="rv-from">— {{ r.memberName || '匿名顾客' }}</p>
      </div>
    </van-pull-refresh>
  </div>
</template>

<style scoped>
.summary {
  display: flex; align-items: center; gap: 16px;
  margin: 12px; padding: 18px; border-radius: 14px;
}
.sm-score { font-size: 40px; font-weight: 800; line-height: 1; }
.sm-meta p { margin: 6px 0 0; font-size: 13px; opacity: .9; }
.rv-item { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.rv-head { display: flex; align-items: center; justify-content: space-between; }
.rv-date { color: #b0b8c4; font-size: 13px; }
.rv-tags { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 10px; }
.rv-comment { margin: 10px 0 0; color: #1f2733; font-size: 14px; line-height: 1.6; }
.rv-from { margin: 8px 0 0; color: #98a2b3; font-size: 13px; text-align: right; }
</style>
