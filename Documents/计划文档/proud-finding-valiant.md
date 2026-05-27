# 把"计时收费房"从房间管理迁到收银/技师小程序

## Context

当前 BS `RoomsView.vue` 既负责"房间 CRUD（管理基础数据）"又负责"计时房开台/结束（运营动作）"，把两种业务混在一张表里：
- 「计时单价」「占用 / 计时」两列对普通房常年显示 "—"，列结构稀疏；
- 操作列按 `isTimedRoom` 条件渲染"开始/结束计时"按钮，普通房没有，按钮密度不一致；
- 列表底部还挂着「今日计时房记录」二级表，跟"房间管理"语义错位。

更关键的是，现实业务里**收银员**才是结账的人——他在前台用 PosView 收钱，没理由切回房间管理页去停一个计时再回 PosView 收。**技师**在房间里服务，能第一时间看到客人进出，应该由他在小程序里开台。

本次重构：
1. RoomsView 退回**纯 CRUD**（含 isTimedRoom + hourlyRate 配置）；
2. **PosView 收银台**新增"计时房费"区域，收银员可看本店计时房状态、加入当前订单结算；
3. **技师小程序**新增"我的计时房"页，技师可开台、查看占用情况；
4. 计时房费在 cart 里作为 `kind: 'roomCharge'` 的特殊行，**结账时通过同一 PayMethod 调 timedRoomsApi.stop 走独立流水**（不改 schema），实现"一次结账，两条流水共享支付方式"的效果。

## 关键设计取舍

- **数据并行而非合并**：`TimedRoomSession` 与 `Order` 各自落库，不加 FK；用同一 PayMethod 保证报表一致。理由：`TimedRoomSession` 已有 `Amount/PayMethod/Status` 全套字段，日结报表已经在分别聚合两者；强行合并需要 OrderItem.ServiceId/TechnicianId 全部变可空，改动面太广。
- **技师小程序只"开台"不"结账"**：收钱是收银职责。技师小程序按"结束计时"只 UI 提示"请到前台结账"，不调 stop API。避免新加 status 或两阶段 stop 接口。
- **CS 端本轮不动**：CS WPF `RoomsView.xaml` 当前根本没有计时房 start/stop 按钮，只做 CRUD，符合新定位，零改动。
- **不做后端 schema 变更**：现有 `TimedRoomsController` 三个接口（start/stop/cancel）足够支撑新流程；`TimedRoomSession` 实体保持原状。

## 实施清单

### 1. BS `frontend/apps/shop-admin/src/views/RoomsView.vue`（瘦身）

- 去掉操作列里"开始计时 / 结束计时"两个按钮（保留编辑/删除）；
- 移除底部"今日计时房记录"那张 `el-card` + `el-table` 子表；
- 移除 `timedRoomsApi.sessions/start/stop/cancel` 调用与相关 reactive 状态（`timedSessions`、`startOpen/stopOpen` 等）和两个 dialog；
- **保留**：表头"计时单价"列（只读展示 `hourlyRate`，告诉店长本店配了什么单价）、状态列里"计时中"标签（仅作只读提示，不再附操作）；
- 表单（新建/编辑）继续支持勾选 `isTimedRoom` + 设 `hourlyRate`，CRUD 链路不变。

### 2. BS `frontend/apps/shop-admin/src/views/PosView.vue`（核心）

**CartItem 加 kind 字段**：
```ts
interface CartItem {
  kind: 'service' | 'roomCharge';
  // service 已有字段：serviceId/technicianId/roomId/unitPrice/quantity/durationMinutes/serviceName
  // roomCharge 专有：sessionId, roomId, roomNo, elapsedMinutes, hourlyRate, amount
}
```

**新增"计时房"卡片区域**（与"选择服务项目"并列，左栏底部或可折叠面板）：
- 拉一次 `roomsApi.list(storeId)` 过滤 `isTimedRoom`；拉 `timedRoomsApi.sessions(storeId)` 知道哪些有 Open session；
- 每间计时房一张卡：
  - 无 Open session → 按钮「开始计时」→ 弹现在 RoomsView 里那个"客户姓名 + 备注"小弹窗（直接复用 markup）→ 调 `timedRoomsApi.start` → 刷新本卡；
  - 有 Open session → 显示「已计时 N 分钟 ≈ ¥M.MM」→ 按钮「加入订单」→ 追加一行 `kind: 'roomCharge'` 的 cart item（前端构造 `serviceName: "${roomNo} 计时房 ${minutes} 分钟"`、`unitPrice = computed amount`、`quantity = 1`），并把 sessionId 记下。

**渲染 cart 列表区别对待**：
- service 行保留现有"技师/房间/数量"控件；
- roomCharge 行**不显示**技师下拉、房间下拉、数量控件，只显示项目名 + 金额 + 移除按钮。

