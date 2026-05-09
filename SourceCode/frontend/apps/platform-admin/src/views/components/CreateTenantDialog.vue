<template>
  <el-dialog
    :model-value="modelValue"
    @update:model-value="(v: boolean) => emit('update:modelValue', v)"
    title="新建按摩店租户"
    width="560px"
    @close="reset"
  >
    <el-form :model="form" :rules="rules" ref="formRef" label-width="120px">
      <el-form-item label="店铺名" prop="name">
        <el-input v-model="form.name" placeholder="如：齐源按摩中心" />
      </el-form-item>
      <el-form-item label="联系电话" prop="contactPhone">
        <el-input v-model="form.contactPhone" />
      </el-form-item>
      <el-form-item label="联系人">
        <el-input v-model="form.contactName" />
      </el-form-item>
      <el-form-item label="总店名称" prop="headquartersName">
        <el-input v-model="form.headquartersName" placeholder="将自动建立的总店" />
      </el-form-item>
      <el-form-item label="店主账号" prop="ownerUsername">
        <el-input v-model="form.ownerUsername" />
      </el-form-item>
      <el-form-item label="店主初始密码" prop="ownerPassword">
        <el-input v-model="form.ownerPassword" type="password" show-password />
      </el-form-item>
      <el-form-item label="店主姓名">
        <el-input v-model="form.ownerRealName" />
      </el-form-item>
      <el-form-item label="初始套餐">
        <el-select v-model="form.initialPlanId" placeholder="（可选）激活一年" clearable style="width: 100%">
          <el-option
            v-for="p in plans"
            :key="p.id"
            :label="`${p.name} - ¥${p.annualPrice}/年`"
            :value="p.id"
          />
        </el-select>
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="emit('update:modelValue', false)">取消</el-button>
      <el-button type="primary" :loading="loading" @click="submit">创建</el-button>
    </template>
  </el-dialog>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { tenantsApi } from '@/api/modules';
import type { CreateTenantRequest, Plan } from '@/api/types';

const props = defineProps<{ modelValue: boolean; plans: Plan[] }>();
const emit = defineEmits<{
  (e: 'update:modelValue', v: boolean): void;
  (e: 'created'): void;
}>();

const formRef = ref<FormInstance>();
const loading = ref(false);

const form = reactive<CreateTenantRequest>({
  name: '',
  contactPhone: '',
  contactName: '',
  ownerUsername: '',
  ownerPassword: '',
  ownerRealName: '',
  headquartersName: '',
  initialPlanId: null
});

const rules: FormRules = {
  name: [{ required: true, message: '请输入店铺名', trigger: 'blur' }],
  contactPhone: [{ required: true, message: '请输入联系电话', trigger: 'blur' }],
  headquartersName: [{ required: true, message: '请输入总店名称', trigger: 'blur' }],
  ownerUsername: [{ required: true, message: '请输入店主账号', trigger: 'blur' }],
  ownerPassword: [
    { required: true, message: '请输入密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 位', trigger: 'blur' }
  ]
};

function reset() {
  form.name = '';
  form.contactPhone = '';
  form.contactName = '';
  form.ownerUsername = '';
  form.ownerPassword = '';
  form.ownerRealName = '';
  form.headquartersName = '';
  form.initialPlanId = null;
}

async function submit() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  loading.value = true;
  try {
    await tenantsApi.create({ ...form });
    ElMessage.success('创建成功');
    emit('created');
  } finally {
    loading.value = false;
  }
}

void props;
</script>
