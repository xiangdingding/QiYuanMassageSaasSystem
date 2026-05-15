using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReferralRewardPercent",
                table: "tenants",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "CloseReason",
                table: "members",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedAt",
                table: "members",
                type: "datetime(6)",
                nullable: true);

            // 默认 true：现存会员（退卡功能上线前）一律视为启用，避免一刀切关闭。
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "members",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferralRewardEarned",
                table: "members",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "ReferredByMemberId",
                table: "members",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CounterpartyMemberId",
                table: "member_recharge_records",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "member_recharge_records",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_members_ReferredByMemberId",
                table: "members",
                column: "ReferredByMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_member_recharge_records_CounterpartyMemberId",
                table: "member_recharge_records",
                column: "CounterpartyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_member_recharge_records_Kind",
                table: "member_recharge_records",
                column: "Kind");

            migrationBuilder.AddForeignKey(
                name: "FK_member_recharge_records_members_CounterpartyMemberId",
                table: "member_recharge_records",
                column: "CounterpartyMemberId",
                principalTable: "members",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_members_members_ReferredByMemberId",
                table: "members",
                column: "ReferredByMemberId",
                principalTable: "members",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_member_recharge_records_members_CounterpartyMemberId",
                table: "member_recharge_records");

            migrationBuilder.DropForeignKey(
                name: "FK_members_members_ReferredByMemberId",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_ReferredByMemberId",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_member_recharge_records_CounterpartyMemberId",
                table: "member_recharge_records");

            migrationBuilder.DropIndex(
                name: "IX_member_recharge_records_Kind",
                table: "member_recharge_records");

            migrationBuilder.DropColumn(
                name: "ReferralRewardPercent",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "CloseReason",
                table: "members");

            migrationBuilder.DropColumn(
                name: "ClosedAt",
                table: "members");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "members");

            migrationBuilder.DropColumn(
                name: "ReferralRewardEarned",
                table: "members");

            migrationBuilder.DropColumn(
                name: "ReferredByMemberId",
                table: "members");

            migrationBuilder.DropColumn(
                name: "CounterpartyMemberId",
                table: "member_recharge_records");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "member_recharge_records");
        }
    }
}
