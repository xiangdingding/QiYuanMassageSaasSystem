<template>
  <div class="site">
    <!-- 顶部导航 -->
    <header class="nav" :class="{ scrolled }">
      <div class="container nav-inner">
        <a class="brand" href="#top">
          <span class="brand-mark">齐</span>
          <span class="brand-text">齐源按摩 <em>SaaS</em></span>
        </a>
        <nav class="nav-links">
          <a href="#features">核心功能</a>
          <a href="#clients">多端协同</a>
          <a href="#a11y">无障碍</a>
          <a href="#why">为什么选我们</a>
        </nav>
        <div class="nav-cta">
          <a class="btn btn-ghost" :href="loginUrl" target="_blank" rel="noopener">店铺登录</a>
          <button class="btn btn-primary" @click="openConsult('nav')">业务咨询</button>
        </div>
      </div>
    </header>

    <!-- 英雄区 -->
    <section id="top" class="hero">
      <div class="hero-bg" aria-hidden="true">
        <span class="blob blob-1"></span>
        <span class="blob blob-2"></span>
        <span class="blob blob-3"></span>
      </div>
      <div class="container hero-inner">
        <div class="hero-copy">
          <span class="hero-badge">正常模式 / 无障碍模式 一键切换 · 明眼人、盲人都好用</span>
          <h1 class="hero-title">
            让每一家按摩店<br />
            <span class="grad">经营得更轻松、更体面</span>
          </h1>
          <p class="hero-sub">
            收银、会员、预约、技师提成、工资、库存、报表一站打通；
            网页端 / 桌面端 / 小程序 / 移动端四端协同。一套系统，明眼员工用正常模式，
            盲人技师切到无障碍模式，同样独立、有尊严地使用。
          </p>
          <div class="hero-actions">
            <button class="btn btn-primary btn-lg" @click="openConsult('hero')">免费申请开通</button>
            <a class="btn btn-outline btn-lg" :href="loginUrl" target="_blank" rel="noopener">已有账号 · 登录</a>
          </div>
          <div class="hero-stats">
            <div class="stat"><b>4</b><span>端协同</span></div>
            <div class="stat"><b>20+</b><span>业务模块</span></div>
            <div class="stat"><b>30</b><span>天免费试用</span></div>
            <div class="stat"><b>A11y</b><span>无障碍优先</span></div>
          </div>
        </div>
        <div class="hero-card" aria-hidden="true">
          <div class="hc-row hc-head"><span>今日营业</span><span class="hc-amt">¥ 8,640</span></div>
          <div class="hc-bars">
            <i style="height: 38%"></i><i style="height: 62%"></i><i style="height: 48%"></i>
            <i style="height: 80%"></i><i style="height: 66%"></i><i style="height: 92%"></i>
            <i style="height: 74%"></i>
          </div>
          <div class="hc-list">
            <div><span class="dot g"></span>钟单收入</div><b>¥ 6,120</b>
            <div><span class="dot b"></span>会员充值</div><b>¥ 1,800</b>
            <div><span class="dot y"></span>商品销售</div><b>¥ 720</b>
          </div>
        </div>
      </div>
    </section>

    <!-- 信任条 -->
    <section class="trust-band">
      <div class="container trust-grid">
        <div><b>收银到提成全闭环</b><span>下单即算钟、自动核算技师业绩与工资</span></div>
        <div><b>会员资产清清楚楚</b><span>充值卡 / 次卡 / 套餐 / 优惠券一体管理</span></div>
        <div><b>数据安全可托付</b><span>阿里云存储与备份，经营数据有保障</span></div>
      </div>
    </section>

    <!-- 核心功能 -->
    <section id="features" class="section">
      <div class="container">
        <div class="section-head">
          <span class="section-eyebrow">CORE FEATURES</span>
          <h2 class="section-title">门店经营，一个系统就够了</h2>
          <p class="section-sub">从前台收银到后台核算，覆盖按摩门店每天真实发生的业务。</p>
        </div>
        <div class="feature-grid">
          <article v-for="f in features" :key="f.title" class="feature-card">
            <div class="feature-ico">{{ f.icon }}</div>
            <h3>{{ f.title }}</h3>
            <p>{{ f.desc }}</p>
          </article>
        </div>
      </div>
    </section>

    <!-- 多端协同 -->
    <section id="clients" class="section section-alt">
      <div class="container clients">
        <div class="clients-copy">
          <span class="section-eyebrow">MULTI-CLIENT</span>
          <h2 class="section-title">四端协同，各司其职</h2>
          <p class="section-sub">同一套数据、同一套权限，门店按场景自由选择终端。</p>
          <ul class="clients-list">
            <li v-for="c in clients" :key="c.name">
              <b>{{ c.name }}</b><span>{{ c.desc }}</span>
            </li>
          </ul>
        </div>
        <div class="clients-art" aria-hidden="true">
          <div class="device d-web"><span>网页端</span></div>
          <div class="device d-cs"><span>桌面端</span></div>
          <div class="device d-mini"><span>小程序</span></div>
          <div class="device d-mobile"><span>移动端</span></div>
        </div>
      </div>
    </section>

    <!-- 无障碍 -->
    <section id="a11y" class="section a11y">
      <div class="container a11y-inner">
        <div class="a11y-badge" aria-hidden="true">♿</div>
        <span class="section-eyebrow on-dark">NORMAL / ACCESSIBILITY MODE</span>
        <h2 class="section-title on-dark">正常模式与无障碍模式，一键切换</h2>
        <p class="section-sub on-dark">
          明眼员工用正常模式，界面紧凑高效；盲人技师一键切到无障碍模式，
          自动大字号、强对比、纯线性布局、全键盘可达，金额语音友好朗读，读屏软件下每个按钮都有清晰标签。
          同一套系统、同一份数据，谁来用都顺手——盲人技师也能独立查业绩、看排班、对工资，无需他人代劳。
        </p>
        <div class="a11y-tags">
          <span>一键模式切换</span><span>大字号</span><span>读屏适配</span><span>全键盘操作</span>
          <span>金额防歧义朗读</span><span>高对比配色</span>
        </div>
      </div>
    </section>

    <!-- 为什么选我们 -->
    <section id="why" class="section">
      <div class="container">
        <div class="section-head">
          <span class="section-eyebrow">WHY US</span>
          <h2 class="section-title">为什么按摩店选择我们</h2>
        </div>
        <div class="why-grid">
          <div v-for="w in reasons" :key="w.title" class="why-card">
            <div class="why-num">{{ w.no }}</div>
            <h3>{{ w.title }}</h3>
            <p>{{ w.desc }}</p>
          </div>
        </div>
      </div>
    </section>

    <!-- 阿里云信任条 -->
    <section class="aliyun">
      <div class="container aliyun-inner">
        <span class="shield" aria-hidden="true">🛡️</span>
        本系统由阿里云提供数据存储、备份均由阿里巴巴（中国）提供，更值得托付！
      </div>
    </section>

    <!-- 咨询 CTA -->
    <section class="cta">
      <div class="container cta-inner">
        <div>
          <h2 class="cta-title">想了解或开通系统？留个电话，我们联系您</h2>
          <p class="cta-sub">填写联系电话与咨询内容，平台顾问会尽快回访，为您演示与报价。</p>
        </div>
        <button class="btn btn-light btn-lg" @click="openConsult('cta')">立即业务咨询</button>
      </div>
    </section>

    <!-- 页脚 -->
    <footer class="footer">
      <div class="container footer-inner">
        <div class="footer-brand">
          <span class="brand-mark">齐</span>
          <div>
            <b>齐源按摩 SaaS 系统</b>
            <p>为按摩门店打造的全功能数字化经营平台 · 正常 / 无障碍双模式</p>
          </div>
        </div>
        <div class="footer-meta">
          <a :href="loginUrl" target="_blank" rel="noopener">店铺登录</a>
          <button class="link-btn" @click="openConsult('footer')">业务咨询</button>
          <span>© {{ year }} 齐源 · 按摩门店 SaaS</span>
        </div>
      </div>
    </footer>

    <!-- 悬浮咨询按钮 -->
    <button class="fab" @click="openConsult('fab')" aria-label="业务咨询">
      <span class="fab-ico" aria-hidden="true">💬</span>
      <span class="fab-text">业务咨询</span>
    </button>

    <!-- 咨询弹窗 -->
    <el-dialog
      v-model="consultVisible"
      title="业务咨询"
      :width="dialogWidth"
      append-to-body
      class="consult-dialog"
    >
      <p class="consult-tip">留下您的联系电话与需求，平台顾问会尽快与您联系（信息仅用于业务回访）。</p>
      <el-form
        ref="formRef"
        :model="form"
        :rules="rules"
        label-position="top"
        @submit.prevent
      >
        <el-form-item label="称呼（选填）" prop="contactName">
          <el-input v-model="form.contactName" placeholder="如：王店长" maxlength="64" />
        </el-form-item>
        <el-form-item label="联系电话" prop="phone">
          <el-input v-model="form.phone" placeholder="方便回访的手机号 / 座机" maxlength="32" />
        </el-form-item>
        <el-form-item label="咨询内容" prop="content">
          <el-input
            v-model="form.content"
            type="textarea"
            :rows="4"
            maxlength="2000"
            show-word-limit
            placeholder="想了解的功能、门店规模、价格，或希望预约演示的时间等"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="consultVisible = false">取消</el-button>
        <el-button type="primary" :loading="submitting" @click="submit">提交咨询</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { consultationApi } from './api';

