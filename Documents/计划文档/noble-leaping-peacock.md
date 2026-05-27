# 会员卡种（MembershipCardType）租户自治改造

## Context

当前 `Member` 已有 `Level` 枚举（Regular/Silver/Gold/Diamond，写死阈值 1000/5000/10000），但租户无法改名、改阈值、改默认折扣；充值赠送金额 `BonusAmount` 完全由收银员手填，没有按等级自动算的规则。

用户希望把"会员等级"重构为**租户自治的"会员卡种"主数据**，并由卡种规则驱动两件事：①默认折扣 ②充值赠送比例。`MemberPackage`（计次/期限卡）保持现有"会员套餐"独立入口，不并入卡种。`ServiceItem.MemberPrice` 对所有会员一视同仁，不区分卡种。

目标产出：每个按摩店店主可以在 shop-admin 里定义"普通卡 / 黄金卡 / 黑金卡"等卡种，并设阈值、默认折扣、赠送比例；会员充值时按规则自动跳档与算赠送，结账折扣链路保持现有 `Member.Discount` 不变。

## 实施步骤

### A. 数据模型（Domain + Infrastructure）

1. **新建 `MembershipCardType` 实体**
   - `backend/src/MassageSaas.Domain/Entities/MembershipCardType.cs`（新）
   - 字段：`Id`、`TenantId(ITenantScoped)`、`Code(32)`、`Name(64)`、`Sort(int)`、`MinRechargeThreshold(decimal 18,2)`、`DefaultDiscount(decimal 5,4)`、`BonusPercent(decimal 5,4)`、`IsActive`、`IsDefault`、`Remark(200?)`
   - 赠送规则采用**内联单字段** `BonusPercent`（非 JSON、非子表）；够用且可观测，后续要做阶梯再追加子表向上兼容
2. **修改 `Member`**
   - `backend/src/MassageSaas.Domain/Entities/Member.cs`
   - 新增 `CardTypeId (long?)` + 导航 `CardType`
   - 保留 `Level` 枚举一个版本不删（CS 客户端兼容、可回滚），写入时按 Code 近似映射同步刷新；下一个大版本再删
3. **EF 配置**
   - `backend/src/MassageSaas.Infrastructure/Persistence/Configurations/EntityConfigurations.cs` 增加 `MembershipCardTypeConfiguration`：
     - 唯一索引 `(TenantId, Code)`
     - 索引 `(TenantId, Sort)`
   - `MemberConfiguration` 加 `HasOne(x=>x.CardType).WithMany().HasForeignKey(x=>x.CardTypeId).OnDelete(DeleteBehavior.SetNull)` + 索引 `CardTypeId`
   - `ApplicationDbContext` 加 `DbSet<MembershipCardType>`

### B. EF Core 迁移与种子

新建迁移 `20260522xxxxxx_AddMembershipCardTypes`，路径 `backend/src/MassageSaas.Infrastructure/Migrations/`。

`Up` 顺序：
1. 建表 `membership_card_types`（字段对齐 A.1，含唯一/普通索引）
2. `members` 加列 `CardTypeId BIGINT NULL` + 外键 + 索引
3. **裸 SQL seed**（必须用 `migrationBuilder.Sql(...)`，**禁用 `dbContext.Add()`**——EF 的 `SaveChanges` 会注入当前 `TenantContext.TenantId` 为 null 导致全租户共享）：

```sql
-- 每个租户 seed 4 档默认卡种（复刻原硬编码阈值）
INSERT INTO membership_card_types
  (TenantId, Code, Name, Sort, MinRechargeThreshold, DefaultDiscount, BonusPercent,
   IsActive, IsDefault, CreatedAt, UpdatedAt, IsDeleted)
SELECT t.Id,'REGULAR','普通卡',0,0,1.0000,0.0000,1,1,NOW(6),NOW(6),0 FROM tenants t WHERE t.IsDeleted=0
UNION ALL SELECT t.Id,'SILVER','银卡',10,1000,0.9500,0.0000,1,0,NOW(6),NOW(6),0 FROM tenants t WHERE t.IsDeleted=0
UNION ALL SELECT t.Id,'GOLD','金卡',20,5000,0.9000,0.0500,1,0,NOW(6),NOW(6),0 FROM tenants t WHERE t.IsDeleted=0
UNION ALL SELECT t.Id,'DIAMOND','钻石卡',30,10000,0.8500,0.1000,1,0,NOW(6),NOW(6),0 FROM tenants t WHERE t.IsDeleted=0;

-- 现有 Member.Level → CardTypeId（同租户内 Code 映射）
UPDATE members m
JOIN membership_card_types c ON c.TenantId=m.TenantId
  AND c.Code = CASE m.Level WHEN 30 THEN 'DIAMOND' WHEN 20 THEN 'GOLD' WHEN 10 THEN 'SILVER' ELSE 'REGULAR' END
SET m.CardTypeId = c.Id WHERE m.CardTypeId IS NULL;
```

