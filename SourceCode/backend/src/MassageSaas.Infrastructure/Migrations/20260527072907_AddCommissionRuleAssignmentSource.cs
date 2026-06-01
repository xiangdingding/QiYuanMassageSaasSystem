using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionRuleAssignmentSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL 在外键所依赖的索引被丢弃前需要先有备用索引覆盖该 FK 列。
            // 旧的复合索引 IX_..._TenantId_ServiceId_TechnicianId 是当前唯一覆盖 TenantId FK 的索引，
            // 直接 DropIndex 会触发 errno 1553。先建一个 TenantId 单列临时索引顶替，再做后续操作。
            migrationBuilder.Sql(
                "CREATE INDEX IX_commission_rules_TenantId_tmp ON commission_rules (TenantId);");

            migrationBuilder.DropIndex(
                name: "IX_commission_rules_TenantId_ServiceId_TechnicianId",
                table: "commission_rules");

            migrationBuilder.AddColumn<int>(
                name: "AssignmentSource",
                table: "commission_rules",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_TenantId_ServiceId_TechnicianId_AssignmentS~",
                table: "commission_rules",
                columns: new[] { "TenantId", "ServiceId", "TechnicianId", "AssignmentSource" });

            // 新复合索引同样以 TenantId 打头，可继续承载 FK；临时索引可以撤掉。
            migrationBuilder.Sql(
                "DROP INDEX IX_commission_rules_TenantId_tmp ON commission_rules;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "CREATE INDEX IX_commission_rules_TenantId_tmp ON commission_rules (TenantId);");

            migrationBuilder.DropIndex(
                name: "IX_commission_rules_TenantId_ServiceId_TechnicianId_AssignmentS~",
                table: "commission_rules");

            migrationBuilder.DropColumn(
                name: "AssignmentSource",
                table: "commission_rules");

            migrationBuilder.CreateIndex(
                name: "IX_commission_rules_TenantId_ServiceId_TechnicianId",
                table: "commission_rules",
                columns: new[] { "TenantId", "ServiceId", "TechnicianId" });

            migrationBuilder.Sql(
                "DROP INDEX IX_commission_rules_TenantId_tmp ON commission_rules;");
        }
    }
}
