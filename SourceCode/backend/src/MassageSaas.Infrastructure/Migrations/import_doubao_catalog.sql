-- 导入豆包AI生成的全套配置表（服务项目价目表 + 会员类型体系 + 次卡套餐表）
-- 目标租户：1（天津齐源按摩中心）
-- 现有样例数据保留，仅叠加新编码（101-503 / V1-V4 / 601-606）

START TRANSACTION;

SET @tid := 1;
SET @now := UTC_TIMESTAMP(6);

-- ============================================================
-- 1) 服务项目价目表（含 501-503 三个套餐，统一落到 service_items）
-- ============================================================
INSERT INTO service_items
  (TenantId, Code, Name, DurationMinutes, Price, MemberPrice, Description, IsActive, Sort, CreatedAt, UpdatedAt, IsDeleted)
VALUES
  (@tid, '101', '全身推拿',       60,  88.00,  70.00, NULL, 1, 101, @now, @now, 0),
  (@tid, '102', '肩颈专项按摩',   40,  58.00,  46.00, NULL, 1, 102, @now, @now, 0),
  (@tid, '103', '腰背推拿',       40,  58.00,  46.00, NULL, 1, 103, @now, @now, 0),
  (@tid, '104', '头部放松按摩',   30,  38.00,  30.00, NULL, 1, 104, @now, @now, 0),
  (@tid, '105', '四肢舒缓按摩',   30,  38.00,  30.00, NULL, 1, 105, @now, @now, 0),
  (@tid, '201', '经络疏通推拿',   60,  98.00,  78.00, NULL, 1, 201, @now, @now, 0),
  (@tid, '202', '刮痧理疗',       30,  45.00,  36.00, NULL, 1, 202, @now, @now, 0),
  (@tid, '203', '真空拔罐',       30,  45.00,  36.00, NULL, 1, 203, @now, @now, 0),
  (@tid, '204', '艾灸温疗',       40,  68.00,  54.00, NULL, 1, 204, @now, @now, 0),
  (@tid, '205', '热敷理疗',       20,  28.00,  22.00, NULL, 1, 205, @now, @now, 0),
  (@tid, '301', '颈椎养护调理',   50,  78.00,  62.00, NULL, 1, 301, @now, @now, 0),
  (@tid, '302', '腰椎劳损调理',   50,  78.00,  62.00, NULL, 1, 302, @now, @now, 0),
  (@tid, '303', '关节舒缓调理',   40,  65.00,  52.00, NULL, 1, 303, @now, @now, 0),
  (@tid, '304', '安神助眠头疗',   45,  60.00,  48.00, NULL, 1, 304, @now, @now, 0),
  (@tid, '401', '经典足底按摩',   45,  65.00,  52.00, NULL, 1, 401, @now, @now, 0),
  (@tid, '402', '泡脚腿脚放松',   50,  72.00,  58.00, NULL, 1, 402, @now, @now, 0),
  (@tid, '501', '基础舒缓套餐',   70, 118.00,  94.00, '组合套餐', 1, 501, @now, @now, 0),
  (@tid, '502', '深度理疗套餐',   90, 158.00, 126.00, '组合套餐', 1, 502, @now, @now, 0),
  (@tid, '503', '尊享养生套餐',  120, 208.00, 166.00, '组合套餐', 1, 503, @now, @now, 0);

-- ============================================================
-- 2) 会员类型体系 V1-V4（充值卡 Kind=0）
-- ============================================================
INSERT INTO member_types
  (TenantId, Code, Name, Sort, Kind, ServiceItemId, MinRechargeAmount, MinPurchaseCount,
   Discount, BonusAmount, BonusCount, ValidDays, IsActive, Remark, CreatedAt, UpdatedAt, IsDeleted)
