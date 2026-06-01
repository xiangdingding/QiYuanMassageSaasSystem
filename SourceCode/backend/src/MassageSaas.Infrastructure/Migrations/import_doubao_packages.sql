-- 把 501-503 从 service_items 迁移到 service_packages，并写入子项
-- 组合方式：升级型（舒缓 / 深度理疗 / 尊享养生）

START TRANSACTION;

SET @tid := 1;
SET @now := UTC_TIMESTAMP(6);

-- 1) 先删 service_items 中的 501-503（之前误归到了单品表）
DELETE FROM service_items
 WHERE TenantId=@tid AND Code IN ('501','502','503');

-- 2) 写入 service_packages 三行
INSERT INTO service_packages
  (TenantId, Code, Name, Price, MemberPrice, Description, IsActive, CreatedAt, UpdatedAt, IsDeleted)
VALUES
  (@tid, '501', '基础舒缓套餐',  118.00,  94.00,
     '肩颈40+头部30，合计70分钟；舒缓型组合', 1, @now, @now, 0),
  (@tid, '502', '深度理疗套餐',  158.00, 126.00,
     '颈椎调理50+艾灸40，合计90分钟；理疗型组合', 1, @now, @now, 0),
  (@tid, '503', '尊享养生套餐',  208.00, 166.00,
     '颈椎调理50+腰椎调理50+热敷20，合计120分钟；养生型组合', 1, @now, @now, 0);

-- 3) 写入 service_package_items（按 Code 解析 PackageId / ServiceId）
--    501: 102 肩颈 + 104 头部
INSERT INTO service_package_items
  (TenantId, PackageId, ServiceId, Quantity, CreatedAt, UpdatedAt, IsDeleted)
SELECT @tid,
       (SELECT Id FROM service_packages WHERE TenantId=@tid AND Code='501'),
       (SELECT Id FROM service_items    WHERE TenantId=@tid AND Code='102' AND IsDeleted=0),
       1, @now, @now, 0
UNION ALL
SELECT @tid,
       (SELECT Id FROM service_packages WHERE TenantId=@tid AND Code='501'),
       (SELECT Id FROM service_items    WHERE TenantId=@tid AND Code='104' AND IsDeleted=0),
       1, @now, @now, 0

-- 502: 301 颈椎 + 204 艾灸
UNION ALL
SELECT @tid,
       (SELECT Id FROM service_packages WHERE TenantId=@tid AND Code='502'),
       (SELECT Id FROM service_items    WHERE TenantId=@tid AND Code='301' AND IsDeleted=0),
       1, @now, @now, 0
UNION ALL
SELECT @tid,
       (SELECT Id FROM service_packages WHERE TenantId=@tid AND Code='502'),
       (SELECT Id FROM service_items    WHERE TenantId=@tid AND Code='204' AND IsDeleted=0),
       1, @now, @now, 0

-- 503: 301 颈椎 + 302 腰椎 + 205 热敷
UNION ALL
SELECT @tid,
       (SELECT Id FROM service_packages WHERE TenantId=@tid AND Code='503'),
       (SELECT Id FROM service_items    WHERE TenantId=@tid AND Code='301' AND IsDeleted=0),
       1, @now, @now, 0
UNION ALL
SELECT @tid,
       (SELECT Id FROM service_packages WHERE TenantId=@tid AND Code='503'),
       (SELECT Id FROM service_items    WHERE TenantId=@tid AND Code='302' AND IsDeleted=0),
       1, @now, @now, 0
UNION ALL
SELECT @tid,
       (SELECT Id FROM service_packages WHERE TenantId=@tid AND Code='503'),
       (SELECT Id FROM service_items    WHERE TenantId=@tid AND Code='205' AND IsDeleted=0),
       1, @now, @now, 0;

COMMIT;

-- 校验
SELECT '== service_packages ==' AS info;
SELECT TenantId, Code, Name, Price, MemberPrice
  FROM service_packages WHERE TenantId=1 AND IsDeleted=0 ORDER BY Code;

SELECT '== service_package_items ==' AS info;
SELECT p.Code AS PkgCode, p.Name AS Pkg, s.Code AS SvcCode, s.Name AS Svc, s.DurationMinutes, i.Quantity
  FROM service_package_items i
  JOIN service_packages p ON p.Id = i.PackageId
  JOIN service_items    s ON s.Id = i.ServiceId
 WHERE p.TenantId=1 AND i.IsDeleted=0
 ORDER BY p.Code, s.Code;

SELECT '== service_items 中 501-503 应已不存在 ==' AS info;
SELECT TenantId, Code, Name FROM service_items
 WHERE TenantId=1 AND Code IN ('501','502','503') AND IsDeleted=0;
