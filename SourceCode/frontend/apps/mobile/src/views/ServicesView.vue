<script lang="ts">
export default { name: 'ServicesView' };
</script>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import {
  NavBar as VanNavBar, PullRefresh as VanPullRefresh, Empty as VanEmpty, Tag as VanTag,
  Button as VanButton, Popup as VanPopup, Form as VanForm, Field as VanField,
  Stepper as VanStepper, Switch as VanSwitch, CellGroup as VanCellGroup, Checkbox as VanCheckbox,
  showSuccessToast, showToast, showConfirmDialog
} from 'vant';
import { servicesApi } from '@/api/modules';
import type { ServiceItem } from '@/api/types';

const rows = ref<ServiceItem[]>([]);
const loading = ref(false);
const refreshing = ref(false);
const includeInactive = ref(false);

const formOpen = ref(false);
const formMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const saving = ref(false);
const form = reactive({
  code: '', name: '', durationMinutes: 60, price: 0, memberPrice: 0,
  description: '', isActive: true, sort: 0
});

function fmt(n?: number | null) { return (n ?? 0).toFixed(2); }

async function reload() {
  loading.value = true;
  try { rows.value = await servicesApi.list(includeInactive.value); }
  catch { /* */ } finally { loading.value = false; refreshing.value = false; }
}

function openCreate() {
  formMode.value = 'create';
  editingId.value = null;
  const maxSort = rows.value.reduce((m, r) => Math.max(m, r.sort ?? 0), 0);
  Object.assign(form, { code: '', name: '', durationMinutes: 60, price: 0, memberPrice: 0, description: '', isActive: true, sort: maxSort + 1 });
  formOpen.value = true;
}
function openEdit(row: ServiceItem) {
  formMode.value = 'edit';
  editingId.value = row.id;
  Object.assign(form, {
    code: row.code, name: row.name, durationMinutes: row.durationMinutes,
    price: row.price, memberPrice: row.memberPrice, description: row.description ?? '',
    isActive: row.isActive, sort: row.sort ?? 0
  });
  formOpen.value = true;
}

async function save() {
  if (!form.code.trim()) { showToast('请输入编码'); return; }
  if (!form.name.trim()) { showToast('请输入名称'); return; }
  saving.value = true;
  try {
    const payload = { ...form, code: form.code.trim(), name: form.name.trim() };
    if (formMode.value === 'create') await servicesApi.create(payload);
    else if (editingId.value != null) await servicesApi.update(editingId.value, payload);
    showSuccessToast('已保存');
    formOpen.value = false;
    reload();
  } catch { /* */ } finally { saving.value = false; }
}

async function toggle(row: ServiceItem) {
  try {
    const { code: _c, id: _i, ...rest } = row;
    void _c; void _i;
    await servicesApi.update(row.id, { ...rest, isActive: !row.isActive });
    showSuccessToast(row.isActive ? '已停用' : '已启用');
    reload();
  } catch { /* */ }
}

async function remove(row: ServiceItem) {
  try { await showConfirmDialog({ title: '删除服务项目', message: `确认删除「${row.name}」？` }); }
  catch { return; }
  try { await servicesApi.remove(row.id); showSuccessToast('已删除'); reload(); } catch { /* */ }
}

onMounted(reload);
</script>

<template>
  <div class="qy-page services">
    <van-nav-bar title="服务项目" left-text="返回" left-arrow @click-left="$router.back()">
      <template #right><span class="nav-add" @click="openCreate">新建</span></template>
    </van-nav-bar>

    <div class="bar">
      <van-checkbox v-model="includeInactive" shape="square" @change="reload">显示已停用</van-checkbox>
    </div>

    <van-pull-refresh v-model="refreshing" @refresh="reload">
      <van-empty v-if="!loading && rows.length === 0" description="暂无服务项目" />
      <div v-for="s in rows" :key="s.id" class="svc-item">
        <div class="si-main">
          <div class="si-name">
            <span class="si-code">{{ s.code }}</span>{{ s.name }}
            <van-tag :type="s.isActive ? 'success' : 'default'">{{ s.isActive ? '启用' : '停用' }}</van-tag>
          </div>
          <div class="si-sub">{{ s.durationMinutes }}分 · 原价 ¥{{ fmt(s.price) }} · 会员 ¥{{ fmt(s.memberPrice) }}</div>
          <div v-if="s.description" class="si-desc">{{ s.description }}</div>
        </div>
        <div class="si-actions">
          <van-button size="mini" @click="openEdit(s)">编辑</van-button>
          <van-button size="mini" :type="s.isActive ? 'warning' : 'success'" plain @click="toggle(s)">{{ s.isActive ? '停用' : '启用' }}</van-button>
          <van-button size="mini" type="danger" plain @click="remove(s)">删除</van-button>
        </div>
      </div>
    </van-pull-refresh>

    <van-popup v-model:show="formOpen" position="bottom" round :style="{ maxHeight: '90%' }">
      <div class="sheet">
        <div class="sheet-title">{{ formMode === 'create' ? '新建服务项目' : '编辑服务项目' }}</div>
        <van-form @submit="save">
          <van-cell-group inset>
            <van-field v-model="form.code" label="编码" placeholder="必填" :readonly="formMode === 'edit'" />
            <van-field v-model="form.name" label="名称" placeholder="必填" />
            <van-field label="时长(分)"><template #input><van-stepper v-model="form.durationMinutes" :min="1" :max="600" /></template></van-field>
            <van-field label="原价"><template #input><van-stepper v-model="form.price" :min="0" :step="10" :decimal-length="2" /></template></van-field>
            <van-field label="会员价"><template #input><van-stepper v-model="form.memberPrice" :min="0" :step="10" :decimal-length="2" /></template></van-field>
            <van-field v-model="form.description" label="说明" type="textarea" rows="2" autosize placeholder="选填" />
            <van-field label="排序"><template #input><van-stepper v-model="form.sort" :min="0" /></template></van-field>
            <van-field label="启用"><template #input><van-switch v-model="form.isActive" /></template></van-field>
          </van-cell-group>
          <div class="sheet-actions">
            <van-button block type="primary" native-type="submit" :loading="saving">保存</van-button>
          </div>
        </van-form>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.nav-add { color: var(--qy-brand); font-size: 15px; }
.bar { padding: 10px 14px; }
.svc-item { background: #fff; margin: 8px 12px; padding: 14px; border-radius: 12px; }
.si-name { font-size: 15px; font-weight: 600; display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.si-code { color: #b0b8c4; font-weight: 400; font-size: 13px; margin-right: 4px; }
.si-sub { margin-top: 6px; color: #6b7280; font-size: 13px; }
.si-desc { margin-top: 4px; color: #98a2b3; font-size: 12px; }
.si-actions { display: flex; gap: 8px; margin-top: 12px; justify-content: flex-end; }
.sheet { padding: 16px 0 24px; }
.sheet-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 12px; }
.sheet-actions { padding: 16px 16px 0; }
</style>
