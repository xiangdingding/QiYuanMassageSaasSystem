<template>
  <div class="register-bg">
    <el-card class="register-card">
      <h1>注册按摩店账号</h1>
      <p class="subtitle">免费试用 30 天，无需信用卡。试用结束前联系平台续费即可转正</p>
      <el-form :model="form" :rules="rules" ref="formRef" label-width="100px" @submit.prevent>
        <el-form-item label="店铺名" prop="name">
          <el-input v-model="form.name" placeholder="如：齐源按摩中心" />
        </el-form-item>
        <el-form-item label="联系电话" prop="contactPhone">
          <el-input v-model="form.contactPhone" placeholder="11 位手机号" />
        </el-form-item>
        <el-form-item label="联系人">
          <el-input v-model="form.contactName" placeholder="选填" />
        </el-form-item>
        <el-form-item label="登录账号" prop="ownerUsername">
          <el-input v-model="form.ownerUsername" placeholder="6-20 位字母数字" />
        </el-form-item>
        <el-form-item label="登录密码" prop="ownerPassword">
          <el-input
            v-model="form.ownerPassword"
            type="password"
            show-password
            placeholder="至少 6 位"
          />
        </el-form-item>
        <el-form-item label="确认密码" prop="confirmPassword">
          <el-input
            v-model="form.confirmPassword"
            type="password"
            show-password
            placeholder="再次输入"
            @keyup.enter="submit"
          />
        </el-form-item>
        <el-form-item label="您的姓名">
          <el-input v-model="form.ownerRealName" placeholder="选填" />
        </el-form-item>
        <el-button
          type="primary"
          size="large"
          :loading="loading"
          style="width: 100%"
          @click="submit"
        >
          注册并开通 30 天试用
        </el-button>
        <div class="footer-link">
          已有账号？<router-link to="/login">直接登录</router-link>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus';
import { tenantsApi } from '@/api/modules';

const router = useRouter();

const form = reactive({
  name: '',
  contactPhone: '',
  contactName: '',
  ownerUsername: '',
  ownerPassword: '',
  confirmPassword: '',
  ownerRealName: ''
});
const formRef = ref<FormInstance>();
const loading = ref(false);

const rules: FormRules = {
  name: [{ required: true, message: '请输入店铺名', trigger: 'blur' }],
  contactPhone: [
    { required: true, message: '请输入联系电话', trigger: 'blur' },
    { pattern: /^\d{11}$/, message: '请输入 11 位手机号', trigger: 'blur' }
  ],
  ownerUsername: [
    { required: true, message: '请设置登录账号', trigger: 'blur' },
    { min: 4, max: 32, message: '4-32 位', trigger: 'blur' }
  ],
  ownerPassword: [
    { required: true, message: '请设置密码', trigger: 'blur' },
    { min: 6, message: '密码至少 6 位', trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: '请再次输入密码', trigger: 'blur' },
    {
      validator: (_rule, val, cb) => {
        if (val !== form.ownerPassword) cb(new Error('两次密码不一致'));
        else cb();
      },
      trigger: 'blur'
    }
  ]
};

async function submit() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  loading.value = true;
  try {
    const resp = await tenantsApi.register({
      name: form.name,
      contactPhone: form.contactPhone,
      contactName: form.contactName || null,
      ownerUsername: form.ownerUsername,
      ownerPassword: form.ownerPassword,
      ownerRealName: form.ownerRealName || null
    });
    ElMessage.success('注册成功');
    await ElMessageBox.alert(
      `账号「${resp.ownerUsername}」已开通 ${resp.trialDays} 天试用，试用至 ${new Date(resp.expireAt).toLocaleDateString('zh-CN')}。即将跳转到登录页。`,
      '欢迎使用',
      { confirmButtonText: '去登录', type: 'success' }
    ).catch(() => null);
    router.replace({ path: '/login', query: { u: resp.ownerUsername } });
  } finally {
    loading.value = false;
  }
}
</script>

<style scoped>
.register-bg {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #2d6a4f 0%, #40916c 100%);
  padding: 24px 0;
}
.register-card {
  width: 460px;
  padding: 16px 8px;
}
.register-card h1 {
  margin: 0;
  text-align: center;
  font-size: 22px;
}
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
