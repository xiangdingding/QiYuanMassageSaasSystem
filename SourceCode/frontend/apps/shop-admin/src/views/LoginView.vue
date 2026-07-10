<template>
  <div class="login-bg">
    <div
      class="a11y-bar"
      role="group"
      aria-label="无障碍模式切换"
    >
      <span class="a11y-bar-label" aria-hidden="true">显示模式</span>
      <el-radio-group
        :model-value="prefs.a11yMode"
        size="large"
        aria-label="选择显示模式，无障碍模式会放大字号、加粗焦点、强化读屏支持"
        @change="(v: string) => prefs.setA11yMode(v as 'normal' | 'a11y')"
      >
        <el-radio-button value="normal" aria-label="正常模式">正常模式</el-radio-button>
        <el-radio-button value="a11y" aria-label="无障碍模式（盲人友好）">无障碍模式</el-radio-button>
      </el-radio-group>
    </div>
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
        <div class="footer-link">
          <a :href="CS_DOWNLOAD_URL" aria-label="下载电脑客户端安装程序（桌面版）">⬇ 下载电脑客户端（桌面版）</a>
        </div>
      </el-form>
      <p class="trust-note" role="note">
        <span class="trust-icon" aria-hidden="true">🛡️</span>
        本系统由阿里云提供数据存储、备份均由阿里巴巴（中国）提供，更值得托付！
      </p>
    </el-card>
    <footer class="beian">
      <a href="http://beian.miit.gov.cn/" target="_blank" rel="noopener">津ICP备2026008317号</a>
    </footer>
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
import { usePrefsStore } from '@/stores/prefs';
import { CS_DOWNLOAD_URL } from '@/config';

const prefs = usePrefsStore();

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
  /* 外层 body 锁了滚动；登录页若超过 viewport，自己接管滚动 */
  height: 100vh;
  overflow: auto;
  display: flex;
  align-items: center;
  justify-content: center;
  /* 水疗/按摩主题背景图，渐变作为加载前/兜底底色 */
  background-color: #2d6a4f;
  background-image: url('@/assets/login-bg.svg');
  background-size: cover;
  background-position: center;
  background-repeat: no-repeat;
  position: relative;
}
.a11y-bar {
  position: absolute;
  top: 18px;
  right: 18px;
  display: flex;
  align-items: center;
  gap: 8px;
  background: rgba(255, 255, 255, 0.85);
  padding: 6px 10px;
  border-radius: 6px;
}
.a11y-bar-label { font-size: 13px; color: #2d6a4f; font-weight: 600; }
.login-card {
  width: 380px;
  padding: 16px 8px;
  border: none;
  border-radius: 14px;
  background: rgba(255, 255, 255, 0.94);
  backdrop-filter: blur(4px);
  box-shadow: 0 18px 48px rgba(15, 51, 36, 0.32);
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
.trust-note {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  text-align: center;
  margin: 20px 0 4px;
  padding: 10px 12px;
  font-size: 13px;
  font-weight: 600;
  line-height: 1.6;
  color: #1b5e3f;
  background: linear-gradient(135deg, #eaf6ef 0%, #d8f3dc 100%);
  border: 1px solid #b7e4c7;
  border-radius: 8px;
}
.trust-icon {
  font-size: 16px;
  flex-shrink: 0;
}
.beian {
  position: absolute;
  bottom: 12px;
  left: 0;
  right: 0;
  text-align: center;
  font-size: 13px;
}
.beian a {
  color: rgba(255, 255, 255, 0.9);
  text-decoration: none;
  text-shadow: 0 1px 3px rgba(0, 0, 0, 0.4);
}
.beian a:hover { color: #fff; text-decoration: underline; }
</style>