// 店铺登录入口地址：按实际部署改成 shop-admin 的访问地址（默认同源 /login）
const loginUrl = '/login';

const year = new Date().getFullYear();

const features = [
  { icon: '🧾', title: '前台收银 POS', desc: '开钟、点单、会员结算、混合支付，钟单实时入账。' },
  { icon: '💳', title: '会员与储值', desc: '充值卡 / 次卡 / 套餐 / 折扣 / 优惠券，会员资产清晰。' },
  { icon: '📅', title: '预约与排钟', desc: '电话/小程序预约、技师轮钟点钟、计时房间管理。' },
  { icon: '🧮', title: '技师提成', desc: '按服务/技师配置提成规则，下单自动算钟、可查可对。' },
  { icon: '💰', title: '工资核算', desc: '底薪+提成+补贴一键生成工资单，导出发放无差错。' },
  { icon: '📦', title: '库存进销存', desc: '商品/耗材入库出库、盘点预警，成本一目了然。' },
  { icon: '📊', title: '经营报表', desc: '营业额、客流、技师业绩、会员增长多维报表。' },
  { icon: '🏬', title: '总店分店', desc: '一个账号管多店，权限到岗，数据按店隔离。' }
];

const clients = [
  { name: '网页端（BS）', desc: '开箱即用，店长在任意电脑浏览器管理门店全业务。' },
  { name: '桌面端（CS）', desc: 'Windows 客户端，对接小票打印，收银台稳定高效。' },
  { name: '微信小程序', desc: '顾客在线预约、查券查卡；技师无障碍模式查业绩。' },
  { name: '移动 App', desc: '店长随身管店，技师手机看排班、对工资。' }
];

