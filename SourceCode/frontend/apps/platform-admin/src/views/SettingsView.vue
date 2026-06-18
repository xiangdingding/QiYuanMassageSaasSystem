<template>
  <div class="page">
    <el-card shadow="never" v-loading="loading">
      <template #header>
        <span>服务订阅 · 展示配置</span>
        <span class="hint">此处配置会自动同步到各按摩店端（CS / BS）的「服务订阅」页</span>
      </template>

      <el-form :model="form" label-width="120px" style="max-width: 720px">
        <el-form-item label="到期/续费说明">
          <el-input
            v-model="form.expiryNotice"
            type="textarea"
            :rows="4"
            maxlength="500"
            show-word-limit
            placeholder="例如：订阅到期后系统进入只读模式……新购年限从当前到期日累加。"
          />
        </el-form-item>
        <el-form-item label="客服电话">
          <el-input v-model="form.contactPhone" maxlength="30" placeholder="如 18911916819" style="max-width: 280px" />
        </el-form-item>
        <el-form-item label="客服微信号">
          <el-input v-model="form.contactWechat" maxlength="30" placeholder="如 18911916819" style="max-width: 280px" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" :loading="saving" @click="save">保存</el-button>
          <el-button @click="load">重置</el-button>
        </el-form-item>
      </el-form>

      <el-divider>各端展示预览</el-divider>
      <div class="preview">
        <p v-for="(line, i) in (form.expiryNotice || '').split('\n')" :key="i" class="pv-line">{{ line }}</p>
        <p class="pv-contact">
          支付问题或线下支付请联系客服电话：<strong>{{ form.contactPhone || '—' }}</strong>，
          添加微信号：<strong>{{ form.contactWechat || '—' }}</strong> 咨询帮助。
        </p>
      </div>
    </el-card>

    <el-card shadow="never" v-loading="manualLoading" style="margin-top: 16px">
      <template #header>
        <span>使用手册 · F1 帮助</span>
        <span class="hint">各端按 F1 弹出；CS / BS 各分正常、无障碍两版。某版留空则该端使用系统内置默认手册。</span>
      </template>

      <el-tabs v-model="manualTab">
        <el-tab-pane label="桌面端 CS · 正常" name="csNormal">
          <el-input v-model="manual.csManualNormal" type="textarea" :rows="18" placeholder="留空则使用系统内置默认手册" />
        </el-tab-pane>
        <el-tab-pane label="桌面端 CS · 无障碍" name="csA11y">
          <el-input v-model="manual.csManualA11y" type="textarea" :rows="18" placeholder="留空则使用系统内置默认手册（面向盲人/低视力用户）" />
        </el-tab-pane>
        <el-tab-pane label="网页端 BS · 正常" name="bsNormal">
          <el-input v-model="manual.bsManualNormal" type="textarea" :rows="18" placeholder="留空则使用系统内置默认手册" />
        </el-tab-pane>
        <el-tab-pane label="网页端 BS · 无障碍" name="bsA11y">
          <el-input v-model="manual.bsManualA11y" type="textarea" :rows="18" placeholder="留空则使用系统内置默认手册（面向盲人/低视力用户）" />
        </el-tab-pane>
      </el-tabs>

      <div style="margin-top: 12px">
        <el-button type="primary" :loading="manualSaving" @click="saveManual">保存手册</el-button>
        <el-button @click="loadManual">重置</el-button>
      </div>
    </el-card>

    <el-card shadow="never" v-loading="agreementLoading" style="margin-top: 16px">
      <template #header>
        <span>注册协议</span>
        <span class="hint">门店注册页勾选同意时展示；留空则使用系统内置默认协议文本。</span>
      </template>

      <el-tabs v-model="agreementTab">
        <el-tab-pane label="用户服务协议" name="service">
          <el-input v-model="agreement.serviceAgreement" type="textarea" :rows="18" placeholder="留空则使用系统内置默认《用户服务协议》" />
        </el-tab-pane>
        <el-tab-pane label="隐私协议" name="privacy">
          <el-input v-model="agreement.privacyPolicy" type="textarea" :rows="18" placeholder="留空则使用系统内置默认《隐私协议》" />
        </el-tab-pane>
      </el-tabs>

      <div style="margin-top: 12px">
        <el-button type="primary" :loading="agreementSaving" @click="saveAgreement">保存协议</el-button>
        <el-button @click="loadAgreement">重置</el-button>
      </div>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, onMounted } from 'vue';