`Down`：删外键 → 删 `members.CardTypeId` → 删表 `membership_card_types`。

### C. Shared DTO

- 修改 `backend/src/MassageSaas.Shared/Members/MemberDtos.cs`
  - `MemberDto` 末尾追加 `CardTypeId, CardTypeCode, CardTypeName, CardTypeBonusPercent`
  - `CreateMemberRequest`、`UpdateMemberRequest` 加 `CardTypeId (long?)`（不传走自动匹配）
  - `RechargeRequest.BonusAmount` 标 `[Obsolete("由卡种自动计算")]`，服务端忽略
- 新建 `backend/src/MassageSaas.Shared/Members/MembershipCardTypeDtos.cs`
  - `MembershipCardTypeDto`、`CreateMembershipCardTypeRequest`、`UpdateMembershipCardTypeRequest`、`RecomputeMembersResult(int updated,int unchanged)`

### D. API

1. **新建 `backend/src/MassageSaas.Api/Controllers/MembershipCardTypesController.cs`**
   - Route `api/membership-card-types`，Policy `LeadOnly`（ShopOwner/StoreManager）
   - `GET /`（按 Sort，可 `includeInactive`）、`GET /{id}`、`POST /`、`PUT /{id}`、`DELETE /{id}`、`POST /recompute`
   - 校验：`Code` 租户内唯一、`Discount∈(0,1]`、`BonusPercent∈[0,1]`；`IsDefault=true` 时同事务清同租户其它 IsDefault
   - DELETE 软删：先查 `COUNT(members WHERE CardTypeId=?)`，>0 拒绝，提示先迁移
2. **改 `MembersController.cs`**
   - 删 `ComputeLevel` 静态阈值方法
   - `Recharge`：
     - 按 `MinRechargeThreshold DESC` 查租户卡种，取首个 `Threshold ≤ TotalRecharge` 的 `newCardType`
     - 若 `newCardType.Id != m.CardTypeId`（跳档）：`m.CardTypeId = newId`、`m.Discount = newCardType.DefaultDiscount`（覆盖）、同步刷 `m.Level`
     - **不跳档不动 Discount**，避免覆盖店主手工设置的 VIP 价
     - `bonus = round(req.Amount * currentCardType.BonusPercent, 2)`，忽略 `req.BonusAmount`（若传值打 warning）
   - `Create`：按 `InitialBalance` 匹配；缺省走 `IsDefault=true` 兜底卡
   - `Update`：接受 `CardTypeId` 手动指派（VIP 晋升场景），同步覆盖 Discount
3. **`OrdersController.ResolvePrice` 不动**——折扣链路仍读 `Member.Discount`

### E. 前端 shop-admin

1. **新建 `frontend/apps/shop-admin/src/views/MembershipCardTypesView.vue`**
   - `el-table`：Sort、Code、Name、阈值、折扣、赠送%、IsActive、IsDefault、操作
   - 新增/编辑 dialog：表单校验范围；`IsDefault` 互斥提示
   - 顶部按钮"按当前阈值重算所有会员"→ `POST /recompute`
2. **改 `frontend/apps/shop-admin/src/views/MembersView.vue`**
   - 列表加列"卡种"（`el-tag` + CardTypeName）
   - 开卡/编辑表单加 `el-select` 选卡种（可空走自动）
   - 充值 dialog 移除"赠送金额"输入框，改为只读预览"将按 X 卡种规则赠送 ¥Y"
