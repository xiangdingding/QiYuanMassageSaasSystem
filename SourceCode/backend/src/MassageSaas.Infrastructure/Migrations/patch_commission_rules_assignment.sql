-- 补完已部分应用的迁移
-- 现状：AssignmentSource 列已加；旧复合索引已删；IX_..._tmp 还在；新复合索引未建；migration history 未写

CREATE INDEX `IX_commission_rules_TenantId_ServiceId_TechnicianId_AssignmentS~`
  ON commission_rules (TenantId, ServiceId, TechnicianId, AssignmentSource);

DROP INDEX IX_commission_rules_TenantId_tmp ON commission_rules;

INSERT INTO __efmigrationshistory (MigrationId, ProductVersion)
VALUES ('20260527072907_AddCommissionRuleAssignmentSource', '8.0.0');

SELECT 'patched' AS info;
SHOW INDEX FROM commission_rules;
SELECT MigrationId FROM __efmigrationshistory ORDER BY MigrationId DESC LIMIT 3;