import { ElMessage } from 'element-plus';
import { platformSettingsApi } from '@/api/modules';

const loading = ref(false);
const saving = ref(false);
const form = reactive<{ expiryNotice: string; contactPhone: string; contactWechat: string }>({
  expiryNotice: '',
  contactPhone: '',
  contactWechat: ''
});

async function load() {
  loading.value = true;
  try {
    const s = await platformSettingsApi.getSubscription();
    form.expiryNotice = s.expiryNotice ?? '';
    form.contactPhone = s.contactPhone ?? '';
    form.contactWechat = s.contactWechat ?? '';
  } finally {
    loading.value = false;
  }
}

async function save() {
  saving.value = true;
  try {
    const s = await platformSettingsApi.updateSubscription({
      expiryNotice: form.expiryNotice || null,
      contactPhone: form.contactPhone || null,
      contactWechat: form.contactWechat || null
    });
    form.expiryNotice = s.expiryNotice ?? '';
    form.contactPhone = s.contactPhone ?? '';
    form.contactWechat = s.contactWechat ?? '';
    ElMessage.success('已保存，各端将自动获取最新配置');
  } finally {
    saving.value = false;
  }
}

// ---- 使用手册（CS/BS × 正常/无障碍 四份）----
const manualLoading = ref(false);
const manualSaving = ref(false);
const manualTab = ref<'csNormal' | 'csA11y' | 'bsNormal' | 'bsA11y'>('csNormal');
const manual = reactive({
  csManualNormal: '',
  csManualA11y: '',
  bsManualNormal: '',
  bsManualA11y: ''
});

async function loadManual() {
  manualLoading.value = true;
  try {
    const m = await platformSettingsApi.getManual();
    manual.csManualNormal = m.csManualNormal ?? '';
    manual.csManualA11y = m.csManualA11y ?? '';
    manual.bsManualNormal = m.bsManualNormal ?? '';
    manual.bsManualA11y = m.bsManualA11y ?? '';
  } finally {
    manualLoading.value = false;
  }
}

async function saveManual() {
  manualSaving.value = true;
  try {
    await platformSettingsApi.updateManual({
      csManualNormal: manual.csManualNormal || null,
      csManualA11y: manual.csManualA11y || null,
      bsManualNormal: manual.bsManualNormal || null,
      bsManualA11y: manual.bsManualA11y || null
    });
    await loadManual();
    ElMessage.success('手册已保存，各端 F1 帮助将自动获取最新内容');
  } finally {
    manualSaving.value = false;
  }
}

// ---- 注册协议（用户服务协议 / 隐私协议）----
const agreementLoading = ref(false);
const agreementSaving = ref(false);
const agreementTab = ref<'service' | 'privacy'>('service');
const agreement = reactive({ serviceAgreement: '', privacyPolicy: '' });

async function loadAgreement() {
  agreementLoading.value = true;
  try {
    const a = await platformSettingsApi.getAgreements();
    agreement.serviceAgreement = a.serviceAgreement ?? '';
    agreement.privacyPolicy = a.privacyPolicy ?? '';
  } finally {
    agreementLoading.value = false;
  }
}

async function saveAgreement() {
  agreementSaving.value = true;
  try {
    await platformSettingsApi.updateAgreements({
      serviceAgreement: agreement.serviceAgreement || null,
      privacyPolicy: agreement.privacyPolicy || null
    });
    await loadAgreement();
    ElMessage.success('协议已保存，注册页将自动获取最新内容');
  } finally {
    agreementSaving.value = false;
  }
}

onMounted(() => {
  load();
  loadManual();
  loadAgreement();
});
</script>

<style scoped>
.hint { color: var(--el-text-color-secondary); font-size: 12px; margin-left: 8px; }
.preview {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 14px 16px;
  max-width: 720px;
}
.pv-line { margin: 2px 0; color: #475569; }
.pv-contact { margin: 10px 0 0; color: #9a3412; }
</style>
