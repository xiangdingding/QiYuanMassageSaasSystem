<template>
  <router-view />
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { applyA11yModeToDom, usePrefsStore } from '@/stores/prefs';

// 启动时把存储的无障碍模式落到 <html data-a11y>，避免首屏样式闪烁
const prefs = usePrefsStore();
onMounted(() => applyA11yModeToDom(prefs.a11yMode));
</script>

<style>
/* ---- 视口锁定 ----
   外层 html / body / #app 锁死不滚，浏览器滚动条永不出现；
   主内容区（MainLayout 的 .main）自己 overflow:auto，超长内容由它内部的表格 / 卡片自滚。
   登录 / 注册等独立页面通过 min-height:100vh 适配视口，不依赖外层滚。 */
html, body, #app {
  height: 100%;
  margin: 0;
  overflow: hidden;
}
body {
  font-family: -apple-system, BlinkMacSystemFont, 'PingFang SC', 'Microsoft YaHei',
    'Helvetica Neue', Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  color: #2c3e50;
  background: #f5f7fa;
}

/* ---- Element Plus 主题色：盲人按摩品牌绿 ---- */
:root {
  --el-color-primary: #2d6a4f;
  --el-color-primary-light-3: #5a8b76;
  --el-color-primary-light-5: #87a99c;
  --el-color-primary-light-7: #b3c7be;
  --el-color-primary-light-8: #d5e0db;
  --el-color-primary-light-9: #eaf1ed;
  --el-color-primary-dark-2: #245540;
}

/* ---- 全局元素微调：圆角统一、阴影更柔 ---- */
.el-card {
  border-radius: 10px !important;
  border-color: var(--el-border-color-lighter);
}
.el-card.is-never-shadow {
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.03) !important;
}
.el-card__header {
  font-weight: 600;
  padding: 14px 18px !important;
  border-bottom: 1px solid var(--el-border-color-lighter);
}
.el-card__body { padding: 18px !important; }

.el-button {
  border-radius: 6px;
  font-weight: 500;
}
.el-button--large { border-radius: 8px; }

.el-tag { border-radius: 4px; }

.el-input__wrapper,
.el-select__wrapper,
.el-textarea__inner { border-radius: 6px; }

.el-table {
  --el-table-header-bg-color: #fafbfc;
  --el-table-header-text-color: #4a5568;
}
.el-table th.el-table__cell { font-weight: 600; }

.el-dialog { border-radius: 12px; overflow: hidden; }
.el-dialog__header {
  padding: 16px 20px;
  border-bottom: 1px solid var(--el-border-color-lighter);
  margin-right: 0;
}
.el-dialog__body { padding: 20px; }

/* 视图区统一边距：由 .main 提供，避免与各视图 scoped 的 .page 规则冲突 */