3. **`frontend/apps/shop-admin/src/router/index.ts`**
   - 加路由 `membership-card-types`，`meta: { title: '会员卡种', icon: 'Postcard', menu: true, roles: LEAD }`，放在 `services` 与 `member-packages` 之间
4. **`frontend/apps/shop-admin/src/api/modules.ts`**
   - 新增 `cardTypesApi`：list / get / create / update / remove / recompute
   - `membersApi` DTO 类型同步加 CardType 字段

### F. CS 端（WPF）轻改

- `backend/src/MassageSaas.Cs/ViewModels/MembersViewModel.cs` + 对应 XAML
  - 列表行显示 `CardTypeName`；充值对话框去 BonusAmount 输入框（改只读预览）
  - 卡种管理不放 CS 端，避免双端维护；CS 仅消费

## 关键风险

- **R1 迁移 seed**：必须用 `migrationBuilder.Sql(...)`，绕过 `DbContext` 的 Global Query Filter / TenantContext 注入
- **R2 大表 UPDATE 锁**：迁移里 `UPDATE members JOIN ...` 在 members 量大时需分批；本期单租户数据量有限，先一次性
- **R3 折扣覆盖时机**：`DefaultDiscount` 仅在**跳档瞬间**回写 `Member.Discount`，否则会覆盖店主手设的 VIP 价
- **R4 删除卡种**：FK `ON DELETE SET NULL` + 业务层拒绝硬删（有会员引用直接 400）
- **R5 IsDefault 唯一**：MySQL 不支持 partial unique index，由 Controller 事务保证同租户最多一个

## 关键文件

- `backend/src/MassageSaas.Domain/Entities/Member.cs`
- `backend/src/MassageSaas.Domain/Entities/MembershipCardType.cs`（新）
- `backend/src/MassageSaas.Infrastructure/Persistence/ApplicationDbContext.cs`
- `backend/src/MassageSaas.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`
- `backend/src/MassageSaas.Infrastructure/Migrations/2026xxxx_AddMembershipCardTypes.cs`（新）
- `backend/src/MassageSaas.Api/Controllers/MembersController.cs`
- `backend/src/MassageSaas.Api/Controllers/MembershipCardTypesController.cs`（新）
- `backend/src/MassageSaas.Shared/Members/MemberDtos.cs`
- `backend/src/MassageSaas.Shared/Members/MembershipCardTypeDtos.cs`（新）
- `frontend/apps/shop-admin/src/views/MembersView.vue`
- `frontend/apps/shop-admin/src/views/MembershipCardTypesView.vue`（新）
- `frontend/apps/shop-admin/src/router/index.ts`
- `frontend/apps/shop-admin/src/api/modules.ts`
- `backend/src/MassageSaas.Cs/ViewModels/MembersViewModel.cs`（轻改）

## 验证

### 单元 / 集成测试（`backend/tests/MassageSaas.IntegrationTests`）
- `MembershipCardTypesControllerTests`：CRUD、唯一性约束、IsDefault 互斥、有引用拒删
- `MembersControllerRechargeTests`：
  - 迁移后 `Level=Silver, TotalRecharge=1500` 的旧会员 → 断言 `CardTypeId` 指向租户的 SILVER 行
  - 累计 800 → 充 300 → 跳到 SILVER，Discount 被覆盖为 0.95
  - 累计 6000 → 充 100 → 不跳档，Discount 保留店主手设值
  - 充 1000 在 GOLD（BonusPercent=0.05）→ Balance +1050；`req.BonusAmount=999` 应被忽略并打 warning
- 多租户隔离：租户 A 的卡种不出现在 B 的列表

### 手工 e2e（shop-admin）
1. 新建测试租户 → 4 张默认卡种自动出现
2. "会员卡种"页加一张"黑金卡" 阈值 20000、赠送 15%
3. 创建会员 → 初始充值 25000 → 自动绑定黑金卡、Discount=黑金折扣、赠送 3750
4. Update 手动指派为"普通卡"，Discount 跟随重置
5. 切租户 B 验证隔离

### 回滚演练
- `dotnet ef database update <previous migration>` 回滚成功；旧代码因 `Member.Level` 未删仍可跑
