using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerReferralMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerReferralMode",
                table: "tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 历史数据回填：原先百分比与固定可并行，迁移后二选一。
            // 已配置百分比的优先按百分比；否则有固定额的按固定；都没有则保持关闭(0)。
            migrationBuilder.Sql(
                "UPDATE `tenants` SET `CustomerReferralMode` = 1 WHERE `ReferralRewardPercent` > 0;");
            migrationBuilder.Sql(
                "UPDATE `tenants` SET `CustomerReferralMode` = 2 WHERE `CustomerReferralMode` = 0 AND `CustomerReferralFixedReward` > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerReferralMode",
                table: "tenants");
        }
    }
}
