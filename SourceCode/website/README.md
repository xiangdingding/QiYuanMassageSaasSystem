# 天津齐源科技有限公司 · 官网

公司官方网站（静态站点，无需构建、可离线打开）。

## 内容

- 公司介绍：天津齐源科技有限公司
- 主营业务：软件定制开发
- 产品中心（五大 SaaS）：
  - 齐源按摩 SaaS 系统
  - 齐源球馆 SaaS 系统
  - 齐源收银 SaaS 系统
  - 齐源进销存 SaaS 系统
  - 齐源送货打印单 SaaS 系统
- 客服 / 技术支持（电话 / 微信同号）：**18911916819**

## 目录结构

```
website/
  index.html        # 单页官网（首屏 / 产品 / 定制开发 / 优势 / 关于 / 联系）
  css/style.css     # 全部样式（含响应式、深色页脚、动画）
  js/main.js        # 顶栏滚动、移动端菜单、滚动揭示动画
  assets/
    favicon.svg     # 站点图标（齐源 品牌色）
  README.md
```

## 本地预览

直接双击 `index.html` 即可在浏览器打开。

如需用本地服务器预览（推荐，链接/锚点更稳定）：

```bash
# 任选其一，在 website/ 目录下执行
python -m http.server 8080
# 然后浏览器访问 http://localhost:8080
```

## 技术说明

- 纯 HTML + CSS + 原生 JS，零第三方依赖，无需打包，加载快、易托管。
- 响应式布局，适配手机 / 平板 / 桌面。
- 无障碍：语义化标签、`aria-label`、键盘可达、对比度友好。
- 可直接部署到任意静态托管（Nginx、对象存储 + CDN、GitHub Pages 等）。

## 替换占位内容

### 各产品接入 / 下载地址（重要 · 上线前务必替换为真实地址）

每张产品卡片底部有三个链接：**网页端（BS）/ PC 端下载 / 移动端**，
对应 `index.html` 里各产品的 `<div class="product-access">`。当前均为**占位地址**：

| 产品 | 网页端（BS） | PC 端下载 | 移动端 |
|---|---|---|---|
| 齐源按摩 | `https://massage.qiyuan-tech.com` | `…/massage-setup.exe` | `…/massage-app.apk` |
| 齐源球馆 | `https://stadium.qiyuan-tech.com` | `…/stadium-setup.exe` | `…/stadium-app.apk` |
| 齐源收银 | `https://pos.qiyuan-tech.com` | `…/pos-setup.exe` | `…/pos-app.apk` |
| 齐源进销存 | `https://erp.qiyuan-tech.com` | `…/erp-setup.exe` | `…/erp-app.apk` |
| 齐源送货打印单 | `https://delivery.qiyuan-tech.com` | `…/delivery-setup.exe` | `…/delivery-app.apk` |

（PC / 移动端下载前缀均为 `https://dl.qiyuan-tech.com/`。）把每个 `href` 改成真实地址即可；
移动端如为微信小程序，可把链接改为引导文案或二维码。

### 其它占位

- 首屏「78W+ 累计服务门店」与各产品「使用门店」数字为宣传占位，按实际数据修改即可。
- 联系区/页脚的二维码目前为 SVG 占位图标，正式上线时可替换为真实微信二维码图片
  （把图片放到 `assets/`，在 `index.html` 的 `.wechat-qr` 处改为 `<img>` 即可）。
- 如需补充公司地址、备案号、营业执照等信息，可在页脚 `.footer-bar` 处添加。
