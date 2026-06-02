<template>
  <div class="page">
    <el-card shadow="never">
      <div class="toolbar">
        <span class="title">物耗库存 — {{ activeStoreName }}</span>
        <el-checkbox v-model="onlyLowStock" @change="reload">仅显示库存预警</el-checkbox>
        <div class="spacer" />
        <el-button type="primary" :icon="Plus" @click="openNew">建档</el-button>
        <el-button :icon="Refresh" @click="reload">刷新</el-button>
      </div>

      <div class="table-wrap">
      <el-table :data="rows" v-loading="loading" stripe height="100%">
        <el-table-column prop="code" label="编码" width="120" />
        <el-table-column prop="name" label="名称" min-width="160" />
        <el-table-column prop="unit" label="单位" width="80" />
        <el-table-column label="库存" width="120">
          <template #default="{ row }">
            <span :class="{ low: row.lowStock }" :aria-label="`${row.name} 库存 ${row.quantity} ${row.unit || ''}${row.lowStock ? '，已低于预警' : ''}`">
              {{ row.quantity }} {{ row.unit || '' }}
            </span>
            <el-tag v-if="row.lowStock" type="danger" size="small" style="margin-left: 6px">预警</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="minQuantity" label="预警阈值" width="100" />
        <el-table-column prop="unitCost" label="单价" width="100">
          <template #default="{ row }">{{ row.unitCost ?? '—' }}</template>
        </el-table-column>
        <el-table-column label="操作" :width="$actCol(280)" fixed="right">
          <template #default="{ row }">
            <el-button size="small" @click="openMovement(row, 'PurchaseIn')">入库</el-button>
            <el-button size="small" @click="openMovement(row, 'Consume')">领用</el-button>
            <el-button size="small" @click="openMovement(row, 'Adjust')">盘点</el-button>
            <el-button size="small" type="info" @click="viewMovements(row)">流水</el-button>
          </template>
        </el-table-column>
      </el-table>
      </div>
    </el-card>

    <el-dialog v-model="formOpen" title="物品建档" width="460px">
      <el-form :model="form" label-width="90px">
        <el-form-item label="编码" required><el-input v-model="form.code" maxlength="64" /></el-form-item>
        <el-form-item label="名称" required><el-input v-model="form.name" maxlength="200" /></el-form-item>
        <el-form-item label="单位"><el-input v-model="form.unit" maxlength="16" placeholder="瓶/包/个" /></el-form-item>
        <el-form-item label="初始库存"><el-input-number v-model="form.quantity" :min="0" :precision="3" /></el-form-item>
        <el-form-item label="预警阈值"><el-input-number v-model="form.minQuantity" :min="0" :precision="3" /></el-form-item>
        <el-form-item label="单价"><el-input-number v-model="form.unitCost" :min="0" :precision="2" /></el-form-item>
        <el-form-item label="备注"><el-input v-model="form.remark" type="textarea" :rows="2" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="formOpen = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="movementOpen" :title="movementTitle" width="420px">
      <el-form :model="movement" label-width="90px">
        <el-form-item label="物品">{{ movement.itemName }}</el-form-item>
        <el-form-item label="数量" required>
          <el-input-number v-model="movement.delta" :precision="3" />
          <span style="margin-left:8px;color:#999">{{ movementHint }}</span>
        </el-form-item>
        <el-form-item label="备注"><el-input v-model="movement.remark" maxlength="200" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="movementOpen = false">取消</el-button>
        <el-button type="primary" :loading="moving" @click="submitMovement">提交</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="historyOpen" :title="`${historyItemName} 出入库流水`" width="640px">
      <el-table :data="movements" stripe>
        <el-table-column prop="kind" label="类型" width="100">
          <template #default="{ row }">{{ inventoryKindLabel(row.kind) }}</template>
        </el-table-column>
        <el-table-column prop="delta" label="变化" width="100" />
        <el-table-column prop="quantityAfter" label="结余" width="100" />
        <el-table-column prop="operatorName" label="操作人" width="120" />
        <el-table-column prop="remark" label="备注" min-width="120" />
        <el-table-column label="时间" width="180">
          <template #default="{ row }">{{ dayjs(row.createdAt).format('YYYY-MM-DD HH:mm:ss') }}</template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { ElMessage } from 'element-plus';