/* 紧凑标签去掉黑底感 */
.el-tag.el-tag--info { color: #4a5568; background: #f0f2f5; border-color: #e4e7eb; }

/* ===================================================================
   视口锁定全局规则
   所有 view 的根容器统一 class="page"；这里给所有"经典布局" view 自动套：
     .page 撑满 .main → el-card 撑满 .page → card-body flex column 不滚 →
     view 内部的 .table-wrap / .scroll-area 用 flex:1 + overflow:auto 自滚
   每个 view 只需保证根 div 是 .page，并把表格放在 .table-wrap 里即可。
   =================================================================== */
.main > .page {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;
  padding: 0;
}
.main > .page > .el-card {
  flex: 1 1 auto;
  display: flex;
  flex-direction: column;
  min-height: 0;
}
.main > .page > .el-card > .el-card__body {
  flex: 1 1 auto;
  display: flex;
  flex-direction: column;
  min-height: 0;
  overflow: hidden;
}
/* 表格 wrapper：view 把 <el-table height="100%"> 放进 .table-wrap 即得到内滚 */
.main .table-wrap { flex: 1 1 auto; min-height: 0; }
/* 分页 / 底部操作栏永远贴底，不参与滚动 */
.main .pager,
.main .footer-actions { flex: 0 0 auto; }

/* ===================================================================
   无障碍模式（data-a11y='a11y'）
   设计原则：
   - 字号 +2px，按钮 / 输入框最小 44px 触控目标（WCAG 2.5.5 AAA）
   - 焦点环加粗 + 高对比黄色，避免依赖纯颜色识别活动元素
   - 链接默认下划线，正文行高 1.7
   - 强制外层视口锁定：让屏幕阅读器一次扫完整页、避免主区域出滚动条
   - 减少非必要动画，听屏时减少噪音
   - 这是一套 CSS 覆盖；语义结构 (aria-label / role / live region) 各 view 自己挂
   =================================================================== */
:root[data-a11y='a11y'] {
  --el-font-size-base: 16px;
  font-size: 17px;
}
:root[data-a11y='a11y'] body { line-height: 1.7; color: #1a202c; }
:root[data-a11y='a11y'] a { text-decoration: underline; }

/* 焦点环：粗、高对比、有 offset，不依赖颜色识别 */
:root[data-a11y='a11y'] *:focus-visible {
  outline: 3px solid #ffd04b !important;
  outline-offset: 2px !important;
  border-radius: 4px;
}

/* Element Plus 控件：放大触控目标、加粗字号 */
:root[data-a11y='a11y'] .el-button {
  min-height: 44px;
  font-size: 15px;
  padding: 10px 18px;
}
:root[data-a11y='a11y'] .el-button--small { min-height: 36px; font-size: 14px; }
:root[data-a11y='a11y'] .el-input__inner,
:root[data-a11y='a11y'] .el-select__wrapper,
:root[data-a11y='a11y'] .el-textarea__inner {
  font-size: 15px;
}
:root[data-a11y='a11y'] .el-input--large .el-input__inner { font-size: 17px; }

/* 表格：行高加大、加粗字号、斑马纹更明显 */
:root[data-a11y='a11y'] .el-table {
  --el-table-row-hover-bg-color: #fff8df;
}
:root[data-a11y='a11y'] .el-table th.el-table__cell,
:root[data-a11y='a11y'] .el-table td.el-table__cell {
  padding: 12px 0;
  font-size: 15px;
}
:root[data-a11y='a11y'] .el-table tr.el-table__row:focus-within {
  background: #fff8df;
  outline: 2px solid #ffd04b;
}

/* 标签字号同步抬升，避免读屏跳过太小的状态字 */
:root[data-a11y='a11y'] .el-tag { font-size: 13px; padding: 0 10px; height: 26px; line-height: 24px; }

/* 操作列：含按钮的单元格在盲人模式下强制按钮一排不换行（列宽由各表 actCol() 加宽配合） */
:root[data-a11y='a11y'] .el-table .cell:has(.el-button) {
  white-space: nowrap;
  display: flex;
  flex-wrap: nowrap;
  align-items: center;
  gap: 6px;
}

/* 盲人模式：操作列里的"文字/链接"按钮一律显示为带边框背景的实体按钮，
   不能是纯文字样式——便于弱视/盲人辨识为可点击控件。类型色作为文字色保留（类似 plain 按钮）。 */
:root[data-a11y='a11y'] .el-table .cell .el-button.is-link,
:root[data-a11y='a11y'] .el-table .cell .el-button.is-text {
  border: 1px solid var(--el-border-color-dark);
  background-color: #fff;
  border-radius: 6px;
  padding: 9px 15px;
  min-height: 40px;
  text-decoration: none;
}
:root[data-a11y='a11y'] .el-table .cell .el-button.is-link:hover,
:root[data-a11y='a11y'] .el-table .cell .el-button.is-text:hover {
  background-color: var(--el-fill-color-light);
}

/* 菜单：放大 row 高度，更容易键盘命中 */
:root[data-a11y='a11y'] .menu :deep(.el-menu-item),
:root[data-a11y='a11y'] .el-menu-item { height: 52px !important; line-height: 52px !important; font-size: 16px; }

/* 视口锁定已经在外层 html/body/#app + .main 默认锁定，无障碍模式不再额外覆盖 */

/* 装饰性动画：减弱，避免读屏伴随的视觉抖动分心 */
:root[data-a11y='a11y'] *,
:root[data-a11y='a11y'] *::before,
:root[data-a11y='a11y'] *::after {
  transition-duration: 0.05s !important;
  animation-duration: 0.05s !important;
}

/* skip-link 在无障碍模式下默认就常驻顶部，方便键盘第一下 Tab 直接跳主内容 */
:root[data-a11y='a11y'] .skip-link {
  left: 8px !important;
  top: 8px !important;
  outline: 2px solid #ffd04b;
}

/* ===== 各 view 的"盲人放大"覆盖 =====
   Scoped style 默认走正常字号；无障碍模式由这里把关键文字/卡片放大。
   特异性 (0,4,0) > scoped (0,2,0)，无需 !important 即可覆盖。 */

/* MembersView：手机号 / 姓名 / 金额 三层放大；卡片留白也更宽 */
:root[data-a11y='a11y'] .member-card { padding: 16px 20px; }
:root[data-a11y='a11y'] .member-card .head-main { gap: 10px; }
:root[data-a11y='a11y'] .member-card .phone { font-size: 22px; letter-spacing: 0.5px; }
:root[data-a11y='a11y'] .member-card .name { font-size: 18px; }
:root[data-a11y='a11y'] .member-card .empty-tip { font-size: 16px; }
:root[data-a11y='a11y'] .member-card .stat { font-size: 15px; }
:root[data-a11y='a11y'] .member-card .stat-label { font-size: 13px; }
:root[data-a11y='a11y'] .member-card .stat-val { font-size: 16px; }
:root[data-a11y='a11y'] .member-card .stat-val.money { font-size: 18px; }
:root[data-a11y='a11y'] .member-card .card-line1 { font-size: 16px; }
:root[data-a11y='a11y'] .member-card .badge { font-size: 13px; padding: 3px 12px; }
</style>
