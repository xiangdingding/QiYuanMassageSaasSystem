<script lang="ts">
export default { name: 'ReviewsView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, Tabs as VanTabs, Tab as VanTab, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Tag as VanTag, Rate as VanRate, Button as VanButton, Popup as VanPopup,
  Field as VanField, Picker as VanPicker, Cell as VanCell, CellGroup as VanCellGroup,
  showSuccessToast, showToast
} from 'vant';
import { ordersApi, reviewsApi, staffApi, type ServiceReviewDto, type TechnicianServedItemDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';

const appStore = useAppStore();
const tabIndex = ref(0);
const rows = ref<ServiceReviewDto[]>([]);
const summary = ref<{ technicianId: number; technicianName: string; reviewCount: number; averageRating: number }[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const techList = ref<Staff[]>([]);

const techFilter = ref<number | null>(null);
const showTechFilterPicker = ref(false);
const techFilterColumns = computed(() => [{ text: '全部技师', value: 0 }, ...techList.value.map((t) => ({ text: `${t.realName || t.username}（${t.employeeNo ?? '—'}）`, value: t.id }))]);
function techFilterName() {
  const t = techList.value.find((x) => x.id === techFilter.value);
  return t ? (t.realName || t.username) : '全部技师';
}

function todayStr() { const d = new Date(); const p = (n: number) => String(n).padStart(2, '0'); return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`; }
function ratingLabel(r: number) { return ({ 5: '非常满意', 4: '满意', 3: '一般', 2: '不满意', 1: '非常不满意' } as Record<number, string>)[r] ?? ''; }
function parseTags(t: string | null): string[] { return t ? t.split(/[,，;；]+/).filter(Boolean) : []; }
function fmtTime(t: string | null) { return t ? t.slice(0, 16).replace('T', ' ') : ''; }

async function reload() {
  loading.value = true;
  try {
    if (tabIndex.value === 0) {
      const resp = await reviewsApi.list({ technicianId: techFilter.value || undefined, page: 1, pageSize: 50 });
      rows.value = resp.items;
    } else {
      summary.value = await reviewsApi.technicianSummary();
    }
  } catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}
function onTechFilter({ selectedValues }: { selectedValues: number[] }) {
  techFilter.value = selectedValues[0] || null;
  showTechFilterPicker.value = false;
  reload();
}

async function loadTechs() {
  if (!appStore.activeStoreId) return;
  const r = await staffApi.list({ role: 'Technician', storeId: appStore.activeStoreId, page: 1, pageSize: 200 }).catch(() => null);
  if (r) techList.value = r.items;
}

// 代客录入
const createOpen = ref(false);
const creating = ref(false);
const lookingUp = ref(false);
const lookedUp = ref(false);
const itemCandidates = ref<TechnicianServedItemDto[]>([]);
const showTechPicker = ref(false);
const tagOptions = ['手法专业', '力度合适', '态度热情', '环境整洁', '服务周到', '准时守约', '性价比高', '耐心细致'];
const cForm = reactive({
  technicianId: null as number | null, date: todayStr(), orderItemId: 0, orderId: 0,
  rating: 5, selectedTags: [] as string[], tags: '', comment: ''
});
const techColumns = computed(() => techList.value.map((t) => ({ text: `${t.realName || t.username}（${t.employeeNo ?? '—'}）`, value: t.id })));
function cTechName() { const t = techList.value.find((x) => x.id === cForm.technicianId); return t ? (t.realName || t.username) : '选择技师'; }
const canSubmit = computed(() => cForm.orderItemId > 0 && cForm.rating >= 1);

function openCreate() {
  Object.assign(cForm, { technicianId: null, date: todayStr(), orderItemId: 0, orderId: 0, rating: 5, selectedTags: [], tags: '', comment: '' });
  itemCandidates.value = []; lookedUp.value = false;
  createOpen.value = true;
}
function onCTechPicked({ selectedValues }: { selectedValues: number[] }) {
  cForm.technicianId = selectedValues[0] ?? null;
  showTechPicker.value = false;
  lookupItems();
}
async function lookupItems() {
  if (!appStore.activeStoreId || !cForm.technicianId || !cForm.date) return;
  lookingUp.value = true; cForm.orderItemId = 0; cForm.orderId = 0;
  try {
    itemCandidates.value = await ordersApi.itemsByTechnician(appStore.activeStoreId, cForm.technicianId, cForm.date);
    lookedUp.value = true;
  } catch { /* */ } finally { lookingUp.value = false; }
}
function pickItem(it: TechnicianServedItemDto) {
  if (it.hasReview) return;
  cForm.orderItemId = it.itemId; cForm.orderId = it.orderId;
}
function toggleTag(t: string) {
  const i = cForm.selectedTags.indexOf(t);
  if (i === -1) cForm.selectedTags.push(t); else cForm.selectedTags.splice(i, 1);
}
async function submitCreate() {
  if (cForm.orderItemId <= 0) { showToast('请选择被评价的服务项'); return; }
  const manual = cForm.tags.split(/[,，]/).map((s) => s.trim()).filter(Boolean);
  const allTags = [...cForm.selectedTags, ...manual];
  creating.value = true;
  try {
    await reviewsApi.submit({
      orderId: cForm.orderId, orderItemId: cForm.orderItemId, rating: cForm.rating,
      tags: allTags.length ? allTags.join(',') : null, comment: cForm.comment.trim() || null
    });
    showSuccessToast('已录入评价');
    createOpen.value = false;
    reload();
  } catch { /* */ } finally { creating.value = false; }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  await Promise.all([reload(), loadTechs()]);
});
</script>

<template>
  <div class="qy-page reviews">
    <van-nav-bar title="服务评价" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openCreate">代客录入</span></template>
    </van-nav-bar>

    <van-tabs v-model:active="tabIndex" @change="reload" sticky>
      <van-tab title="全部评价" />
      <van-tab title="技师汇总" />
    </van-tabs>

    <div v-if="tabIndex === 0" class="filter">
      <span class="f-label" @click="showTechFilterPicker = true">技师：{{ techFilterName() }} ▾</span>
    </div>

    <van-pull-refresh v-model="refreshing" @refresh="reload">
      <template v-if="tabIndex === 0">
        <van-empty v-if="!loading && rows.length === 0" description="暂无评价" />
        <div v-for="r in rows" :key="r.id" class="rv">
          <div class="rv-head">
            <span class="rv-tech">{{ r.technicianName }}</span>
            <van-rate :model-value="r.rating" readonly size="15" color="#ffb400" />
          </div>
          <div class="rv-sub">{{ ratingLabel(r.rating) }} · {{ r.memberName || '散客' }} · {{ fmtTime(r.createdAt) }}</div>
          <div v-if="parseTags(r.tags).length" class="rv-tags">
            <van-tag v-for="(t, i) in parseTags(r.tags)" :key="i" plain type="primary">{{ t }}</van-tag>
          </div>
          <p v-if="r.comment" class="rv-comment">{{ r.comment }}</p>
        </div>
      </template>

      <template v-else>
        <van-empty v-if="!loading && summary.length === 0" description="暂无汇总" />
        <div v-for="s in summary" :key="s.technicianId" class="sm">
          <span class="sm-name">{{ s.technicianName }}</span>
          <span class="sm-stat">{{ s.reviewCount }} 条 · 平均 <b>{{ s.averageRating?.toFixed(1) }}</b></span>
        </div>
      </template>
    </van-pull-refresh>

    <van-popup v-model:show="showTechFilterPicker" position="bottom" round>
      <van-picker :columns="techFilterColumns" @confirm="onTechFilter" @cancel="showTechFilterPicker = false" />
    </van-popup>

    <!-- 代客录入 -->
    <van-popup v-model:show="createOpen" position="bottom" round :style="{ maxHeight: '92%' }">
      <div class="sheet">
        <div class="sheet-title">代客录入评价</div>
        <van-cell-group inset>
          <van-field label="技师" :model-value="cTechName()" readonly is-link required @click="showTechPicker = true" />
          <van-field label="日期"><template #input><input v-model="cForm.date" type="date" class="dt" :max="todayStr()" @change="lookupItems" /></template></van-field>
        </van-cell-group>

        <div class="pick">
          <div class="pick-h">选择项目</div>
          <p v-if="!lookedUp" class="muted">请先选技师与日期，自动查询订单。</p>
          <p v-else-if="itemCandidates.length === 0" class="muted">该技师当日没有已完成服务项。</p>
          <label v-for="it in itemCandidates" :key="it.itemId" class="item" :class="{ disabled: it.hasReview, on: cForm.orderItemId === it.itemId }">
            <input type="radio" :value="it.itemId" :checked="cForm.orderItemId === it.itemId" :disabled="it.hasReview" @change="pickItem(it)" />
            <div class="it-info">
              <div class="it-svc">{{ it.serviceName }} <van-tag v-if="it.hasReview" type="success">已评价</van-tag></div>
              <div class="it-sub">{{ it.orderNo }} · {{ it.memberName || it.memberCardNo || '散客' }} · {{ fmtTime(it.completedAt) }}</div>
            </div>
          </label>
        </div>

        <van-cell-group inset>
          <van-field label="满意度">
            <template #input><van-rate v-model="cForm.rating" :count="5" /></template>
          </van-field>
        </van-cell-group>
        <div class="tags-pick">
          <div class="tp-h">常用标签</div>
          <button v-for="t in tagOptions" :key="t" type="button" class="chip" :class="{ on: cForm.selectedTags.includes(t) }" @click="toggleTag(t)">{{ t }}</button>
        </div>
        <van-cell-group inset>
          <van-field v-model="cForm.tags" label="其他标签" placeholder="逗号分隔" />
          <van-field v-model="cForm.comment" label="评论" type="textarea" rows="2" autosize maxlength="500" placeholder="客户原话/补充" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="creating" :disabled="!canSubmit" @click="submitCreate">提交评价</van-button></div>
      </div>
    </van-popup>
    <van-popup v-model:show="showTechPicker" position="bottom" round>
      <van-picker :columns="techColumns" @confirm="onCTechPicked" @cancel="showTechPicker = false" />
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.filter { padding: 10px 14px; }
.f-label { color: var(--qy-brand); font-size: 14px; }
.rv { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.rv-head { display: flex; align-items: center; justify-content: space-between; }
.rv-tech { font-weight: 600; font-size: 15px; }
.rv-sub { margin-top: 6px; color: #98a2b3; font-size: 13px; }
.rv-tags { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px; }
.rv-comment { margin: 8px 0 0; font-size: 14px; color: #1f2733; line-height: 1.5; }
.sm { display: flex; justify-content: space-between; align-items: center; background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.sm-name { font-weight: 600; }
.sm-stat { color: #6b7280; font-size: 14px; }
.sm-stat b { color: var(--qy-brand); }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.muted { color: #98a2b3; font-size: 13px; padding: 0 16px; }
.dt { border: none; outline: none; font-size: 14px; background: transparent; font-family: inherit; width: 100%; }
.pick { margin: 12px 16px; }
.pick-h { font-weight: 600; margin-bottom: 8px; }
.item { display: flex; align-items: center; gap: 10px; padding: 10px; border: 1px solid #eef1f4; border-radius: 10px; margin-bottom: 8px; }
.item.on { border-color: var(--qy-brand); box-shadow: 0 0 0 1px var(--qy-brand) inset; }
.item.disabled { opacity: .55; }
.it-svc { font-size: 14px; font-weight: 500; display: flex; align-items: center; gap: 6px; }
.it-sub { margin-top: 4px; color: #98a2b3; font-size: 12px; }
.tags-pick { margin: 12px 16px; }
.tp-h { font-weight: 600; margin-bottom: 8px; }
.chip { border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 16px; padding: 5px 12px; font-size: 13px; margin: 0 8px 8px 0; }
.chip.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
</style>
