<template>
  <el-dialog
    v-model="visible"
    title="个人设置"
    width="520px"
    aria-label="个人设置"
    :close-on-click-modal="false"
    @open="reload"
  >
    <el-tabs v-model="tab">
      <el-tab-pane label="基本资料" name="profile">
        <el-form
          ref="profileFormRef"
          :model="profileForm"
          :rules="profileRules"
          label-width="100px"
          v-loading="loading"
        >
          <el-form-item label="登录账号">
            <el-input :model-value="profile?.username ?? ''" disabled />
          </el-form-item>
          <el-form-item label="角色">
            <el-input :model-value="roleLabel" disabled />
          </el-form-item>
          <el-form-item label="姓名" prop="realName">
            <el-input
              v-model="profileForm.realName"
              placeholder="您的真实姓名（可选）"
              maxlength="32"
              aria-label="姓名"
            />
          </el-form-item>
          <el-form-item label="手机号" prop="phone">
            <el-input
              v-model="profileForm.phone"
              placeholder="11 位手机号，用于登录"
              maxlength="11"
              aria-label="手机号"
            />
          </el-form-item>
        </el-form>
        <div class="tab-footer">
          <el-button type="primary" :loading="saving" @click="saveProfile">保存</el-button>
        </div>
      </el-tab-pane>

      <el-tab-pane label="修改密码" name="password">
        <el-form
          ref="pwdFormRef"
          :model="pwdForm"
          :rules="pwdRules"
          label-width="100px"
        >
          <el-form-item label="原密码" prop="oldPassword">
            <el-input
              v-model="pwdForm.oldPassword"
              type="password"
              show-password
              placeholder="请输入原密码"
              aria-label="原密码"
            />
          </el-form-item>
          <el-form-item label="新密码" prop="newPassword">
            <el-input
              v-model="pwdForm.newPassword"
              type="password"
              show-password
              placeholder="至少 6 位"
              aria-label="新密码"
            />
          </el-form-item>
          <el-form-item label="确认新密码" prop="confirmPassword">
            <el-input
              v-model="pwdForm.confirmPassword"
              type="password"
              show-password
              placeholder="再次输入新密码"
              aria-label="确认新密码"
            />
          </el-form-item>
        </el-form>
        <div class="tab-footer">
          <el-button type="primary" :loading="saving" @click="savePassword">修改密码</el-button>
        </div>
      </el-tab-pane>
    </el-tabs>
  </el-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { authApi, type UserProfile } from '@/api/modules';
import { useAuthStore } from '@/stores/auth';

const props = defineProps<{ modelValue: boolean }>();
const emit = defineEmits<{ (e: 'update:modelValue', v: boolean): void }>();

const visible = computed({
  get: () => props.modelValue,
  set: (v) => emit('update:modelValue', v)
});

const auth = useAuthStore();

const ROLE_LABELS: Record<string, string> = {
  PlatformAdmin: '平台管理员',
  ShopOwner: '店主',
  StoreManager: '店长',
  Cashier: '收银员',
  Technician: '技师'
};

const tab = ref<'profile' | 'password'>('profile');
const loading = ref(false);
const saving = ref(false);
const profile = ref<UserProfile | null>(null);

const profileForm = reactive({ realName: '', phone: '' });
const pwdForm = reactive({ oldPassword: '', newPassword: '', confirmPassword: '' });

const profileFormRef = ref<FormInstance>();
const pwdFormRef = ref<FormInstance>();

const roleLabel = computed(() => (profile.value ? ROLE_LABELS[profile.value.role] ?? profile.value.role : ''));

const profileRules: FormRules = {
  phone: [
    {
      validator: (_r, val: string, cb) => {
        if (val && !/^\d{11}$/.test(val)) cb(new Error('请输入 11 位手机号'));
        else cb();
      },
      trigger: 'blur'
    }
  ]
};

const pwdRules: FormRules = {
  oldPassword: [{ required: true, message: '请输入原密码', trigger: 'blur' }],
  newPassword: [
    { required: true, message: '请输入新密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请再次输入新密码', trigger: 'blur' },
    {
      validator: (_r, val, cb) => {
        if (val !== pwdForm.newPassword) cb(new Error('两次输入不一致'));
        else cb();
      },
      trigger: 'blur'
    }
  ]
};

async function reload() {
  tab.value = 'profile';
  loading.value = true;
  try {
    profile.value = await authApi.profile();
    profileForm.realName = profile.value.realName ?? '';
    profileForm.phone = profile.value.phone ?? '';
    pwdForm.oldPassword = '';
    pwdForm.newPassword = '';
    pwdForm.confirmPassword = '';
  } finally {
    loading.value = false;
  }
}

async function saveProfile() {
  if (!profileFormRef.value) return;
  const ok = await profileFormRef.value.validate().catch(() => false);
  if (!ok) return;
  saving.value = true;
  try {
    const phoneChanged = (profileForm.phone || null) !== (profile.value?.phone ?? null);
    const updated = await authApi.updateProfile({
      realName: profileForm.realName || null,
      phone: profileForm.phone || null
    });
    profile.value = updated;
    if (auth.user) {
      auth.user.realName = updated.realName ?? null;
      localStorage.setItem('massage_saas_shop_user', JSON.stringify(auth.user));
    }
    if (phoneChanged) {
      await ElMessageBox.alert('手机号已修改，下次登录请使用新手机号。', '提示', {
        confirmButtonText: '知道了'
      }).catch(() => null);
    } else {
      ElMessage.success('保存成功');
    }
  } finally {
    saving.value = false;
  }
}

async function savePassword() {
  if (!pwdFormRef.value) return;
  const ok = await pwdFormRef.value.validate().catch(() => false);
  if (!ok) return;
  saving.value = true;
  try {
    await authApi.changePassword({
      oldPassword: pwdForm.oldPassword,
      newPassword: pwdForm.newPassword
    });
    ElMessage.success('密码修改成功，请重新登录');
    visible.value = false;
    auth.logout();
    setTimeout(() => location.assign('/login'), 300);
  } finally {
    saving.value = false;
  }
}
</script>

<style scoped>
.tab-footer {
  display: flex;
  justify-content: flex-end;
  padding-top: 8px;
}
</style>
