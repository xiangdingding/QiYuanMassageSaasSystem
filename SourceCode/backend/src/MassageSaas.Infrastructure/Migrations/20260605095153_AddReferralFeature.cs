using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CustomerReferralFixedReward",
                table: "tenants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "StaffReferralMode",
                table: "tenants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "StaffReferralValue",
                table: "tenants",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferralCommissionTotal",
                table: "payroll_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "ReferredByStaffId",
                table: "members",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "staff_referral_records",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    StaffUserId = table.Column<long>(type: "bigint", nullable: false),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EarnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Remark = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_referral_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_referral_records_members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_staff_referral_records_users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_members_ReferredByStaffId",
                table: "members",
                column: "ReferredByStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_referral_records_MemberId",
                table: "staff_referral_records",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_referral_records_StaffUserId_EarnedAt",
                table: "staff_referral_records",
                columns: new[] { "StaffUserId", "EarnedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_referral_records_StoreId",
                table: "staff_referral_records",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_members_users_ReferredByStaffId",
                table: "members",
                column: "ReferredByStaffId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_members_users_ReferredByStaffId",
                table: "members");

            migrationBuilder.DropTable(
                name: "staff_referral_records");

            migrationBuilder.DropIndex(
                name: "IX_members_ReferredByStaffId",
                table: "members");

            migrationBuilder.DropColumn(
                name: "CustomerReferralFixedReward",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "StaffReferralMode",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "StaffReferralValue",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "ReferralCommissionTotal",
                table: "payroll_items");

            migrationBuilder.DropColumn(
                name: "ReferredByStaffId",
                table: "members");
        }
    }
}
