<template>
  <div class="login-bg">
    <el-card class="login-card">
      <h1>按摩店管理系统</h1>
      <p class="subtitle">店主 / 店长 / 收银 / 技师 共用登录入口</p>
      <el-form :model="form" :rules="rules" ref="formRef" label-width="0" @submit.prevent>
        <el-form-item prop="username">
          <el-input v-model="form.username" placeholder="账号" size="large" :prefix-icon="User" autofocus />
        </el-form-item>
        <el-form-item prop="password">
          <el-input
            v-model="form.password"
            type="password"
            size="large"
            placeholder="密码"
            :prefix-icon="Lock"
            show-password
            @keyup.enter="submit"
          />
        </el-form-item>
        <el-button type="primary" size="large" :loading="loading" style="width: 100%" @click="submit">
          登录
        </el-button>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { Lock, User } from '@element-plus/icons-vue';
import { authApi } from '@/api/modules';
import { useAuthStore } from '@/stores/auth';

const router = useRouter();
const auth = useAuthStore();

const form = reactive({ username: '', password: '' });
const formRef = ref<FormInstance>();
const loading = ref(false);

const rules: FormRules = {
  username: [{ required: true, message: '请输入账号', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
};

async function submit() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  loading.value = true;
  try {
    const data = await authApi.login(form);
    if (data.user.role === 'PlatformAdmin') {
      ElMessage.error('该账号是平台管理员，请使用运营平台登录');
      return;
    }
    auth.setSession(data.accessToken, data.user, data.expiresAt);
    router.replace('/');
  } finally {
    loading.value = false;
  }
}
</script>

<style scoped>
.login-bg {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #2d6a4f 0%, #40916c 100%);
}
.login-card {
  width: 380px;
  padding: 16px 8px;
}
.login-card h1 { margin: 0; text-align: center; font-size: 22px; }
.subtitle {
  text-align: center;
  color: var(--el-text-color-secondary);
  margin: 4px 0 24px;
}
</style>
