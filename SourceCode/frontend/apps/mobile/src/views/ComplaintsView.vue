<script lang="ts">
export default { name: 'ComplaintsView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, Tabs as VanTabs, Tab as VanTab, PullRefresh as VanPullRefresh,
  Empty as VanEmpty, Tag as VanTag, Button as VanButton, Popup as VanPopup, Switch as VanSwitch,
  Field as VanField, Picker as VanPicker, Cell as VanCell, CellGroup as VanCellGroup,
  showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import { complaintsApi, ordersApi, staffApi, type ComplaintDto, type TechnicianServedItemDto } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Staff } from '@/api/types';

const appStore = useAppStore();
const rows = ref<ComplaintDto[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const techList = ref<Staff[]>([]);

const statusTab = ref(0);
const statuses: { label: string; value: string }[] = [
  { label: '待处理', value: 'Pending' }, { label: '已处理', value: 'Resolved' },
  { label: '已取消', value: 'Cancelled' }, { label: '全部', value: '' }
];

function todayStr() { const d = new Date(); const p = (n: number) => String(n).padStart(2, '0'); return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`; }
function statusLabel(s: string) { return ({ Pending: '待处理', Resolved: '已处理', Cancelled: '已取消' } as Record<string, string>)[s] ?? s; }
function statusType(s: string) { return s === 'Pending' ? 'warning' : s === 'Resolved' ? 'success' : 'default'; }
function resolutionLabel(r: string) { return ({ Reassigned: '改派', Refunded: '退款', Apologized: '道歉/补偿', NoAction: '不予处理' } as Record<string, string>)[r] ?? r; }
function parseTags(t: string | null): string[] { return t ? t.split(/[,，]+/).filter(Boolean) : []; }
function fmtTime(t: string | null) { return t ? t.slice(0, 16).replace('T', ' ') : ''; }

async function reload() {
  loading.value = true;
  try {
    const res = await complaintsApi.list({
      storeId: appStore.activeStoreId ?? undefined,
      status: statuses[statusTab.value].value || undefined,
      page: 1, pageSize: 50
    });
    rows.value = res.items;
  } catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}
async function loadTechs() {
  if (!appStore.activeStoreId) return;
  const r = await staffApi.list({ role: 'Technician', storeId: appStore.activeStoreId, page: 1, pageSize: 200 }).catch(() => null);
  if (r) techList.value = r.items;
}

// 登记投诉
const createOpen = ref(false);
const creating = ref(false);
const lookingUp = ref(false);
const lookedUp = ref(false);
const itemCandidates = ref<TechnicianServedItemDto[]>([]);
const showTechPicker = ref(false);
const tagOptions = ['态度差', '力度不合适', '技术生疏', '迟到/超时', '卫生不佳', '环境嘈杂', '乱收费', '中途离岗'];
const cForm = reactive({
  anonymous: false, technicianId: null as number | null, date: todayStr(),
  orderItemId: 0, selectedTags: [] as string[], tags: '', comment: ''
});
const techColumns = computed(() => techList.value.map((t) => ({ text: `${t.realName || t.username}（${t.employeeNo ?? '—'}）`, value: t.id })));
function cTechName() { const t = techList.value.find((x) => x.id === cForm.technicianId); return t ? (t.realName || t.username) : '选择技师'; }
const canSubmit = computed(() => cForm.anonymous ? true : cForm.orderItemId > 0);

function openCreate() {
  Object.assign(cForm, { anonymous: false, technicianId: null, date: todayStr(), orderItemId: 0, selectedTags: [], tags: '', comment: '' });
  itemCandidates.value = []; lookedUp.value = false;
  createOpen.value = true;
}
function onCTechPicked({ selectedValues }: { selectedValues: number[] }) {
  cForm.technicianId = selectedValues[0] ?? null;
  showTechPicker.value = false;
  if (!cForm.anonymous) lookupItems();
}
async function lookupItems() {
  if (!appStore.activeStoreId || !cForm.technicianId || !cForm.date) return;
  lookingUp.value = true; cForm.orderItemId = 0;
  try {
    itemCandidates.value = await ordersApi.itemsByTechnician(appStore.activeStoreId, cForm.technicianId, cForm.date);
    lookedUp.value = true;
  } catch { /* */ } finally { lookingUp.value = false; }
}
function pickItem(it: TechnicianServedItemDto) { if (!it.hasPendingComplaint) cForm.orderItemId = it.itemId; }
function toggleTag(t: string) { const i = cForm.selectedTags.indexOf(t); if (i === -1) cForm.selectedTags.push(t); else cForm.selectedTags.splice(i, 1); }
async function submitCreate() {
  if (!cForm.anonymous && cForm.orderItemId <= 0) { showToast('请选择被投诉的服务项'); return; }
  const manual = cForm.tags.split(/[,，]/).map((s) => s.trim()).filter(Boolean);
  const allTags = [...cForm.selectedTags, ...manual];
  const tags = allTags.length ? allTags.join(',') : null;
  creating.value = true;
  try {
    await complaintsApi.create(cForm.anonymous
      ? { orderItemId: null, storeId: appStore.activeStoreId, technicianId: cForm.technicianId, tags, comment: cForm.comment.trim() || null }
      : { orderItemId: cForm.orderItemId, tags, comment: cForm.comment.trim() || null });
    showSuccessToast('已登记投诉');
    createOpen.value = false;
    reload();
  } catch { /* */ } finally { creating.value = false; }
}

// 处理投诉
const resolveOpen = ref(false);
const saving = ref(false);
const resolving = ref<ComplaintDto | null>(null);
const showReassignPicker = ref(false);
const rForm = reactive({ resolution: 'Reassigned', reassignedToTechnicianId: null as number | null, resolutionNote: '' });
const resolutionOptions = computed(() => {
  const base = [{ v: 'Apologized', label: '道歉/补偿' }, { v: 'NoAction', label: '不予处理' }];
  return resolving.value?.orderItemId ? [{ v: 'Reassigned', label: '改派' }, { v: 'Refunded', label: '退款' }, ...base] : base;
});
const reassignColumns = computed(() => techList.value.filter((t) => t.id !== resolving.value?.originalTechnicianId).map((t) => ({ text: `${t.realName || t.username}（${t.employeeNo ?? '—'}）`, value: t.id })));
function reassignName() { const t = techList.value.find((x) => x.id === rForm.reassignedToTechnicianId); return t ? (t.realName || t.username) : '选择新技师'; }
function openResolve(row: ComplaintDto) {
  resolving.value = row;
  rForm.resolution = row.orderItemId ? 'Reassigned' : 'Apologized';
  rForm.reassignedToTechnicianId = null;
  rForm.resolutionNote = '';
  resolveOpen.value = true;
}
function onReassignPicked({ selectedValues }: { selectedValues: number[] }) {
  rForm.reassignedToTechnicianId = selectedValues[0] ?? null;
  showReassignPicker.value = false;
}
async function submitResolve() {
  if (!resolving.value) return;
  if (rForm.resolution === 'Reassigned' && !rForm.reassignedToTechnicianId) { showToast('请选择改派目标'); return; }
  saving.value = true;
  try {
    await complaintsApi.resolve(resolving.value.id, {
      resolution: rForm.resolution,
      reassignedToTechnicianId: rForm.resolution === 'Reassigned' ? rForm.reassignedToTechnicianId : null,
      resolutionNote: rForm.resolutionNote.trim() || null
    });
    showSuccessToast('已处理');
    resolveOpen.value = false;
    reload();
  } catch { /* */ } finally { saving.value = false; }
}
async function cancelOne(row: ComplaintDto) {
  try { await showConfirmDialog({ title: '取消投诉', message: `确认取消这条投诉记录？` }); } catch { return; }
  try { await complaintsApi.cancel(row.id); showSuccessToast('已取消'); reload(); } catch { /* */ }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  await Promise.all([reload(), loadTechs()]);
});
</script>

<template>
  <div class="qy-page complaints">
    <van-nav-bar title="投诉处理" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openCreate">登记</span></template>
    </van-nav-bar>

    <van-tabs v-model:active="statusTab" @change="reload" sticky>
      <van-tab v-for="s in statuses" :key="s.value" :title="s.label" />
    </van-tabs>

    <van-pull-refresh v-model="refreshing" @refresh="reload">
      <van-empty v-if="!loading && rows.length === 0" description="暂无投诉" />
      <div v-for="c in rows" :key="c.id" class="cp">
        <div class="cp-head">
          <span class="cp-svc">{{ c.serviceName || '匿名投诉' }}<span v-if="c.originalTechnicianName" class="cp-tech"> · {{ c.originalTechnicianName }}</span></span>
          <van-tag :type="statusType(c.status)">{{ statusLabel(c.status) }}</van-tag>
        </div>
        <div v-if="c.orderNo" class="cp-order">{{ c.orderNo }}<span v-if="c.memberName"> · {{ c.memberName }}</span></div>
        <div v-if="parseTags(c.tags).length" class="cp-tags">
          <van-tag v-for="(t, i) in parseTags(c.tags)" :key="i" type="warning" plain>{{ t }}</van-tag>
        </div>
        <p v-if="c.comment" class="cp-comment">{{ c.comment }}</p>
        <div v-if="c.resolution" class="cp-resolution">
          处理：{{ resolutionLabel(c.resolution) }}<span v-if="c.reassignedToTechnicianName"> → {{ c.reassignedToTechnicianName }}</span>
          <span v-if="c.resolvedByName" class="muted"> · {{ c.resolvedByName }} {{ fmtTime(c.resolvedAt) }}</span>
        </div>
        <div class="cp-foot">
          <span class="muted">登记 {{ c.recordedByName || '—' }} · {{ fmtTime(c.createdAt) }}</span>
          <div v-if="c.status === 'Pending'" class="cp-actions">
            <van-button size="mini" type="primary" @click="openResolve(c)">处理</van-button>
            <van-button size="mini" type="danger" plain @click="cancelOne(c)">取消</van-button>
          </div>
        </div>
      </div>
    </van-pull-refresh>

    <!-- 登记投诉 -->
    <van-popup v-model:show="createOpen" position="bottom" round :style="{ maxHeight: '92%' }">
      <div class="sheet">
        <div class="sheet-title">登记投诉</div>
        <van-cell-group inset>
          <van-cell title="不指定项目" label="打开后仅记录文字，处理时只能道歉/补偿或不予处理">
            <template #right-icon><van-switch v-model="cForm.anonymous" /></template>
          </van-cell>
          <van-field label="被投诉技师" :model-value="cTechName()" readonly is-link @click="showTechPicker = true" />
          <van-field v-if="!cForm.anonymous" label="日期"><template #input><input v-model="cForm.date" type="date" class="dt" :max="todayStr()" @change="lookupItems" /></template></van-field>
        </van-cell-group>

        <div v-if="!cForm.anonymous" class="pick">
          <div class="pick-h">选择项目</div>
          <p v-if="!lookedUp" class="muted">请先选技师与日期。</p>
          <p v-else-if="itemCandidates.length === 0" class="muted">该技师当日没有已完成服务项。</p>
          <label v-for="it in itemCandidates" :key="it.itemId" class="item" :class="{ disabled: it.hasPendingComplaint, on: cForm.orderItemId === it.itemId }">
            <input type="radio" :value="it.itemId" :checked="cForm.orderItemId === it.itemId" :disabled="it.hasPendingComplaint" @change="pickItem(it)" />
            <div class="it-info">
              <div class="it-svc">{{ it.serviceName }} <van-tag v-if="it.hasPendingComplaint" type="warning">已投诉</van-tag></div>
              <div class="it-sub">{{ it.orderNo }} · {{ it.memberName || it.memberCardNo || '散客' }} · ¥{{ it.amount.toFixed(2) }}</div>
            </div>
          </label>
        </div>

        <div class="tags-pick">
          <div class="tp-h">常用标签</div>
          <button v-for="t in tagOptions" :key="t" type="button" class="chip" :class="{ on: cForm.selectedTags.includes(t) }" @click="toggleTag(t)">{{ t }}</button>
        </div>
        <van-cell-group inset>
          <van-field v-model="cForm.tags" label="其他标签" placeholder="逗号分隔" />
          <van-field v-model="cForm.comment" label="描述" type="textarea" rows="2" autosize maxlength="500" placeholder="客户原话/补充" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="creating" :disabled="!canSubmit" @click="submitCreate">登记投诉</van-button></div>
      </div>
    </van-popup>
    <van-popup v-model:show="showTechPicker" position="bottom" round>
      <van-picker :columns="techColumns" @confirm="onCTechPicked" @cancel="showTechPicker = false" />
    </van-popup>

    <!-- 处理投诉 -->
    <van-popup v-model:show="resolveOpen" position="bottom" round>
      <div class="sheet" v-if="resolving">
        <div class="sheet-title">处理投诉 #{{ resolving.id }}</div>
        <div class="rsv-detail">
          <p v-if="resolving.orderItemId"><b>{{ resolving.serviceName }}</b> · {{ resolving.originalTechnicianName }}</p>
          <p v-else><van-tag type="default">匿名</van-tag> {{ resolving.originalTechnicianName || '未指定技师' }}</p>
          <p v-if="resolving.comment" class="muted">{{ resolving.comment }}</p>
        </div>
        <van-cell-group inset>
          <van-field label="处理方式">
            <template #input>
              <div class="seg wrap">
                <button v-for="o in resolutionOptions" :key="o.v" type="button" :class="{ on: rForm.resolution === o.v }" @click="rForm.resolution = o.v">{{ o.label }}</button>
              </div>
            </template>
          </van-field>
          <van-field v-if="rForm.resolution === 'Reassigned'" label="改派给" :model-value="reassignName()" readonly is-link @click="showReassignPicker = true" />
          <van-field v-model="rForm.resolutionNote" label="备注" type="textarea" rows="2" autosize placeholder="选填" />
        </van-cell-group>
        <div class="sheet-actions"><van-button block type="primary" :loading="saving" @click="submitResolve">确认处理</van-button></div>
      </div>
    </van-popup>
    <van-popup v-model:show="showReassignPicker" position="bottom" round>
      <van-picker :columns="reassignColumns" @confirm="onReassignPicked" @cancel="showReassignPicker = false" />
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.cp { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.cp-head { display: flex; align-items: center; justify-content: space-between; }
.cp-svc { font-size: 15px; font-weight: 600; }
.cp-tech { color: #6b7280; font-weight: 400; font-size: 13px; }
.cp-order { margin-top: 4px; color: #98a2b3; font-size: 13px; }
.cp-tags { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 8px; }
.cp-comment { margin: 8px 0 0; font-size: 14px; color: #1f2733; }
.cp-resolution { margin-top: 8px; font-size: 13px; color: #16a34a; }
.cp-foot { display: flex; align-items: center; justify-content: space-between; margin-top: 10px; }
.cp-actions { display: flex; gap: 8px; }
.muted { color: #98a2b3; font-size: 12px; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
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
.rsv-detail { background: #f5f7f9; margin: 0 16px 12px; padding: 12px; border-radius: 10px; }
.rsv-detail p { margin: 4px 0; }
.seg { display: flex; gap: 8px; width: 100%; }
.seg.wrap { flex-wrap: wrap; }
.seg button { flex: 0 0 calc(50% - 4px); border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 8px; padding: 6px 0; font-size: 13px; }
.seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
</style>
