# MassageSaas.Api · IIS 部署说明

把后端 API 打包成可在 **IIS** 中运行的发布包，并让 **HTTP 与 HTTPS 两种方式都能访问**：

- **shop-admin（B/S 租户端）** 通过 **HTTP** 访问；
- **移动端 mobile / CS 端** 通过 **HTTPS** 访问。

实现方式：**同一个 IIS 站点同时绑定 http 和 https 两个端口，指向同一个发布目录**。应用本身**不强制跳转 https**（否则 http 请求会被 307 重定向，shop-admin 不可用）。客户端各自配置调用 http 还是 https 即可。

---

## 一、生成发布包

在项目根目录（`SourceCode/`）执行：

```powershell
pwsh ./backend/publish-iis.ps1
```

产物：

- 发布目录：`backend/src/MassageSaas.Api/bin/Release/net8.0/publish-iis/`
- 部署 zip：`SourceCode/MassageSaas.Api-IIS部署包-yyyyMMdd-HHmm.zip`

> 也可在 Visual Studio 里右键 `MassageSaas.Api` →「发布」→ 选 `IIS-Production` 配置。
> 发布为**自包含 win-x64**：服务器**无需单独装 .NET 运行时**，但 IIS 仍需安装下面第 2 步的 Hosting Bundle。

---

## 二、服务器准备（仅首次）

1. 启用 IIS：`服务器管理器 → 添加角色 → Web 服务器(IIS)`（含「应用程序开发」相关功能）。
2. 安装 **.NET 8 Hosting Bundle**（提供 ASP.NET Core Module V2，IIS 反代到应用的关键组件）：
   <https://dotnet.microsoft.com/download/dotnet/8.0> → ASP.NET Core Runtime → **Hosting Bundle**。
   安装后执行 `iisreset` 重启 IIS。
3. 确认 MySQL 8.0 可用，库名 `massage_saas`（应用首次启动会自动迁移建表）。

---

## 三、创建站点并配置 http + https 双绑定

1. 把发布目录（或解压后的 zip）放到服务器，例如 `D:\Sites\MassageSaasApi`。
2. IIS 管理器 →「网站」→ 右键「添加网站」：
   - **网站名称**：`MassageSaasApi`
   - **物理路径**：`D:\Sites\MassageSaasApi`
   - **绑定**先填 http，端口按需（如 `80` 或 `8080`）。
3. 选中该站点 →「绑定…」→「添加」：
   - 类型 **https**，端口 `443`（或自定义），选择服务器上的 **SSL 证书**
     （正式证书；测试可用自签证书，但移动端/浏览器会提示不受信任）。
4. 应用程序池：
   - **.NET CLR 版本** 选「**无托管代码**」（ASP.NET Core 走 ANCM，不用 CLR）。
   - 标识（Identity）账户需能访问 MySQL 与发布目录、`logs` 目录可写。

结果：同一站点两个绑定——
`http://服务器IP:80` 与 `https://服务器IP:443`，都指向同一份程序。

---

## 四、改生产配置

编辑发布目录里的 `appsettings.Production.json`（随包发布），把占位值改成真实值：

| 配置项 | 说明 |
|---|---|
| `ConnectionStrings:MySql` | 生产数据库连接串（真实账号/密码） |
| `Jwt:SecretKey` | **32 位以上**随机密钥，须与开发环境不同 |
| `Database:RunMigrationsOnStartup` | 首次/升级建议 `true`，稳定后可改 `false` |
| `Https:ForceRedirect` | 保持 `false`（设 `true` 会强制 http→https，shop-admin 失效） |
| `Swagger:Enabled` | 想用浏览器验证可临时设 `true`，验证完改回 `false` |

> `web.config` 已把 `ASPNETCORE_ENVIRONMENT` 设为 `Production`，所以会自动加载该文件。

改完在 IIS 里**重启站点**生效（改 `web.config` 会自动重启应用，改 `appsettings` 需手动重启站点或回收应用池）。

---

## 五、客户端指向

| 端 | 调用地址 | 配置位置 |
|---|---|---|
| shop-admin（B/S） | `http://服务器IP:端口` | 前端 `.env` / API baseURL |
| mobile（移动端） | `https://服务器IP:端口` | 移动端 API baseURL |
| CS 端（WPF） | `https://服务器IP:端口` | CS 端 `appsettings` / 服务地址 |

> 两端调用的是同一套接口、同一份数据，仅协议不同。CORS 已为任意来源放行（鉴权走 `Authorization: Bearer`，不依赖 Cookie）。

---

## 六、验证

1. 浏览器开 `http://服务器IP:端口/health` 和 `https://服务器IP:端口/health`，都应返回健康状态。
2. 临时把 `Swagger:Enabled` 设 `true`，访问 `/swagger` 确认两种协议接口都通，验证后改回。
3. shop-admin 用 http 登录、移动端/CS 用 https 登录各跑一遍。

---

## 七、常见问题

- **HTTP 500.19 / 找不到 AspNetCoreModuleV2**：没装 Hosting Bundle 或装完没 `iisreset`。
- **HTTP 访问被跳到 HTTPS**：`Https:ForceRedirect` 被设成了 `true`，改回 `false`。
- **502.5 进程启动失败**：多为连接串错误或数据库不可达；把 `web.config` 的 `stdoutLogEnabled` 临时设 `true` 看 `logs\stdout` 日志，或查应用自身的 `logs/massage-saas-*.log`（Serilog）。
- **应用池账户无权限**：`logs` 目录写不进、连不上 MySQL，给应用池标识授权即可。