const reasons = [
  { no: '01', title: '专注按摩门店行业', desc: '从行业真实流程出发，不是通用系统硬套，贴合门店日常。' },
  { no: '02', title: '正常 / 无障碍双模式', desc: '一键切换，明眼员工与盲人技师同用一套系统，读屏、大字、语音朗读全覆盖。' },
  { no: '03', title: '功能全且各端一致', desc: 'BS 与 CS 功能完全等价，按角色差异化菜单，不阉割。' },
  { no: '04', title: '数据安全有保障', desc: '阿里云存储与备份，经营数据稳妥可托付。' }
];

const scrolled = ref(false);
function onScroll() { scrolled.value = window.scrollY > 24; }
onMounted(() => window.addEventListener('scroll', onScroll, { passive: true }));
onUnmounted(() => window.removeEventListener('scroll', onScroll));

const dialogWidth = computed(() => (window.innerWidth < 560 ? '92vw' : '460px'));

const consultVisible = ref(false);
const submitting = ref(false);
const formRef = ref<FormInstance>();
const form = reactive<{ contactName: string; phone: string; content: string; source: string }>({
  contactName: '',
  phone: '',
  content: '',
  source: 'website'
});

const rules: FormRules = {
  phone: [
    { required: true, message: '请填写联系电话', trigger: 'blur' },
    { min: 5, max: 32, message: '请输入正确的联系电话', trigger: 'blur' }
  ],
  content: [{ required: true, message: '请填写咨询内容', trigger: 'blur' }]
};

function openConsult(source: string) {
  form.source = `website:${source}`;
  consultVisible.value = true;
}