**`canCheckout` / `doCheckout` 改造**：
- canCheckout：要求所有 service 行选好技师；roomCharge 行不参与该校验；至少 1 行（任意 kind）即可结账；
- doCheckout：
  - 把 cart 拆成 `services = cart.filter(c => c.kind === 'service')` 和 `roomCharges = cart.filter(c => c.kind === 'roomCharge')`；
  - **services 非空**时走原路径 `ordersApi.create` + `ordersApi.checkout`（含合并结算 secondaryMemberIds）；为空则跳过；
  - **roomCharges 非空**时遍历调 `timedRoomsApi.stop(sessionId, payMethod)`，所有 stop 使用同一 `payload.payMethod`；
  - 收据弹窗合并两类流水：原有的 OrderDto 字段 + roomCharge 列表汇总（金额、分钟、房号）；
- 错误处理：若 services 已落库但某个 roomCharge 的 stop 失败，提示用户该 session 仍是 Open，让其在 PosView 重试。

**会员校验注意**：roomCharge 不参与 [[feedback_industry_full_business_logic]] 的"次卡-服务匹配"校验（次卡不可能绑定到"计时房"这种虚拟项目）。

### 3. 技师小程序新增 `frontend/apps/miniprogram/pages/tech/timed-rooms/`

四件套：`timed-rooms.{ts, wxml, wxss, json}`，参照同目录下现有的 `performance/` 页结构。

- 顶部页签：店名 + 刷新按钮；
- 列表：本店所有 `isTimedRoom` 房间，每行：房号 / 类型 / 单价 / 当前状态（空闲 ↔ 计时中 N 分钟）/ 操作按钮；
- 操作：
  - 空闲 → 「开台」按钮 → 弹底部抽屉填客户姓名（可空，则记为散客）→ 调 `timedRoomsApi.start` → toast「已开台」 → 刷新；
  - 计时中 → 「通知前台结账」按钮 → **只弹** "请到前台结账，前台收钱后会自动结束计时"，**不调接口**；
- 严格遵循 [[盲人按摩业务全景对照]]：所有可点元素 `aria-label`，字号 ≥ 34rpx，金额朗读用 `yuanToReadable()`。

**`frontend/apps/miniprogram/utils/api.ts` 新增**：
```ts
timedRoomsApi: {
  list(storeId): Promise<Room[]>            // 仅 isTimedRoom 过滤在前端做
  sessions(storeId): Promise<Session[]>     // 用于判断哪些有 Open
  start(roomId, body): Promise<Session>
}
```

**技师 home 页（`pages/tech/home/`）入口卡片**：在现有"我的队列"下方加一张「计时房」卡片，跳转到新页。

### 4. 验证

**E2E（手动）**：
1. RoomsView 配置一间 `isTimedRoom = true, hourlyRate = 60` 的房间 `VIP1`；
2. **技师小程序**：登录技师账号，进入"计时房"页 → 看到 VIP1 空闲 → 开台 → 状态变为"计时中 0 分钟"；
3. **收银 BS**：进 PosView → 计时房区域看到 VIP1「已计时 N 分钟 ≈ ¥M.MM」→ 同时加 1 项普通"足疗 60min"服务 → 关联会员 → 现金结账；
4. 验证：
   - Order 落库（含足疗 OrderItem）、PaidAmount = 足疗金额；
   - TimedRoomSession 状态 → Settled、PayMethod = Cash、Amount = 房费；
   - 收据弹窗合计 = 足疗 + 房费；
   - 日结报表（DayCloseView）现金合计 = Order.PaidAmount + TimedRoomSession.Amount；
5. 边界：
   - 只加 roomCharge 不加 service → 仅 stop 调用，不落 Order，收据只显示房费一行；
   - 多间计时房同时加入订单 → 多次 stop 调用，全部用相同 PayMethod；
   - 技师小程序「通知前台结账」→ 不调任何写接口（grep 网络面板）。

**回归**：
- RoomsView 普通房 CRUD 不变；
- 次卡-服务匹配校验仍只看 service 行，不阻拦 roomCharge；
- CS WPF 不动。

## 关键文件

- `frontend/apps/shop-admin/src/views/RoomsView.vue` — 瘦身
- `frontend/apps/shop-admin/src/views/PosView.vue` — cart kind + 计时房区域 + doCheckout 拆分
- `frontend/apps/shop-admin/src/api/modules.ts` — `timedRoomsApi` 已存在，无新增
- `frontend/apps/miniprogram/pages/tech/timed-rooms/` — 新建四件套
- `frontend/apps/miniprogram/utils/api.ts` — 加 `timedRoomsApi`
- `frontend/apps/miniprogram/pages/tech/home/home.{ts,wxml}` — 加入口卡片
- 后端：**零改动**（`TimedRoomsController` 现有接口完全够用）
