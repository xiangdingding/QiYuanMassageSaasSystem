using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitStaffReferralValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StaffReferralValue",
                table: "tenants",
                newName: "StaffReferralFixedAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "StaffReferralPercent",
                table: "tenants",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // 原先固定/百分比共用一个值（rename 后落在 StaffReferralFixedAmount）。
            // 百分比模式(StaffReferralMode=2)的租户，把该值搬到 StaffReferralPercent，并清空固定金额。
            migrationBuilder.Sql(
                "UPDATE `tenants` SET `StaffReferralPercent` = `StaffReferralFixedAmount`, `StaffReferralFixedAmount` = 0 WHERE `StaffReferralMode` = 2;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StaffReferralPercent",
                table: "tenants");

            migrationBuilder.RenameColumn(
                name: "StaffReferralFixedAmount",
                table: "tenants",
                newName: "StaffReferralValue");
        }
    }
}