VALUES
  (@tid, 'V1', '普通会员',  1, 0, NULL,  300.00, NULL, 1.0000,  30.00, NULL, 365, 1,
   '充300送30；享门店会员价；消费累计积分，1元积1分',                       @now, @now, 0),
  (@tid, 'V2', '银卡会员',  2, 0, NULL,  500.00, NULL, 0.9800,  60.00, NULL, 365, 1,
   '充500送60；会员价基础9.8折；优先排单，积分1元积1.1分',                  @now, @now, 0),
  (@tid, 'V3', '金卡会员',  3, 0, NULL, 1000.00, NULL, 0.9500, 150.00, NULL, 365, 1,
   '充1000送150；会员价基础9.5折；免费热敷理疗1次，积分1元积1.2分',         @now, @now, 0),
  (@tid, 'V4', '钻石会员',  4, 0, NULL, 2000.00, NULL, 0.9000, 350.00, NULL, 365, 1,
   '充2000送350；会员价基础9折；每月免费拔罐1次，专属技师预约',             @now, @now, 0);

-- ============================================================
-- 3) 次卡套餐表 601-606（计次卡 Kind=10）
--    使用子查询绑定本租户的服务项目 Id；605/606 通用卡 ServiceItemId 留空
-- ============================================================
INSERT INTO member_types
  (TenantId, Code, Name, Sort, Kind, ServiceItemId, MinRechargeAmount, MinPurchaseCount,
   Discount, BonusAmount, BonusCount, ValidDays, IsActive, Remark, CreatedAt, UpdatedAt, IsDeleted)
VALUES
  (@tid, '601', '肩颈舒缓次卡', 601, 10,
     (SELECT Id FROM service_items WHERE TenantId=@tid AND Code='102' AND IsDeleted=0),
     NULL, 10, 1.0000, NULL, 0, 365, 1,
     '原价580 会员特惠价480；包含肩颈专项按摩×10次', @now, @now, 0),
  (@tid, '602', '腰背养护次卡', 602, 10,
     (SELECT Id FROM service_items WHERE TenantId=@tid AND Code='103' AND IsDeleted=0),
     NULL, 10, 1.0000, NULL, 0, 365, 1,
     '原价580 会员特惠价480；包含腰背推拿×10次', @now, @now, 0),
  (@tid, '603', '全身推拿次卡', 603, 10,
     (SELECT Id FROM service_items WHERE TenantId=@tid AND Code='101' AND IsDeleted=0),
     NULL, 10, 1.0000, NULL, 0, 365, 1,
     '原价880 会员特惠价720；包含全身推拿×10次', @now, @now, 0),
  (@tid, '604', '足底养生次卡', 604, 10,
     (SELECT Id FROM service_items WHERE TenantId=@tid AND Code='401' AND IsDeleted=0),
     NULL, 10, 1.0000, NULL, 0, 365, 1,
     '原价650 会员特惠价530；包含经典足底按摩×10次', @now, @now, 0),
  (@tid, '605', '综合调理次卡', 605, 10,
     NULL,
     NULL, 15, 1.0000, NULL, 0, 540, 1,
     '原价990 会员特惠价800；任选店内基础项目×15次，有效期18个月', @now, @now, 0),
  (@tid, '606', '尊享全能次卡', 606, 10,
     NULL,
     NULL, 20, 1.0000, NULL, 0, 720, 1,
     '原价1680 会员特惠价1350；全场项目通用×20次，有效期24个月', @now, @now, 0);

COMMIT;

-- 校验
SELECT '== service_items 新导入 ==' AS info;
SELECT TenantId, Code, Name, DurationMinutes, Price, MemberPrice, Sort
  FROM service_items WHERE TenantId=1 AND Code IN
  ('101','102','103','104','105','201','202','203','204','205',
   '301','302','303','304','401','402','501','502','503')
  ORDER BY Code;

SELECT '== member_types 新导入 ==' AS info;
SELECT TenantId, Code, Name, Kind, ServiceItemId, MinRechargeAmount, MinPurchaseCount,
       Discount, BonusAmount, ValidDays, Remark
  FROM member_types WHERE TenantId=1 AND Code IN
  ('V1','V2','V3','V4','601','602','603','604','605','606')
  ORDER BY Sort;
