# 按摩店端 · 移动版（shop-mobile）

与 BS 版（shop-admin）**逻辑同源**的移动端 App，使用 **Vue 3 + TS + Vant + Capacitor**，
一套代码同时打包 **iOS 与 Android**，并兼容 Android 平板 / Android POS 一体机（大屏自动居中为手机宽列）。

## 技术与同源说明

- 复用 BS 端的 **API 端点契约**（`src/api/modules.ts` 与 `shop-admin` 一致）、**认证 store**、**门店选择逻辑**。
- UI 改用 Vant 触屏组件（替代 Element Plus），HTTP 提示改用 Vant Toast。
- 路由用 hash 模式（`createWebHashHistory`），适配 Capacitor 的 file:// 加载。

## 首批已实现（骨架 + 核心纵向切片）

| 模块 | 内容 |
|---|---|
| 登录 | 账号密码登录、服务器地址设置、按角色落地 |
| 首页 | 门店切换、今日经营 KPI（POS 角色）、技师「我的上钟」自助、常用功能宫格、订阅到期提醒 |
| 会员 | 搜索 / 下拉刷新 / 上拉加载 / 会员详情（余额、次卡、消费充值汇总） |
| 技师排队 | 实时排队、叫下一钟、设置技师状态、技师自助上钟/休息/下班、重置当日 |
| 我的 | 个人信息、门店切换、修改密码、服务器地址、退出登录 |

其余模块（收银台、预约、订单、房间、报表、评价、工资等）当前为占位页，按迭代逐步补全。

## 本地开发

```bash
# 在 frontend/ 目录
npm install
npm run dev:mobile          # http://localhost:5175 （dev 通过 proxy 连本地后端 5139）
```

浏览器里用手机视图（DevTools 设备模拟）即可调试。

## 打包成 App（iOS / Android）

```bash
# 在 frontend/apps/mobile 目录
npm run build                       # 产出 dist/
npx cap add android                 # 首次：生成原生 android 工程
npx cap add ios                     # 首次：生成原生 ios 工程（需 macOS）
npx cap sync                        # 每次 build 后同步 web 资源到原生工程

npm run android                     # = build + cap sync android + 打开 Android Studio
npm run ios                         # = build + cap sync ios + 打开 Xcode（需 macOS）
```

- **Android**：可在 Windows + Android Studio 直接编译、装到手机/平板/Android POS。
- **iOS**：需 **macOS + Xcode** 编译签名（Windows 无法产出 iOS 安装包）；代码本身两端通用。

## 重要：连接后端服务器地址

- dev（浏览器）走 Vite 代理 `/api → http://localhost:5139`。
- **打包进 App 后没有代理**，必须指定后端绝对地址：
  1. 运行时：登录页 / 「我的 > 服务器地址」填 `http://服务器IP:端口`（推荐，便于现场部署）；
  2. 构建期：设环境变量 `VITE_API_BASE=https://api.example.com` 后再 `npm run build`。
- 若后端是 http（非 https），Android 已开 `allowMixedContent`；生产建议用 https。

## 云端打包 APK（GitHub Actions，推荐 / 无需本机装环境）

本机未装 Android 工具链时，用 GitHub Actions 在云端构建 APK。工作流见仓库根
`.github/workflows/build-android-apk.yml`（注意 git 仓库根是 `齐源盲人按摩Saas系统/`，
`SourceCode/` 是其子目录）。

**前置：把仓库推到 GitHub（当前仅本地，无 remote）**

```bash
# 在仓库根 齐源盲人按摩Saas系统/ 下
git add -A
git commit -m "add mobile app + android apk workflow"
# 在 GitHub 新建空仓库后：
git remote add origin https://github.com/<你的账号>/<仓库名>.git
git push -u origin master
```

**触发构建**

1. GitHub 仓库页 → **Actions** → 选 **Build Android APK (shop-mobile)** → **Run workflow**。
2. 可选填 `api_base`（后端地址，留空则装机后在 App 内「服务器地址」填）。
3. 跑完（约 5–10 分钟）进入该次运行页面，底部 **Artifacts** 下载 `shop-mobile-apk`，
   解压得到 `app-debug.apk`，传到 Android 手机/平板/POS 安装（需允许“未知来源”）。

> debug 包用调试签名可直接安装；正式上架/对外分发请改 release 并配置签名密钥。
> 云端 ubuntu 运行器已预装 Android SDK，无需自备。

## 目录结构

```
src/
  api/        config.ts(服务器地址) http.ts(同源拦截器) types.ts modules.ts
  stores/     auth.ts  app.ts(门店选择)
  router/     index.ts(角色守卫)
  layouts/    TabLayout.vue(底部导航，按角色显示)
  views/      LoginView HomeView MembersView QueueView ProfileView PlaceholderView
  styles/     global.css(大屏居中、安全区)
capacitor.config.ts
```