async function submit() {
  if (!formRef.value) return;
  const ok = await formRef.value.validate().catch(() => false);
  if (!ok) return;
  submitting.value = true;
  try {
    const res = await consultationApi.submit({
      contactName: form.contactName || null,
      phone: form.phone.trim(),
      content: form.content.trim(),
      source: form.source
    });
    ElMessage.success(res.message || '提交成功，我们会尽快与您联系');
    consultVisible.value = false;
    form.contactName = '';
    form.phone = '';
    form.content = '';
    formRef.value.resetFields();
  } catch {
    ElMessage.error('提交失败，请稍后重试或直接电话联系我们');
  } finally {
    submitting.value = false;
  }
}
</script>

<style scoped>
/* ===== 通用按钮 ===== */
.btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 6px;
  border: 1px solid transparent;
  border-radius: 999px;
  padding: 10px 22px;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  transition: transform 0.15s ease, box-shadow 0.2s ease, background 0.2s ease;
  font-family: inherit;
}
.btn:hover { transform: translateY(-1px); }
.btn-lg { padding: 14px 30px; font-size: 16px; }
.btn-primary { background: var(--brand-700); color: #fff; box-shadow: 0 10px 24px rgba(45, 106, 79, 0.32); }
.btn-primary:hover { background: var(--brand-800); }
.btn-ghost { background: transparent; color: var(--brand-800); }
.btn-ghost:hover { background: var(--brand-100); }
.btn-outline { background: rgba(255, 255, 255, 0.85); color: var(--brand-800); border-color: var(--brand-200); }
.btn-light { background: #fff; color: var(--brand-800); box-shadow: 0 10px 24px rgba(0, 0, 0, 0.18); }

/* ===== 顶部导航 ===== */
.nav {
  position: sticky;
  top: 0;
  z-index: 50;
  transition: background 0.3s ease, box-shadow 0.3s ease;
}
.nav.scrolled {
  background: rgba(255, 255, 255, 0.92);
  backdrop-filter: blur(10px);
  box-shadow: 0 2px 16px rgba(27, 67, 50, 0.08);
}
.nav-inner { display: flex; align-items: center; justify-content: space-between; height: 68px; }
.brand { display: flex; align-items: center; gap: 10px; font-weight: 800; font-size: 18px; color: var(--brand-900); }
.brand-mark {
  width: 34px; height: 34px; border-radius: 9px;
  background: linear-gradient(135deg, var(--brand-600), var(--brand-800));
  color: #fff; display: grid; place-items: center; font-size: 18px;
}
.brand-text em { font-style: normal; color: var(--brand-600); }
.nav-links { display: flex; gap: 28px; font-weight: 600; color: var(--ink-soft); }
.nav-links a:hover { color: var(--brand-700); }
.nav-cta { display: flex; gap: 10px; }

/* ===== 英雄区 ===== */
.hero { position: relative; overflow: hidden; padding: 64px 0 88px; }
.hero-bg { position: absolute; inset: 0; z-index: 0; }
.blob { position: absolute; border-radius: 50%; filter: blur(8px); opacity: 0.55; }
.blob-1 { width: 460px; height: 460px; top: -160px; right: -80px; background: radial-gradient(circle, var(--brand-200), transparent 70%); }
.blob-2 { width: 380px; height: 380px; top: 120px; left: -140px; background: radial-gradient(circle, var(--brand-100), transparent 70%); }
.blob-3 { width: 300px; height: 300px; bottom: -120px; right: 26%; background: radial-gradient(circle, #cdeede, transparent 70%); }
.hero-inner {
  position: relative; z-index: 1;
  display: grid; grid-template-columns: 1.15fr 0.85fr; gap: 48px; align-items: center;
}
.hero-badge {
  display: inline-block; background: var(--brand-100); color: var(--brand-800);
  font-weight: 700; font-size: 13px; padding: 7px 14px; border-radius: 999px; margin-bottom: 20px;
}
.hero-title { font-size: clamp(34px, 5vw, 56px); line-height: 1.16; font-weight: 900; margin: 0 0 18px; letter-spacing: 1px; }
.grad { background: linear-gradient(100deg, var(--brand-700), var(--brand-400)); -webkit-background-clip: text; background-clip: text; color: transparent; }
.hero-sub { font-size: clamp(15px, 1.7vw, 19px); line-height: 1.85; color: var(--ink-soft); margin: 0 0 30px; max-width: 560px; }
.hero-actions { display: flex; gap: 14px; flex-wrap: wrap; margin-bottom: 36px; }
.hero-stats { display: flex; gap: 34px; flex-wrap: wrap; }
.stat { display: flex; flex-direction: column; }
.stat b { font-size: 28px; font-weight: 900; color: var(--brand-700); }
.stat span { font-size: 13px; color: var(--ink-soft); }

/* 英雄区配图卡片 */
.hero-card {
  background: #fff; border-radius: 20px; padding: 22px;
  box-shadow: 0 30px 70px rgba(27, 67, 50, 0.18); border: 1px solid var(--brand-100);
}
.hc-row { display: flex; justify-content: space-between; align-items: baseline; }
.hc-head span:first-child { color: var(--ink-soft); font-weight: 600; }
.hc-amt { font-size: 28px; font-weight: 900; color: var(--brand-700); }
.hc-bars { display: flex; align-items: flex-end; gap: 10px; height: 120px; margin: 20px 0; }
.hc-bars i { flex: 1; border-radius: 6px 6px 0 0; background: linear-gradient(180deg, var(--brand-400), var(--brand-600)); }
.hc-list { display: grid; grid-template-columns: 1fr auto; gap: 10px 12px; align-items: center; }
.hc-list div { color: var(--ink-soft); display: flex; align-items: center; gap: 8px; font-size: 14px; }
.hc-list b { text-align: right; font-weight: 700; }
.dot { width: 9px; height: 9px; border-radius: 50%; display: inline-block; }
.dot.g { background: var(--brand-600); }
.dot.b { background: #3b82f6; }
.dot.y { background: #f0a020; }

/* ===== 信任条 ===== */
.trust-band { background: var(--brand-900); }
.trust-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; padding: 28px 0; }
.trust-grid div { display: flex; flex-direction: column; gap: 4px; }
.trust-grid b { color: #fff; font-size: 17px; }
.trust-grid span { color: var(--brand-200); font-size: 14px; }

/* ===== 通用区块 ===== */
.section { padding: 84px 0; }
.section-alt { background: #fff; }
.section-head { text-align: center; max-width: 720px; margin: 0 auto 48px; }

/* 功能网格 */
.feature-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 22px; }
.feature-card {
  background: #fff; border: 1px solid #e8f1ec; border-radius: 16px; padding: 26px 22px;
  transition: transform 0.18s ease, box-shadow 0.2s ease, border-color 0.2s ease;
}
.feature-card:hover { transform: translateY(-4px); box-shadow: 0 18px 40px rgba(27, 67, 50, 0.12); border-color: var(--brand-200); }
.feature-ico {
  width: 52px; height: 52px; border-radius: 14px; display: grid; place-items: center; font-size: 26px;
  background: linear-gradient(135deg, var(--brand-100), var(--brand-200)); margin-bottom: 16px;
}
.feature-card h3 { margin: 0 0 8px; font-size: 18px; }
.feature-card p { margin: 0; color: var(--ink-soft); line-height: 1.7; font-size: 14px; }

/* 多端 */
.clients { display: grid; grid-template-columns: 1fr 1fr; gap: 56px; align-items: center; }
.clients-list { list-style: none; padding: 0; margin: 26px 0 0; display: grid; gap: 16px; }
.clients-list li { display: flex; flex-direction: column; gap: 3px; padding-left: 18px; border-left: 3px solid var(--brand-400); }
.clients-list b { font-size: 16px; }
.clients-list span { color: var(--ink-soft); font-size: 14px; }
.clients-art { position: relative; height: 360px; }
.device {
  position: absolute; border-radius: 16px; color: #fff; display: grid; place-items: center;
  font-weight: 700; box-shadow: 0 20px 50px rgba(27, 67, 50, 0.18);
}
.d-web { width: 58%; height: 58%; top: 0; left: 0; background: linear-gradient(135deg, var(--brand-600), var(--brand-800)); }
.d-cs { width: 42%; height: 44%; top: 16%; right: 0; background: linear-gradient(135deg, var(--brand-700), var(--brand-900)); }
.d-mini { width: 34%; height: 40%; bottom: 0; left: 12%; background: linear-gradient(135deg, var(--brand-400), var(--brand-600)); }
.d-mobile { width: 30%; height: 50%; bottom: 4%; right: 14%; background: linear-gradient(135deg, var(--brand-600), var(--brand-700)); }

/* 无障碍区 */
.a11y { background: linear-gradient(135deg, var(--brand-800), var(--brand-900)); }
.a11y-inner { text-align: center; max-width: 820px; margin: 0 auto; }
.a11y-badge {
  width: 72px; height: 72px; border-radius: 20px; margin: 0 auto 22px; display: grid; place-items: center;
  font-size: 36px; background: rgba(255, 255, 255, 0.12); border: 1px solid rgba(255, 255, 255, 0.25);
}
.on-dark { color: #fff !important; }
.section-eyebrow.on-dark, .a11y .section-eyebrow { color: var(--brand-200); }
.a11y .section-sub.on-dark { color: var(--brand-100); }
.a11y-tags { display: flex; flex-wrap: wrap; gap: 12px; justify-content: center; margin-top: 26px; }
.a11y-tags span {
  background: rgba(255, 255, 255, 0.12); color: #fff; border: 1px solid rgba(255, 255, 255, 0.25);
  padding: 8px 16px; border-radius: 999px; font-weight: 600; font-size: 14px;
}

/* 为什么选我们 */
.why-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 22px; }
.why-card { background: #fff; border: 1px solid #e8f1ec; border-radius: 16px; padding: 28px 22px; }
.why-num { font-size: 30px; font-weight: 900; color: var(--brand-200); margin-bottom: 10px; }
.why-card h3 { margin: 0 0 10px; font-size: 18px; }
.why-card p { margin: 0; color: var(--ink-soft); line-height: 1.7; font-size: 14px; }

/* 阿里云条 */
.aliyun { background: var(--brand-100); }
.aliyun-inner {
  display: flex; align-items: center; justify-content: center; gap: 10px;
  padding: 18px 0; text-align: center; color: var(--brand-900); font-weight: 700; font-size: clamp(14px, 1.6vw, 17px);
}
.shield { font-size: 20px; }

/* 咨询 CTA */
.cta { background: linear-gradient(120deg, var(--brand-700), var(--brand-600)); }
.cta-inner { display: flex; align-items: center; justify-content: space-between; gap: 28px; padding: 60px 0; flex-wrap: wrap; }
.cta-title { color: #fff; font-size: clamp(22px, 3vw, 34px); font-weight: 800; margin: 0 0 10px; }
.cta-sub { color: var(--brand-100); margin: 0; font-size: 16px; line-height: 1.7; }

/* 页脚 */
.footer { background: var(--brand-900); color: var(--brand-200); padding: 40px 0; }
.footer-inner { display: flex; align-items: center; justify-content: space-between; gap: 24px; flex-wrap: wrap; }
.footer-brand { display: flex; align-items: center; gap: 14px; }
.footer-brand b { color: #fff; font-size: 16px; }
.footer-brand p { margin: 4px 0 0; font-size: 13px; color: var(--brand-200); }
.footer-meta { display: flex; align-items: center; gap: 22px; font-size: 14px; }
.footer-meta a:hover, .link-btn:hover { color: #fff; }
.link-btn { background: none; border: none; color: var(--brand-200); cursor: pointer; font: inherit; padding: 0; }

/* 悬浮咨询按钮 */
.fab {
  position: fixed; right: 24px; bottom: 24px; z-index: 60;
  display: flex; align-items: center; gap: 8px;
  background: var(--brand-700); color: #fff; border: none; cursor: pointer;
  padding: 14px 20px; border-radius: 999px; font-weight: 700; font-size: 15px;
  box-shadow: 0 14px 34px rgba(45, 106, 79, 0.42);
  transition: transform 0.15s ease, background 0.2s ease;
}
.fab:hover { transform: translateY(-2px); background: var(--brand-800); }
.fab-ico { font-size: 18px; }

/* 咨询弹窗 */
.consult-tip { margin: 0 0 14px; color: var(--ink-soft); font-size: 13px; line-height: 1.7; }

/* ===== 响应式 ===== */
@media (max-width: 900px) {
  .nav-links { display: none; }
  .hero-inner { grid-template-columns: 1fr; }
  .hero-card { display: none; }
  .feature-grid { grid-template-columns: repeat(2, 1fr); }
  .why-grid { grid-template-columns: repeat(2, 1fr); }
  .clients { grid-template-columns: 1fr; }
  .clients-art { display: none; }
  .trust-grid { grid-template-columns: 1fr; }
  .fab-text { display: none; }
  .fab { padding: 14px; }
}
@media (max-width: 540px) {
  .feature-grid, .why-grid { grid-template-columns: 1fr; }
  .hero-stats { gap: 22px; }
}
</style>
