<template>
  <div class="login-bg">
    <el-card class="login-card">
      <h1>按摩店管理系统</h1>
      <p class="subtitle">店主 / 店长 / 收银 / 技师 共用登录入口（手机号 + 密码）</p>
      <el-form :model="form" :rules="rules" ref="formRef" label-width="0" @submit.prevent>
        <el-form-item prop="username">
          <el-input v-model="form.username" placeholder="手机号" size="large" :prefix-icon="User" autofocus />
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
        <div class="footer-link">
          还没有账号？<router-link to="/register">免费注册（30 天试用）</router-link>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { Lock, User } from '@element-plus/icons-vue';
import { authApi } from '@/api/modules';
import { useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';

const router = useRouter();
const route = useRoute();
const auth = useAuthStore();
const appStore = useAppStore();

// 从注册页跳过来时携带刚创建的用户名，预填到账号框
const form = reactive({ username: (route.query.u as string) || '', password: '' });
const formRef = ref<FormInstance>();
const loading = ref(false);

const rules: FormRules = {
  username: [{ required: true, message: '请输入手机号', trigger: 'blur' }],
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
    // 切账号必须先清掉上一次会话残留的门店列表/当前门店，
    // 否则新用户进来会先看到旧用户的总店名
    appStore.reset();
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
.footer-link {
  text-align: center;
  margin-top: 12px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
}
.footer-link a {
  color: var(--el-color-primary);
  text-decoration: none;
}
</style>