import { Plus, Refresh } from '@element-plus/icons-vue';
import dayjs from 'dayjs';
import { inventoryApi, type InventoryItemDto, type InventoryMovementDto } from '@/api/modules';
import { inventoryKindLabel } from '@/utils/enumLabels';
import { useAppStore } from '@/stores/app';

const appStore = useAppStore();
const rows = ref<InventoryItemDto[]>([]);
const loading = ref(false);
const onlyLowStock = ref(false);
const formOpen = ref(false);
const saving = ref(false);
const movementOpen = ref(false);
const moving = ref(false);
const historyOpen = ref(false);
const movements = ref<InventoryMovementDto[]>([]);
const historyItemName = ref('');

const form = reactive({
  code: '', name: '', unit: '',
  quantity: 0, minQuantity: 0, unitCost: 0,
  remark: ''
});

const movement = reactive({
  itemId: 0 as number, itemName: '',
  kind: 'PurchaseIn' as string, delta: 1, remark: ''
});

const movementTitle = computed(() => {
  switch (movement.kind) {
    case 'PurchaseIn': return '采购入库';
    case 'Consume': return '领用出库';
    case 'Adjust': return '盘点调整';
    case 'Discard': return '报损';
    default: return '出入库';
  }
});

const movementHint = computed(() => {
  if (movement.kind === 'Consume' || movement.kind === 'Discard') return '系统会自动取负数';
  if (movement.kind === 'Adjust') return '正数=加，负数=减';
  return '正数表示新增库存';
});

const activeStoreName = computed(
  () => appStore.stores.find((s) => s.id === appStore.activeStoreId)?.name ?? ''
);

async function reload() {
  if (!appStore.activeStoreId) return;
  loading.value = true;
  try {
    rows.value = await inventoryApi.items(appStore.activeStoreId, onlyLowStock.value);
  } finally {
    loading.value = false;
  }
}

function openNew() {
  Object.assign(form, { code: '', name: '', unit: '', quantity: 0, minQuantity: 0, unitCost: 0, remark: '' });
  formOpen.value = true;
}

async function save() {
  if (!form.code.trim() || !form.name.trim()) { ElMessage.warning('编码和名称必填'); return; }
  saving.value = true;
  try {
    await inventoryApi.createItem({
      storeId: appStore.activeStoreId,
      code: form.code.trim(), name: form.name.trim(), unit: form.unit || null,
      quantity: form.quantity, minQuantity: form.minQuantity,
      unitCost: form.unitCost || null, remark: form.remark || null
    });
    formOpen.value = false;
    ElMessage.success('已建档');
    await reload();
  } finally {
    saving.value = false;
  }
}

function openMovement(row: InventoryItemDto, kind: string) {
  movement.itemId = row.id;
  movement.itemName = row.name;
  movement.kind = kind;
  movement.delta = 1;
  movement.remark = '';
  movementOpen.value = true;
}

async function submitMovement() {
  moving.value = true;
  try {
    await inventoryApi.move({
      itemId: movement.itemId, kind: movement.kind,
      delta: movement.delta, remark: movement.remark || null
    });
    movementOpen.value = false;
    ElMessage.success('已记录');
    await reload();
  } finally {
    moving.value = false;
  }
}

async function viewMovements(row: InventoryItemDto) {
  historyItemName.value = row.name;
  movements.value = await inventoryApi.movements(row.id, 50);
  historyOpen.value = true;
}

watch(() => appStore.activeStoreId, () => reload());
onMounted(async () => {
  await appStore.loadStores();
  await reload();
});
</script>

<style scoped>
.page { padding-bottom: 24px; }
.toolbar { display: flex; gap: 12px; align-items: center; }
.toolbar .title { font-weight: 600; font-size: 16px; }
.spacer { flex: 1; }
.low { color: #c45656; font-weight: 600; }
</style>
