# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

盲人按摩门店 SaaS 系统。双层业务：**平台方**对外出租系统，**按摩店**（租户）按年付费使用。每个租户含总店与分店。

## 交付端

| 端 | 目录 | 技术栈 |
|---|---|---|
| 后端 API | `backend/src/MassageSaas.Api` | ASP.NET Core 8 + EF Core 8 + MySQL + Serilog |
| 按摩店 CS 版 | `backend/src/MassageSaas.Cs` | WPF (.NET 8) + CommunityToolkit.Mvvm + Refit |
| 按摩店 BS 版 | `frontend/apps/shop-admin` | Vue 3 + TS + Vite + Element Plus |
| 平台端 Web | `frontend/apps/platform-admin` | Vue 3 + TS + Vite + Element Plus |
| 小程序 | `frontend/apps/miniprogram` | 微信原生 + TS（主打无障碍） |

**重要：CS 版（WPF）与 BS 版（Web）功能完全一致**，仅表现层/部署/打印机对接不同。角色权限（店长/收银员/技师）控制菜单可见性。不要以为 CS 是收银专用、Web 是管理专用——两者都是全功能。

## 常用命令

### 后端（从 `backend/` 目录）

```bash
dotnet restore MassageSaas.sln
dotnet build MassageSaas.sln
dotnet run --project src/MassageSaas.Api           # http://localhost:5000 (swagger: /swagger)
dotnet test                                         # 全部测试
dotnet test tests/MassageSaas.UnitTests --filter FullyQualifiedName~CommissionRule  # 单个测试
dotnet ef migrations add <Name> --project src/MassageSaas.Infrastructure --startup-project src/MassageSaas.Api
dotnet ef database update --project src/MassageSaas.Infrastructure --startup-project src/MassageSaas.Api
```

### 前端（从 `frontend/` 目录）

```bash
npm install                        # 安装 monorepo workspaces
npm run dev:platform               # 平台端: http://localhost:5173
npm run dev:shop                   # 按摩店 BS 版: http://localhost:5174
npm run build:all                  # 构建所有 Vue 工程
```

### 小程序
用微信开发者工具打开 `frontend/apps/miniprogram/`。首次运行把 `project.config.json` 的 `appid` 改成自己的测试号。

## 本地依赖

- MySQL 8.0 监听 3306，库名 `massage_saas`（见 `appsettings.json`）
- Redis 7（P1 阶段起必需）
- 后端 `appsettings.Development.json` 用于本地连接串覆写（已在 .gitignore）

## 架构要点

### 多租户
- 共享 DB + `tenant_id` 字段。**所有业务表都实现 `ITenantScoped`**（见 `MassageSaas.Domain.Common`）。
- `ApplicationDbContext.OnModelCreating` 通过 Expression 为所有 `ITenantScoped` 实体批量挂载 Global Query Filter，读路径自动按 `tenantContext.TenantId` 过滤。
- 平台管理员（`UserRole.PlatformAdmin`）登录后 `TenantContext.IsFilterBypassed = true`，拥有跨租户视角。
- 新增跨租户接口时**必须**确认是否要 `tenantContext.BypassTenantFilter()`。

### 到期软锁
- `TenantStatusMiddleware`（`backend/src/MassageSaas.Api/Middleware/`）拦截写请求（POST/PUT/PATCH/DELETE）。
- 读请求（GET）即使到期也放行。
- 白名单：`/api/auth/*`、`/api/callback/*`、`/api/subscriptions/pay`、`/api/subscriptions/activate-offline`、`/health`。新增续费/回调接口要加入白名单。

### 认证
- JWT Bearer。Token 含 `tenant_id`、`store_id`、`role` 这三个自定义 claim。
- 平台端账号 `TenantId = null`。
- 登录接口 `/api/auth/login`（见 `AuthController`）。

### 支付
- 双通道：微信支付 V3 + 支付宝 + 线下手动激活。
- **所有回调必须幂等**：用 `payment_orders.third_trx` 做唯一索引，回调内先 `SELECT ... FOR UPDATE` 再更新状态。

## 项目布局（.NET 解决方案）

```
backend/
  MassageSaas.sln
  src/
    MassageSaas.Api              # Web API 启动项，Controllers + Middleware
    MassageSaas.Application      # CQRS/MediatR + FluentValidation + AutoMapper + 抽象接口
    MassageSaas.Domain           # 实体、枚举、值对象（无外部依赖）
    MassageSaas.Infrastructure   # EF Core DbContext + 配置 + 第三方集成（支付/短信/Redis/JWT）
    MassageSaas.Shared           # 跨端 DTO（被 CS 端引用）
    MassageSaas.Cs               # WPF 桌面端
  tests/
    MassageSaas.UnitTests        # xUnit + Moq + InMemory DbContext
    MassageSaas.IntegrationTests # WebApplicationFactory + Testcontainers.MySql
```

Application 依赖 EF Core 只为 `IApplicationDbContext` 暴露 `DbSet<T>`——这是务实简化，后期如有必要再抽出 `IQueryable<T>` 抽象。

## 盲人无障碍约定

- **小程序（盲人技师主入口）**：所有交互元素必须标 `aria-label`；字号 ≥ 34rpx；单列线性布局；金额朗读用 `yuanToReadable()`（`pages/tech/performance/performance.ts`）避免小数点歧义。新增页面前先查 `app.wxss` 里的 `.a11y-*` 工具类。
- **CS 端**：全键盘可达。新控件务必设置 `AutomationProperties.Name` 给读屏软件。
- 所有端避免纯颜色传递状态，辅以文本/图标。

## 构建/部署提示

- AutoMapper 12.0.1 有公告 GHSA-rvv3-g6hj-g44x（不可信数据反序列化）。我们只用于内部 DTO 映射，不暴露给用户输入，风险不适用；升级到 14+ 时 API 有破坏性变更，暂保留。
- `dotnet workload list` 会提示有更新——WPF 项目目标是 `net8.0-windows`，确保 Windows Desktop SDK 已装。
