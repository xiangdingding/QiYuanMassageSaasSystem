<script lang="ts">
export default { name: 'VouchersView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, Search as VanSearch, Tabs as VanTabs, Tab as VanTab,
  List as VanList, PullRefresh as VanPullRefresh, Empty as VanEmpty, Tag as VanTag,
  Button as VanButton, Popup as VanPopup, Form as VanForm, Field as VanField,
  Stepper as VanStepper, CellGroup as VanCellGroup,
  showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import { vouchersApi, type VoucherDto } from '@/api/modules';

const rows = ref<VoucherDto[]>([]);
const loading = ref(false);
const finished = ref(false);
const refreshing = ref(false);
const page = ref(1);
const pageSize = 20;
const keyword = ref('');
const statusTab = ref(0);
const statuses: { label: string; value: string }[] = [
  { label: '全部', value: '' }, { label: '生效中', value: 'Active' },
  { label: '已核销', value: 'Redeemed' }, { label: '已过期', value: 'Expired' }, { label: '已作废', value: 'Cancelled' }
];

function statusLabel(s: string) { return ({ Active: '生效中', Redeemed: '已核销', Expired: '已过期', Cancelled: '已作废' } as Record<string, string>)[s] ?? s; }
function statusType(s: string) { return s === 'Active' ? 'success' : s === 'Redeemed' ? 'primary' : s === 'Expired' ? 'warning' : 'danger'; }
function kindLabel(k: string) { return k === 'GroupBuy' ? '团购券' : '店内券'; }
function fmt(n?: number | null) { return (n ?? 0).toFixed(2); }
function fmtExpiry(iso: string | null) {
  if (!iso) return '长期有效';
  return iso.slice(0, 16).replace('T', ' ');
}
function generateCode(kind: string): string {
  const CHARS = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
  const prefix = kind === 'GroupBuy' ? 'GB' : 'SC';
  const seg = () => Array.from({ length: 4 }, () => CHARS[Math.floor(Math.random() * CHARS.length)]).join('');
  return `${prefix}-${seg()}-${seg()}`;
}
function toIso(local: string): string | null { return local ? new Date(local).toISOString() : null; }

async function onLoad() {
  loading.value = true;
  try {
    const resp = await vouchersApi.list({
      status: statuses[statusTab.value].value || undefined,
      keyword: keyword.value.trim() || undefined,
      page: page.value, pageSize
    });
    rows.value.push(...resp.items);
    page.value += 1;
    if (rows.value.length >= resp.total || resp.items.length === 0) finished.value = true;
  } catch { finished.value = true; } finally { loading.value = false; }
}
function reset() { rows.value = []; page.value = 1; finished.value = false; }
async function onRefresh() { reset(); await onLoad(); refreshing.value = false; }
function onFilter() { reset(); onLoad(); }

async function cancel(row: VoucherDto) {
  try { await showConfirmDialog({ title: '作废券', message: `确认作废券 ${row.code}？` }); } catch { return; }
  try { await vouchersApi.cancel(row.id); showSuccessToast('已作废'); onFilter(); } catch { /* */ }
}

// 新建券
const formOpen = ref(false);
const saving = ref(false);
const form = reactive({
  kind: 'StoreCoupon', code: '', title: '',
  discountMode: 'face' as 'face' | 'percent',
  faceValue: 0, minOrderAmount: 0, discountPercent: 0.9,
  validFrom: '', expiresAt: '', platform: '', remark: ''
});
function openNew() {
  Object.assign(form, { kind: 'StoreCoupon', code: '', title: '', discountMode: 'face', faceValue: 0, minOrderAmount: 0, discountPercent: 0.9, validFrom: '', expiresAt: '', platform: '', remark: '' });
  formOpen.value = true;
}
async function save() {
  if (!form.code.trim() || !form.title.trim()) { showToast('券码与标题必填'); return; }
  const useFace = form.discountMode === 'face';
  if (useFace && form.faceValue <= 0) { showToast('请填面值'); return; }
  if (!useFace && (form.discountPercent <= 0 || form.discountPercent >= 1)) { showToast('折扣率需在 0-1 之间'); return; }
  saving.value = true;
  try {
    await vouchersApi.create({
      kind: form.kind, code: form.code.trim(), title: form.title.trim(),
      faceValue: useFace ? form.faceValue : 0, minOrderAmount: form.minOrderAmount,
      discountPercent: useFace ? null : form.discountPercent,
      validFrom: toIso(form.validFrom), expiresAt: toIso(form.expiresAt),
      platform: form.platform || null, remark: form.remark || null
    });
    showSuccessToast('已创建');
    formOpen.value = false;
    onFilter();
  } catch { /* */ } finally { saving.value = false; }
}

// 批量生成
const batchOpen = ref(false);
const batchSubmitting = ref(false);
const batchCodes = ref<string[]>([]);
const batchForm = reactive({
  kind: 'StoreCoupon', count: 100, title: '',
  discountMode: 'face' as 'face' | 'percent',
  faceValue: 100, minOrderAmount: 100, discountPercent: 0.9,
  validFrom: '', expiresAt: '', platform: '', remark: ''
});
function openBatch() {
  Object.assign(batchForm, { kind: 'StoreCoupon', count: 100, title: '', discountMode: 'face', faceValue: 100, minOrderAmount: 100, discountPercent: 0.9, validFrom: '', expiresAt: '', platform: '', remark: '' });
  batchCodes.value = [];
  batchOpen.value = true;
}
async function submitBatch() {
  if (!batchForm.title.trim()) { showToast('标题必填'); return; }
  const useFace = batchForm.discountMode === 'face';
  if (useFace && batchForm.faceValue <= 0) { showToast('请填面值'); return; }
  if (!useFace && (batchForm.discountPercent <= 0 || batchForm.discountPercent >= 1)) { showToast('折扣率需在 0-1 之间'); return; }
  batchSubmitting.value = true;
  try {
    const resp = await vouchersApi.batch({
      kind: batchForm.kind, count: batchForm.count, title: batchForm.title.trim(),
      faceValue: useFace ? batchForm.faceValue : 0, minOrderAmount: batchForm.minOrderAmount,
      discountPercent: useFace ? null : batchForm.discountPercent,
      validFrom: toIso(batchForm.validFrom), expiresAt: toIso(batchForm.expiresAt),
      platform: batchForm.platform || null, remark: batchForm.remark || null
    });
    batchCodes.value = resp.codes;
    showSuccessToast(`已生成 ${resp.created} 张`);
    onFilter();
  } catch { /* */ } finally { batchSubmitting.value = false; }
}
async function copyCodes() {
  try { await navigator.clipboard.writeText(batchCodes.value.join('\n')); showSuccessToast('已复制'); }
  catch { showToast('剪贴板不可用，请长按文本复制'); }
}

onMounted(onLoad);
</script>

<template>
  <div class="qy-page vouchers">
    <van-nav-bar title="优惠券" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openNew">新建</span></template>
    </van-nav-bar>

    <van-search v-model="keyword" placeholder="券码 / 标题 / 平台" @search="onFilter" @clear="onFilter" />
    <van-tabs v-model:active="statusTab" @change="onFilter" sticky>
      <van-tab v-for="s in statuses" :key="s.value" :title="s.label" />
    </van-tabs>
    <div class="batch-bar">
      <van-button size="small" plain type="primary" @click="openBatch">批量生成</van-button>
    </div>

    <van-pull-refresh v-model="refreshing" @refresh="onRefresh">
      <van-empty v-if="finished && rows.length === 0" description="暂无优惠券" />
      <van-list v-else v-model:loading="loading" :finished="finished" finished-text="没有更多了" @load="onLoad">
        <div v-for="v in rows" :key="v.id" class="vc">
          <div class="vc-top">
            <span class="vc-title">{{ v.title }}</span>
            <van-tag :type="statusType(v.status)">{{ statusLabel(v.status) }}</van-tag>
          </div>
          <div class="vc-code">{{ v.code }} <van-tag plain :type="v.kind === 'GroupBuy' ? 'warning' : 'success'">{{ kindLabel(v.kind) }}</van-tag></div>
          <div class="vc-meta">
            <span v-if="v.discountPercent">{{ (v.discountPercent * 10).toFixed(1) }}折</span>
            <span v-else>满减 ¥{{ fmt(v.faceValue) }}</span>
            <span v-if="v.minOrderAmount > 0"> · 满 ¥{{ fmt(v.minOrderAmount) }}</span>
            <span v-if="v.platform"> · {{ v.platform }}</span>
            <span> · {{ fmtExpiry(v.expiresAt) }}</span>
          </div>
          <div class="vc-actions">
            <van-button v-if="v.status === 'Active'" size="mini" type="danger" plain @click="cancel(v)">作废</van-button>
          </div>
        </div>
      </van-list>
    </van-pull-refresh>

    <!-- 新建券 -->
    <van-popup v-model:show="formOpen" position="bottom" round :style="{ maxHeight: '92%' }">
      <div class="sheet">
        <div class="sheet-title">新建券</div>
        <van-form @submit="save">
          <van-cell-group inset>
            <van-field label="类型">
              <template #input>
                <div class="seg">
                  <button type="button" :class="{ on: form.kind === 'StoreCoupon' }" @click="form.kind = 'StoreCoupon'">店内券</button>
                  <button type="button" :class="{ on: form.kind === 'GroupBuy' }" @click="form.kind = 'GroupBuy'">团购券</button>
                </div>
              </template>
            </van-field>
            <van-field v-model="form.code" label="券码" placeholder="必填">
              <template #button><van-button size="small" @click="form.code = generateCode(form.kind)">生成</van-button></template>
            </van-field>
            <van-field v-model="form.title" label="标题" placeholder="必填" />
            <van-field label="抵扣方式">
              <template #input>
                <div class="seg">
                  <button type="button" :class="{ on: form.discountMode === 'face' }" @click="form.discountMode = 'face'">满减面值</button>
                  <button type="button" :class="{ on: form.discountMode === 'percent' }" @click="form.discountMode = 'percent'">折扣率</button>
                </div>
              </template>
            </van-field>
            <van-field v-if="form.discountMode === 'face'" label="面值"><template #input><van-stepper v-model="form.faceValue" :min="0" :step="10" :decimal-length="2" /></template></van-field>
            <van-field v-else label="折扣率(0.9=9折)"><template #input><van-stepper v-model="form.discountPercent" :min="0.01" :max="0.99" :step="0.05" :decimal-length="2" /></template></van-field>
            <van-field label="最低订单额"><template #input><van-stepper v-model="form.minOrderAmount" :min="0" :step="10" :decimal-length="2" /></template></van-field>
            <van-field v-if="form.kind === 'GroupBuy'" v-model="form.platform" label="平台" placeholder="美团/点评/抖音" />
            <van-field label="生效起"><template #input><input v-model="form.validFrom" type="datetime-local" class="dt" /></template></van-field>
            <van-field label="到期"><template #input><input v-model="form.expiresAt" type="datetime-local" class="dt" /></template></van-field>
            <van-field v-model="form.remark" label="备注" type="textarea" rows="1" autosize placeholder="选填" />
          </van-cell-group>
          <div class="sheet-actions"><van-button block type="primary" native-type="submit" :loading="saving">保存</van-button></div>
        </van-form>
      </div>
    </van-popup>

    <!-- 批量生成 -->
    <van-popup v-model:show="batchOpen" position="bottom" round :style="{ maxHeight: '92%' }">
      <div class="sheet">
        <div class="sheet-title">批量生成优惠券</div>
        <template v-if="batchCodes.length === 0">
          <van-cell-group inset>
            <van-field label="类型">
              <template #input>
                <div class="seg">
                  <button type="button" :class="{ on: batchForm.kind === 'StoreCoupon' }" @click="batchForm.kind = 'StoreCoupon'">店内券</button>
                  <button type="button" :class="{ on: batchForm.kind === 'GroupBuy' }" @click="batchForm.kind = 'GroupBuy'">团购券</button>
                </div>
              </template>
            </van-field>
            <van-field label="数量(1-500)"><template #input><van-stepper v-model="batchForm.count" :min="1" :max="500" :step="10" /></template></van-field>
            <van-field v-model="batchForm.title" label="标题" placeholder="必填" />
            <van-field label="抵扣方式">
              <template #input>
                <div class="seg">
                  <button type="button" :class="{ on: batchForm.discountMode === 'face' }" @click="batchForm.discountMode = 'face'">满减面值</button>
                  <button type="button" :class="{ on: batchForm.discountMode === 'percent' }" @click="batchForm.discountMode = 'percent'">折扣率</button>
                </div>
              </template>
            </van-field>
            <van-field v-if="batchForm.discountMode === 'face'" label="面值"><template #input><van-stepper v-model="batchForm.faceValue" :min="0" :step="10" :decimal-length="2" /></template></van-field>
            <van-field v-else label="折扣率(0.9=9折)"><template #input><van-stepper v-model="batchForm.discountPercent" :min="0.01" :max="0.99" :step="0.05" :decimal-length="2" /></template></van-field>
            <van-field label="最低订单额"><template #input><van-stepper v-model="batchForm.minOrderAmount" :min="0" :step="10" :decimal-length="2" /></template></van-field>
            <van-field v-if="batchForm.kind === 'GroupBuy'" v-model="batchForm.platform" label="平台" placeholder="美团/点评/抖音" />
            <van-field label="生效起"><template #input><input v-model="batchForm.validFrom" type="datetime-local" class="dt" /></template></van-field>
            <van-field label="到期"><template #input><input v-model="batchForm.expiresAt" type="datetime-local" class="dt" /></template></van-field>
            <van-field v-model="batchForm.remark" label="备注" type="textarea" rows="1" autosize placeholder="选填" />
          </van-cell-group>
          <div class="sheet-actions"><van-button block type="primary" :loading="batchSubmitting" @click="submitBatch">生成 {{ batchForm.count }} 张</van-button></div>
        </template>
        <template v-else>
          <div class="batch-result">
            <p>已生成 <b>{{ batchCodes.length }}</b> 张，下方每行一个券码：</p>
            <textarea class="codes" :value="batchCodes.join('\n')" readonly rows="10"></textarea>
          </div>
          <div class="sheet-actions">
            <van-button block type="primary" @click="copyCodes">复制全部</van-button>
            <van-button block plain style="margin-top:8px" @click="batchCodes = []">再生成一批</van-button>
          </div>
        </template>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.batch-bar { padding: 8px 14px 0; }
.vc { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.vc-top { display: flex; align-items: center; justify-content: space-between; }
.vc-title { font-size: 15px; font-weight: 600; }
.vc-code { margin-top: 6px; font-size: 14px; letter-spacing: .5px; display: flex; align-items: center; gap: 8px; }
.vc-meta { margin-top: 6px; color: #98a2b3; font-size: 13px; }
.vc-actions { display: flex; justify-content: flex-end; margin-top: 8px; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.seg { display: flex; gap: 8px; width: 100%; }
.seg button { flex: 1; border: 1px solid #d6dbe2; background: #fff; color: #4b5563; border-radius: 8px; padding: 6px 0; font-size: 14px; }
.seg button.on { background: var(--qy-brand); color: #fff; border-color: var(--qy-brand); }
.dt { border: none; outline: none; font-size: 14px; width: 100%; background: transparent; font-family: inherit; color: #1f2733; }
.batch-result { padding: 0 16px; }
.batch-result p { color: #4b5563; font-size: 14px; }
.codes { width: 100%; border: 1px solid #e3e7ec; border-radius: 8px; padding: 10px; font-family: monospace; font-size: 13px; resize: none; }
</style>
