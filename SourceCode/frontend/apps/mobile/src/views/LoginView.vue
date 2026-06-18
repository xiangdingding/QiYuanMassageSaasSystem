<template>
  <div class="login qy-brand-bg">
    <div class="login-head">
      <div class="logo">齐</div>
      <h1>齐源按摩</h1>
      <p>门店移动工作台</p>
    </div>

    <div class="login-card">
      <van-form @submit="onSubmit">
        <van-cell-group inset>
          <van-field
            v-model="username"
            name="username"
            label="账号"
            placeholder="请输入账号"
            :rules="[{ required: true, message: '请输入账号' }]"
            clearable
          />
          <van-field
            v-model="password"
            type="password"
            name="password"
            label="密码"
            placeholder="请输入密码"
            :rules="[{ required: true, message: '请输入密码' }]"
          />
        </van-cell-group>

        <div class="login-actions">
          <van-button round block type="primary" native-type="submit" :loading="loading">
            登录
          </van-button>
          <p class="reg-link">没有账号？<a @click="$router.push('/register')">注册开通（免费试用 30 天）</a></p>
        </div>
      </van-form>

      <div class="login-foot">
        <span @click="showServer = true">服务器地址</span>
        <span class="dot">·</span>
        <span>电话/微信 18911916819</span>
      </div>
    </div>

    <van-dialog
      v-model:show="showServer"
      title="服务器地址"
      show-cancel-button
      :before-close="onSaveServer"
    >
      <div class="server-tip">
        打包成 App 后需填后端服务器地址（含端口），例如<br />
        <code>http://192.168.1.100:5139</code>
      </div>
      <van-field v-model="serverInput" placeholder="http://服务器IP:端口" />
    </van-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  Form as VanForm,
  Field as VanField,
  CellGroup as VanCellGroup,
  Button as VanButton,
  Dialog as VanDialog,
  showSuccessToast
} from 'vant';
import { authApi } from '@/api/modules';
import { useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';
import { getApiBase, setApiBase } from '@/api/config';
import { firstAllowedPath } from '@/router';

const router = useRouter();
const auth = useAuthStore();
const appStore = useAppStore();

const username = ref('');
const password = ref('');
const loading = ref(false);

const showServer = ref(false);
const serverInput = ref(getApiBase());

async function onSubmit() {
  loading.value = true;
  try {
    const resp = await authApi.login({ username: username.value.trim(), password: password.value });
    auth.setSession(resp.accessToken, resp.user, resp.expiresAt);
    // 技师不一定有门店列表权限，加载失败不阻断登录。
    try {
      await appStore.loadStores(true);
    } catch {
      /* ignore */
    }
    showSuccessToast('登录成功');
    router.replace(firstAllowedPath(auth.role));
  } catch {
    /* 错误提示已由 http 拦截器统一弹出 */
  } finally {
    loading.value = false;
  }
}

function onSaveServer(action: string) {
  if (action === 'confirm') {
    setApiBase(serverInput.value);
  }
  return true;
}
</script>

<style scoped>
.login {
  min-height: 100%;
  padding: 0 0 40px;
  display: flex;
  flex-direction: column;
}
.login-head {
  text-align: center;
  padding: 64px 0 36px;
}
.logo {
  width: 64px;
  height: 64px;
  border-radius: 18px;
  background: rgba(255, 255, 255, 0.2);
  color: #fff;
  font-size: 34px;
  font-weight: 800;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 16px;
}
.login-head h1 { margin: 0; font-size: 26px; letter-spacing: 2px; }
.login-head p { margin: 6px 0 0; opacity: 0.85; font-size: 14px; }
.login-card {
  background: #fff;
  border-radius: 22px 22px 0 0;
  flex: 1;
  margin-top: 12px;
  padding: 30px 0 0;
}
.login-actions { padding: 24px 16px 12px; }
.reg-link { text-align: center; margin: 16px 0 0; font-size: 13px; color: #98a2b3; }
.reg-link a { color: var(--qy-brand); }
.login-foot {
  text-align: center;
  color: #98a2b3;
  font-size: 13px;
  padding: 10px 16px 0;
}
.login-foot span:first-child { color: var(--qy-brand); }
.login-foot .dot { margin: 0 8px; }
.server-tip { padding: 16px 16px 4px; font-size: 13px; color: #6b7280; line-height: 1.6; }
.server-tip code { color: var(--qy-brand); }
</style>
