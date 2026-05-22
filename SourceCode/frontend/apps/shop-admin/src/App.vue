<template>
  <router-view />
</template>

<script setup lang="ts"></script>

<style>
/* 视口高度归零 body 默认 margin，避免主布局 100vh 之外再多 8px 触发外层滚动条。
   登录 / 注册页用的是 min-height:100vh 自适应，故不强制 overflow:hidden，让
   小屏下注册表单仍可滚动。 */
html, body, #app {
  height: 100%;
  margin: 0;
}
</style>
