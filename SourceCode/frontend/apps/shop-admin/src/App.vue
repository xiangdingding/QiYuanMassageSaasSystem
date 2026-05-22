<template>
  <router-view />
</template>

<script setup lang="ts"></script>

<style>
/* ---- 视口锁定 ---- */
html, body, #app {
  height: 100%;
  margin: 0;
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
</style>
