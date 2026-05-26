using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemodelMemberTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "member_types",
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
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ServiceItemId = table.Column<long>(type: "bigint", nullable: true),
                    MinRechargeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinPurchaseCount = table.Column<int>(type: "int", nullable: true),
                    Discount = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    BonusAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BonusCount = table.Column<int>(type: "int", nullable: true),
                    ValidDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Remark = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_member_types_service_items_ServiceItemId",
                        column: x => x.ServiceItemId,
                        principalTable: "service_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_member_types_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_member_types_ServiceItemId",
                table: "member_types",
                column: "ServiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_member_types_TenantId_Code",
                table: "member_types",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_types_TenantId_Sort",
                table: "member_types",
                columns: new[] { "TenantId", "Sort" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "member_types");

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
                    BonusPercent = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DefaultDiscount = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    MinRechargeThreshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sort = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
        }
    }
}
