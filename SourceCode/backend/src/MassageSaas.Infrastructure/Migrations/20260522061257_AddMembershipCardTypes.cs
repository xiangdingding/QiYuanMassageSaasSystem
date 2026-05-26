using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipCardTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CardTypeId",
                table: "members",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "membership_card_types",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sort = table.Column<int>(type: "int", nullable: false),
                    MinRechargeThreshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultDiscount = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    BonusPercent = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Remark = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_card_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_membership_card_types_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_members_CardTypeId",
                table: "members",
                column: "CardTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_membership_card_types_TenantId_Code",
                table: "membership_card_types",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_membership_card_types_TenantId_Sort",
                table: "membership_card_types",
                columns: new[] { "TenantId", "Sort" });

            migrationBuilder.AddForeignKey(
                name: "FK_members_membership_card_types_CardTypeId",
                table: "members",
                column: "CardTypeId",
                principalTable: "membership_card_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // ---- 跨租户 Seed ----
            // 给每个未软删的租户 seed 四档默认卡种，阈值与折扣复刻原 MembersController.ComputeLevel
            // 的硬编码（0/1000/5000/10000）；BonusPercent 给一个合理默认值 0/0/5%/10%，店主可在
            // shop-admin「会员卡种」页里随时改。
            //
            // 必须用裸 SQL：DbContext.Add() 会触发 ApplicationDbContext.SaveChangesAsync 的
            // tenant 注入逻辑，在迁移上下文里 TenantContext.TenantId=null 会让所有租户共享一份卡种。
            migrationBuilder.Sql(@"
INSERT INTO membership_card_types
  (TenantId, Code, Name, Sort, MinRechargeThreshold, DefaultDiscount, BonusPercent,
   IsActive, IsDefault, CreatedAt, UpdatedAt, IsDeleted)
SELECT t.Id,'REGULAR','普通卡',0,    0.00,1.0000,0.0000,1,1,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6),0
  FROM tenants t WHERE t.IsDeleted=0
UNION ALL
SELECT t.Id,'SILVER','银卡',  10, 1000.00,0.9500,0.0000,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6),0
  FROM tenants t WHERE t.IsDeleted=0
UNION ALL
SELECT t.Id,'GOLD','金卡',    20, 5000.00,0.9000,0.0500,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6),0
  FROM tenants t WHERE t.IsDeleted=0
UNION ALL
SELECT t.Id,'DIAMOND','钻石卡',30,10000.00,0.8500,0.1000,1,0,UTC_TIMESTAMP(6),UTC_TIMESTAMP(6),0
  FROM tenants t WHERE t.IsDeleted=0;
");

            // 把现有 Member.Level（int 0/10/20/30）映射到同租户内对应 Code 的卡种
            migrationBuilder.Sql(@"
UPDATE members m
JOIN membership_card_types c
  ON c.TenantId = m.TenantId
 AND c.Code = CASE m.Level
       WHEN 30 THEN 'DIAMOND'
       WHEN 20 THEN 'GOLD'
       WHEN 10 THEN 'SILVER'
       ELSE        'REGULAR'
     END
SET m.CardTypeId = c.Id
WHERE m.CardTypeId IS NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_members_membership_card_types_CardTypeId",
                table: "members");

            migrationBuilder.DropTable(
                name: "membership_card_types");

            migrationBuilder.DropIndex(
                name: "IX_members_CardTypeId",
                table: "members");

            migrationBuilder.DropColumn(
                name: "CardTypeId",
                table: "members");
        }
    }
}
