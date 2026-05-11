<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">服务评价</span>
        <el-input-number v-model="ratingFilter" :min="1" :max="5" controls-position="right" placeholder="星级" style="width:120px" />
        <el-date-picker v-model="dateRange" type="daterange" range-separator="-" start-placeholder="开始" end-placeholder="结束" />
        <div class="spacer" />
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>

      <el-tabs v-model="tab" style="margin-top:8px" @tab-change="onTabChange">
        <el-tab-pane label="全部评价" name="list">
          <el-table :data="rows" v-loading="loading" stripe>
            <el-table-column prop="technicianName" label="技师" width="120" />
            <el-table-column prop="memberName" label="顾客" width="120" />
            <el-table-column prop="rating" label="评分" width="100">
              <template #default="{ row }">
                <span :aria-label="`${row.rating} 星`">{{ '★'.repeat(row.rating) }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="tags" label="标签" width="200" />
            <el-table-column prop="comment" label="评论" min-width="240" show-overflow-tooltip />
            <el-table-column prop="createdAt" label="时间" width="180" />
          </el-table>
        </el-tab-pane>
        <el-tab-pane label="技师汇总" name="summary">
          <el-table :data="summary" v-loading="loading" stripe>
            <el-table-column prop="technicianName" label="技师" width="160" />
            <el-table-column prop="reviewCount" label="评价数" width="120" />
            <el-table-column prop="averageRating" label="平均分" width="120" />
          </el-table>
        </el-tab-pane>
      </el-tabs>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import { Refresh } from '@element-plus/icons-vue';
import { reviewsApi, type ServiceReviewDto } from '@/api/modules';

const tab = ref<'list' | 'summary'>('list');
const rows = ref<ServiceReviewDto[]>([]);
const summary = ref<any[]>([]);
const loading = ref(false);
const ratingFilter = ref<number | undefined>(undefined);
const dateRange = ref<[Date, Date] | null>(null);

async function reload() {
  loading.value = true;
  try {
    const from = dateRange.value?.[0]?.toISOString();
    const to = dateRange.value?.[1]?.toISOString();
    if (tab.value === 'list') {
      rows.value = await reviewsApi.list({
        rating: ratingFilter.value,
        from, to
      });
    } else {
      const data = await reviewsApi.technicianSummary({ from, to }) as any[];
      summary.value = data;
    }
  } finally {
    loading.value = false;
  }
}

function onTabChange() { reload(); }
watch([ratingFilter, dateRange], () => reload());
onMounted(reload);
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; }
.toolbar .title { font-weight: 600; font-size: 16px; }
.spacer { flex: 1; }
</style>
