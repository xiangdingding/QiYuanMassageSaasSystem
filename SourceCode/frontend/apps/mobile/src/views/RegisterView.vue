<script lang="ts">
export default { name: 'RegisterView' };
</script>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import {
  NavBar as VanNavBar, Form as VanForm, Field as VanField, CellGroup as VanCellGroup,
  Button as VanButton, Checkbox as VanCheckbox, Popup as VanPopup,
  showSuccessToast, showToast, showDialog
} from 'vant';
import { agreementsApi, tenantsApi } from '@/api/modules';
import type { PlatformAgreement } from '@/api/types';

const router = useRouter();

const form = reactive({
  name: '', contactName: '', ownerPhone: '', ownerPassword: '', confirmPassword: ''
});
const agreed = ref(false);
const loading = ref(false);

// 注册协议：匿名拉取，登录前可读
const agreement = ref<PlatformAgreement | null>(null);
const agreementOpen = ref(false);
const agreementTitle = ref('');
const agreementContent = ref('');

async function showAgreement(which: 'service' | 'privacy') {
  if (!agreement.value) {
    try { agreement.value = await agreementsApi.get(); } catch { /* 拉取失败弹窗仍可开 */ }
  }
  agreementTitle.value = which === 'service' ? '用户服务协议' : '隐私协议';
  agreementContent.value = which === 'service'
    ? (agreement.value?.serviceAgreement ?? '加载失败，请稍后重试')
    : (agreement.value?.privacyPolicy ?? '加载失败，请稍后重试');
  agreementOpen.value = true;
}

async function submit() {
  if (!form.name.trim()) { showToast('请输入店铺名'); return; }
  if (!/^\d{11}$/.test(form.ownerPhone)) { showToast('请输入 11 位手机号'); return; }
  if (form.ownerPassword.length < 6) { showToast('密码至少 6 位'); return; }
  if (form.ownerPassword !== form.confirmPassword) { showToast('两次密码不一致'); return; }
  if (!agreed.value) { showToast('请先阅读并勾选同意《用户服务协议》和《隐私协议》'); return; }
  loading.value = true;
  try {
    const resp = await tenantsApi.register({
      name: form.name.trim(),
      // 店铺联系电话与登录手机号合并：用同一个字段
      contactPhone: form.ownerPhone,
      contactName: form.contactName.trim() || null,
      ownerPhone: form.ownerPhone,
      ownerPassword: form.ownerPassword,
      ownerRealName: null
    });
    showSuccessToast('注册成功');
    await showDialog({
      title: '欢迎使用',
      message: `手机号「${resp.ownerPhone}」已开通 ${resp.trialDays} 天试用，试用至 ${new Date(resp.expireAt).toLocaleDateString('zh-CN')}。请用手机号 + 密码登录。`,
      confirmButtonText: '去登录'
    }).catch(() => null);
    router.replace('/login');
  } catch {
    /* 拦截器已提示 */
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="qy-page register">
    <van-nav-bar title="注册按摩店账号" left-text="返回" left-arrow @click-left="$router.replace('/login')" />

    <p class="subtitle">免费试用 30 天，无需付费即可体验全部功能。试用结束前联系平台续费即可转成正式用户。</p>

    <van-form @submit="submit">
      <van-cell-group inset>
        <van-field v-model="form.name" label="店铺名" placeholder="如：齐源按摩中心" />
        <van-field v-model="form.contactName" label="联系人" placeholder="选填" />
        <van-field v-model="form.ownerPhone" label="手机号" type="tel" maxlength="11" placeholder="登录手机号，也作店铺联系电话" />
        <van-field v-model="form.ownerPassword" label="密码" type="password" placeholder="至少 6 位" />
        <van-field v-model="form.confirmPassword" label="确认密码" type="password" placeholder="再次输入" />
      </van-cell-group>

      <div class="agree-row">
        <van-checkbox v-model="agreed" shape="square" icon-size="18px" />
        <span class="agree-text">
          我已阅读并同意
          <a @click="showAgreement('service')">《用户服务协议》</a>
          和
          <a @click="showAgreement('privacy')">《隐私协议》</a>
        </span>
      </div>

      <div class="reg-actions">
        <van-button round block type="primary" native-type="submit" :loading="loading">
          注册并开通 30 天试用
        </van-button>
        <p class="foot-link">已有账号？<a @click="$router.replace('/login')">直接登录</a></p>
      </div>
    </van-form>

    <van-popup v-model:show="agreementOpen" position="bottom" round :style="{ height: '70%' }">
      <div class="agreement">
        <div class="ag-title">{{ agreementTitle }}</div>
        <pre class="ag-text">{{ agreementContent }}</pre>
        <div class="ag-actions">
          <van-button block @click="agreementOpen = false">关闭</van-button>
        </div>
      </div>
    </van-popup>
  </div>
</template>

<style scoped>
.subtitle { color: #6b7280; font-size: 13px; line-height: 1.6; padding: 14px 20px 4px; text-align: center; }
.agree-row { display: flex; align-items: center; gap: 8px; padding: 14px 20px 4px; font-size: 13px; color: #6b7280; }
.agree-text a { color: var(--qy-brand); }
.reg-actions { padding: 18px 16px 0; }
.foot-link { text-align: center; margin-top: 14px; font-size: 13px; color: #98a2b3; }
.foot-link a { color: var(--qy-brand); }
.agreement { display: flex; flex-direction: column; height: 100%; padding: 18px 0 0; }
.ag-title { text-align: center; font-size: 17px; font-weight: 700; margin-bottom: 10px; }
.ag-text {
  flex: 1; overflow-y: auto; white-space: pre-wrap; word-break: break-word;
  font-family: inherit; font-size: 14px; line-height: 1.8; color: #374151; margin: 0; padding: 0 18px;
}
.ag-actions { padding: 14px 16px 24px; }
</style>
