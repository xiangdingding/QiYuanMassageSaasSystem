# 齐源按摩门店 SaaS

多租户 SaaS 系统：平台方对外出租，按摩店按年付费使用。

## 交付端

- **后端 API**（ASP.NET Core 8 + EF Core + MySQL）
- **按摩店 CS 版**（WPF 桌面端，店内收银电脑使用）
- **按摩店 BS 版**（Vue3 Web，功能与 CS 版一致）
- **平台端**（Vue3 Web，SaaS 运营方使用）
- **微信小程序**（盲人技师自助 + 顾客预约，无障碍优化）

## 快速开始

详见 `CLAUDE.md`。

## 目录

```
backend/           .NET 解决方案（Api / Application / Domain / Infrastructure / Shared / Cs / Tests）
frontend/          前端 monorepo（platform-admin / shop-admin / miniprogram + packages）
docs/              设计文档、数据库 ER 图等
```

## 开发阶段

见项目计划 P0 基础设施 → P1 平台端+订阅 → P2 业务后端 → P3 BS 前端 → P4 CS 前端 → P5 小程序 → P6 联调验收。
