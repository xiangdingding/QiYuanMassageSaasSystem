<template>
  <van-popup
    v-model:show="show"
    round
    position="center"
    :close-on-click-overlay="false"
    :closeable="false"
    :style="{ width: '86%', maxWidth: '360px' }"
  >
    <div class="upd">
      <div class="upd-head">
        <div class="upd-title">发现新版本 {{ result?.latestVersion }}</div>
        <div class="upd-sub">当前版本 {{ current }}</div>
      </div>

      <div class="upd-body">
        <div class="upd-log-label">更新内容</div>
        <div class="upd-log">{{ changelog }}</div>
      </div>

      <div v-if="downloading" class="upd-progress">
        <van-progress :percentage="percent" color="#2D6A4F" track-color="#E4E9F2" />
        <div class="upd-progress-text">正在下载安装包… {{ percent }}%</div>
      </div>

      <div class="upd-actions">
        <van-button
          v-if="!force"
          block
          plain
          class="upd-btn"
          :disabled="downloading"
          @click="later"
        >稍后再说</van-button>
        <van-button
          v-else
          block
          plain
          class="upd-btn"
          :disabled="downloading"
          @click="exitApp"
        >退出应用</van-button>
        <van-button
          block
          type="primary"
          color="#2D6A4F"
          class="upd-btn"
          :loading="downloading"
          loading-text="下载中…"
          @click="doUpdate"
        >立即更新</van-button>
      </div>
    </div>
  </van-popup>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { Popup as VanPopup, Progress as VanProgress, Button as VanButton, showFailToast } from 'vant';
import {
  checkAppUpdate,
  downloadAndInstallApk,
  getCurrentVersion,
  type AppVersionCheckResult
} from '@/update/appUpdate';

const show = ref(false);
const force = ref(false);
const downloading = ref(false);
const percent = ref(0);
const current = ref('');
const result = ref<AppVersionCheckResult | null>(null);

const changelog = computed(() => result.value?.changelog?.trim() || '优化体验、修复若干问题。');

onMounted(async () => {
  try {
    const { Capacitor } = await import('@capacitor/core');
    // 仅安卓原生 App 检测；iOS 走 App Store、Web 端无需
    if (!Capacitor?.isNativePlatform?.() || Capacitor.getPlatform() !== 'android') return;

    current.value = await getCurrentVersion();
    const r = await checkAppUpdate(current.value);
    if (r && r.hasUpdate && r.downloadUrl) {
      result.value = r;
      force.value = r.forceUpdate;
      show.value = true;
    }
  } catch {
    /* 检测失败不打扰 */
  }
});

async function doUpdate() {
  if (downloading.value || !result.value) return;
  downloading.value = true;
  percent.value = 0;
  try {
    await downloadAndInstallApk(result.value.downloadUrl, result.value.latestVersion, (ratio) => {
      percent.value = Math.round(ratio * 100);
    });
    // 安装器已唤起，前台交给系统安装界面；用户取消可再次点击更新
  } catch (e) {
    showFailToast('更新失败，请稍后重试或到应用市场下载');
  } finally {
    downloading.value = false;
  }
}

function later() {
  if (downloading.value) return;
  show.value = false;
}

async function exitApp() {
  try {
    const { App } = await import('@capacitor/app');
    await App.exitApp();
  } catch {
    /* ignore */
  }
}
</script>

<style scoped>
.upd { padding: 22px 20px 18px; }
.upd-head { margin-bottom: 14px; }
.upd-title { font-size: 18px; font-weight: 700; color: #1a1a1a; }
.upd-sub { font-size: 13px; color: #999; margin-top: 4px; }
.upd-log-label { font-size: 14px; font-weight: 600; color: #333; margin-bottom: 6px; }
.upd-log { font-size: 14px; line-height: 1.7; color: #555; max-height: 180px; overflow-y: auto; white-space: pre-wrap; }
.upd-progress { margin-top: 16px; }
.upd-progress-text { font-size: 12px; color: #888; margin-top: 6px; text-align: center; }
.upd-actions { margin-top: 20px; display: flex; flex-direction: column; gap: 10px; }
.upd-btn { height: 44px; font-size: 16px; border-radius: 10px; }
</style>
