<template>
  <div class="qy-page profile">
    <div class="prof-head qy-brand-bg">
      <div class="avatar">{{ (auth.user?.realName || auth.user?.username || '?').slice(0, 1) }}</div>
      <div class="prof-name">{{ auth.user?.realName || auth.user?.username }}</div>
      <div class="prof-role">{{ roleLabel }}<span v-if="auth.user?.isBlind"> · 无障碍</span></div>
    </div>

    <van-cell-group inset title="当前门店">
      <van-cell
        title="门店"
        :value="appStore.activeStore?.name || '未选择'"
        :is-link="appStore.stores.length > 1"
        @click="appStore.stores.length > 1 && (showStorePicker = true)"
      />
    </van-cell-group>

    <van-cell-group inset title="账号">
      <van-cell title="账号" :value="auth.user?.username" />
      <van-cell title="修改密码" is-link @click="showPwd = true" />
    </van-cell-group>

    <van-cell-group inset title="设置">
      <van-cell title="服务器地址" :value="serverDisplay" is-link @click="openServer" />
      <van-cell title="版本" :value="version" />
    </van-cell-group>

    <div class="logout">
      <van-button block round type="danger" @click="onLogout">退出登录</van-button>
    </div>

    <!-- 门店切换 -->
    <van-popup v-model:show="showStorePicker" position="bottom" round>
      <van-picker
        :columns="storeColumns"
        :model-value="storePickerValue"
        @confirm="onPickStore"
        @cancel="showStorePicker = false"
      />
    </van-popup>

    <!-- 服务器地址 -->
    <van-dialog v-model:show="showServer" title="服务器地址" show-cancel-button :before-close="onSaveServer">
      <div class="server-tip">
        打包成 App 后填后端地址（含端口），例如 <code>http://192.168.1.100:5139</code>。<br />
        留空则使用默认/代理地址。
      </div>
      <van-field v-model="serverInput" placeholder="http://服务器IP:端口" />
    </van-dialog>

    <!-- 修改密码 -->
    <van-dialog v-model:show="showPwd" title="修改密码" show-cancel-button :before-close="onChangePwd">
      <van-cell-group>
        <van-field v-model="oldPwd" type="password" label="原密码" placeholder="请输入原密码" />
        <van-field v-model="newPwd" type="password" label="新密码" placeholder="至少 6 位" />
      </van-cell-group>
    </van-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  CellGroup as VanCellGroup, Cell as VanCell, Button as VanButton, Popup as VanPopup,
  Picker as VanPicker, Dialog as VanDialog, Field as VanField,
  showSuccessToast, showFailToast, showConfirmDialog
} from 'vant';
import { authApi } from '@/api/modules';
import { useAuthStore } from '@/stores/auth';
import { useAppStore } from '@/stores/app';
import { getApiBase, setApiBase } from '@/api/config';
import { ROLE_LABELS } from '@/api/types';

const router = useRouter();
const auth = useAuthStore();
const appStore = useAppStore();

const version = '1.0.0';
const roleLabel = computed(() => (auth.role ? ROLE_LABELS[auth.role] : ''));

const showStorePicker = ref(false);
const storeColumns = computed(() => appStore.stores.map((s) => ({ text: s.name, value: s.id })));
const storePickerValue = computed<number[]>(() =>
  appStore.activeStoreId != null ? [appStore.activeStoreId] : []
);

const showServer = ref(false);
const serverInput = ref(getApiBase());
const serverDisplay = computed(() => getApiBase() || '默认 / 代理');

const showPwd = ref(false);
const oldPwd = ref('');
const newPwd = ref('');

function onPickStore({ selectedValues }: { selectedValues: (number | undefined)[] }) {
  const id = selectedValues[0];
  showStorePicker.value = false;
  if (typeof id === 'number') appStore.setActiveStore(id);
}

function openServer() {
  serverInput.value = getApiBase();
  showServer.value = true;
}
function onSaveServer(action: string) {
  if (action === 'confirm') {
    setApiBase(serverInput.value);
    showSuccessToast('已保存，下次请求生效');
  }
  return true;
}

async function onChangePwd(action: string) {
  if (action !== 'confirm') return true;
  if (!oldPwd.value || newPwd.value.length < 6) {
    showFailToast('请填写原密码，新密码至少 6 位');
    return false; // 阻止关闭
  }
  try {
    await authApi.changePassword({ oldPassword: oldPwd.value, newPassword: newPwd.value });
    showSuccessToast('密码已修改');
    oldPwd.value = '';
    newPwd.value = '';
    return true;
  } catch {
    return false;
  }
}

async function onLogout() {
  try {
    await showConfirmDialog({ title: '退出登录', message: '确定要退出当前账号吗？' });
  } catch {
    return;
  }
  auth.logout();
  appStore.reset();
  router.replace('/login');
}
</script>

<style scoped>
.prof-head { text-align: center; padding: 30px 0 26px; border-radius: 0 0 18px 18px; }
.avatar {
  width: 64px; height: 64px; border-radius: 50%; background: rgba(255,255,255,0.22);
  color: #fff; font-size: 28px; font-weight: 700; display: flex; align-items: center;
  justify-content: center; margin: 0 auto 12px;
}
.prof-name { font-size: 20px; font-weight: 700; }
.prof-role { margin-top: 4px; opacity: 0.88; font-size: 14px; }
.van-cell-group { margin-top: 14px; }
.logout { padding: 28px 16px; }
.server-tip { padding: 16px 16px 4px; font-size: 13px; color: #6b7280; line-height: 1.6; }
.server-tip code { color: var(--qy-brand); }
</style>
