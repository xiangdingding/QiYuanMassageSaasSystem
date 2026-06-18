<script lang="ts">
export default { name: 'StoresView' };
</script>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, PullRefresh as VanPullRefresh, Empty as VanEmpty, Tag as VanTag,
  Button as VanButton, Popup as VanPopup, Form as VanForm, Field as VanField, Switch as VanSwitch,
  Picker as VanPicker, CellGroup as VanCellGroup,
  showSuccessToast, showToast
} from 'vant';
import { storesApi } from '@/api/modules';
import { useAppStore } from '@/stores/app';
import type { Store } from '@/api/types';

const appStore = useAppStore();
const rows = ref<Store[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const saving = ref(false);

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const showParentPicker = ref(false);
const form = reactive({ name: '', address: '', phone: '', parentStoreId: null as number | null, isActive: true, cutoff: '00:00' });

const headquarters = computed(() => rows.value.filter((s) => s.isHeadquarters));
const parentColumns = () => headquarters.value.map((s) => ({ text: s.name, value: s.id }));
function parentName() { return headquarters.value.find((s) => s.id === form.parentStoreId)?.name || '选择上级总店'; }

function minutesToHHmm(m: number) { const s = Math.max(0, Math.min(1439, m || 0)); return `${String(Math.floor(s / 60)).padStart(2, '0')}:${String(s % 60).padStart(2, '0')}`; }
function hhmmToMinutes(s: string) { if (!s) return 0; const [h, m] = s.split(':').map(Number); return (h || 0) * 60 + (m || 0); }
function formatCutoff(m: number) { return m > 0 ? `${minutesToHHmm(m)} 切日` : '自然日'; }

async function reload() {
  loading.value = true;
  try { rows.value = await storesApi.list(); }
  catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  Object.assign(form, { name: '', address: '', phone: '', parentStoreId: headquarters.value[0]?.id ?? null, isActive: true, cutoff: '00:00' });
  formOpen.value = true;
}
function openEdit(row: Store) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, { name: row.name, address: row.address ?? '', phone: row.phone ?? '', parentStoreId: row.parentStoreId ?? null, isActive: row.isActive, cutoff: minutesToHHmm(row.dayCloseCutoffMinutes ?? 0) });
  formOpen.value = true;
}
function onParentPicked({ selectedValues }: { selectedValues: number[] }) {
  form.parentStoreId = selectedValues[0] ?? null;
  showParentPicker.value = false;
}

async function save() {
  if (!form.name.trim()) { showToast('请输入名称'); return; }
  saving.value = true;
  try {
    const cutoff = hhmmToMinutes(form.cutoff);
    if (formMode.value === 'create') {
      await storesApi.create({ name: form.name.trim(), address: form.address || null, phone: form.phone || null, parentStoreId: form.parentStoreId, dayCloseCutoffMinutes: cutoff });
    } else if (editingId.value != null) {
      await storesApi.update(editingId.value, { name: form.name.trim(), address: form.address || null, phone: form.phone || null, isActive: form.isActive, dayCloseCutoffMinutes: cutoff });
    }
    showSuccessToast('已保存');
    formOpen.value = false;
    await appStore.loadStores(true);
    reload();
  } catch { /* */ } finally { saving.value = false; }
}

onMounted(async () => {
  if (!appStore.stores.length) await appStore.loadStores().catch(() => undefined);
  reload();
});
</script>

<template>
  <div class="qy-page stores">
    <van-nav-bar title="门店管理" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openCreate">新建分店</span></template>
    </van-nav-bar>

    <van-pull-refresh v-model="refreshing" @refresh="reload">
      <van-empty v-if="!loading && rows.length === 0" description="暂无门店" />
      <div v-for="s in rows" :key="s.id" class="st">
        <div class="st-main">
          <div class="st-name">
            {{ s.name }}
            <van-tag v-if="s.isHeadquarters" type="warning">总店</van-tag>
            <van-tag :type="s.isActive ? 'success' : 'default'">{{ s.isActive ? '营业中' : '已停' }}</van-tag>
          </div>
          <div v-if="s.address" class="st-sub">{{ s.address }}</div>
          <div class="st-sub">{{ s.phone || '无电话' }} · {{ formatCutoff(s.dayCloseCutoffMinutes) }}</div>
        </div>
        <van-button size="mini" @click="openEdit(s)">编辑</van-button>
      </div>
    </van-pull-refresh>

    <van-popup v-model:show="formOpen" position="bottom" round :style="{ maxHeight: '90%' }">
      <div class="sheet">
        <div class="sheet-title">{{ formMode === 'create' ? '新建分店' : '编辑门店' }}</div>
        <van-form @submit="save">
          <van-cell-group inset>
            <van-field v-model="form.name" label="名称" placeholder="必填" />
            <van-field v-model="form.address" label="地址" placeholder="选填" />
            <van-field v-model="form.phone" label="电话" placeholder="选填" />
            <van-field v-if="formMode === 'create'" label="所属总店" :model-value="parentName()" readonly is-link @click="showParentPicker = true" />
            <van-field v-if="formMode === 'edit'" label="状态">
              <template #input><van-switch v-model="form.isActive" /></template>
            </van-field>
            <van-field label="营业日切日(HH:mm)"><template #input><input v-model="form.cutoff" type="time" class="dt" /></template></van-field>
          </van-cell-group>
          <p class="hint">北京时间。营业到凌晨的门店把切日点设在停业后（如 06:00），该时刻前完成的订单归到前一营业日。00:00 = 自然日。</p>
          <div class="sheet-actions"><van-button block type="primary" native-type="submit" :loading="saving">保存</van-button></div>
        </van-form>
      </div>
    </van-popup>
    <van-popup v-model:show="showParentPicker" position="bottom" round>
      <van-picker :columns="parentColumns()" @confirm="onParentPicked" @cancel="showParentPicker = false" />
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.st { display: flex; align-items: flex-start; justify-content: space-between; background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.st-name { font-size: 16px; font-weight: 600; display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.st-sub { margin-top: 6px; color: #98a2b3; font-size: 13px; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
.hint { color: #98a2b3; font-size: 12px; padding: 8px 28px 0; line-height: 1.6; }
.dt { border: none; outline: none; font-size: 14px; background: transparent; font-family: inherit; width: 100%; }
</style>
