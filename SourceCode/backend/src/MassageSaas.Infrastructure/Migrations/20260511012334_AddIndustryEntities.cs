using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndustryEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlindCertNo",
                table: "users",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "MaxRoundsPerDay",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Specialties",
                table: "users",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TechnicianLevel",
                table: "users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceJunior",
                table: "service_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceMaster",
                table: "service_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReopenReason",
                table: "orders",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReopenedAt",
                table: "orders",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReopenedByUserId",
                table: "orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipAmount",
                table: "orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "VoucherId",
                table: "orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAddOn",
                table: "order_items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "MemberPackageId",
                table: "order_items",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TipAmount",
                table: "order_items",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HealthNotes",
                table: "members",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "members",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreferenceNotes",
                table: "members",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Unit = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    MinQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_items_stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApproverUserId = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_leave_requests_users_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_leave_requests_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "member_packages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    MemberId = table.Column<long>(type: "bigint", nullable: false),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCount = table.Column<int>(type: "int", nullable: false),
                    RemainCount = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_member_packages_members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_member_packages_service_items_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "service_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_member_packages_stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_packages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MemberPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_packages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_reviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    TechnicianId = table.Column<long>(type: "bigint", nullable: false),
                    MemberId = table.Column<long>(type: "bigint", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Tags = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Comment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_reviews_members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_service_reviews_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_reviews_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_reviews_users_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "staff_schedules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time(6)", nullable: false),
                    Remark = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_schedules_stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_schedules_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "vouchers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FaceValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RedeemedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RedeemedOrderId = table.Column<long>(type: "bigint", nullable: true),
                    Platform = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remark = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vouchers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Delta = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    OperatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    Remark = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_inventory_movements_inventory_items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "inventory_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_inventory_movements_users_OperatorUserId",
                        column: x => x.OperatorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_package_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    PackageId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_package_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_package_items_service_items_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "service_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_package_items_service_packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "service_packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_orders_VoucherId",
                table: "orders",
                column: "VoucherId");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_MemberPackageId",
                table: "order_items",
                column: "MemberPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_items_StoreId_Code",
                table: "inventory_items",
                columns: new[] { "StoreId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_ItemId_CreatedAt",
                table: "inventory_movements",
                columns: new[] { "ItemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_inventory_movements_OperatorUserId",
                table: "inventory_movements",
                column: "OperatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_ApproverUserId",
                table: "leave_requests",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_UserId_FromDate",
                table: "leave_requests",
                columns: new[] { "UserId", "FromDate" });

            migrationBuilder.CreateIndex(
                name: "IX_member_packages_MemberId",
                table: "member_packages",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_member_packages_ServiceId",
                table: "member_packages",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_member_packages_StoreId",
                table: "member_packages",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_member_packages_TenantId_MemberId_Status",
                table: "member_packages",
                columns: new[] { "TenantId", "MemberId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_service_package_items_PackageId",
                table: "service_package_items",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_service_package_items_ServiceId",
                table: "service_package_items",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_service_packages_TenantId_Code",
                table: "service_packages",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_MemberId",
                table: "service_reviews",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_OrderId",
                table: "service_reviews",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_OrderItemId",
                table: "service_reviews",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_TechnicianId",
                table: "service_reviews",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_service_reviews_TenantId_TechnicianId_CreatedAt",
                table: "service_reviews",
                columns: new[] { "TenantId", "TechnicianId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_schedules_StoreId_WorkDate_UserId",
                table: "staff_schedules",
                columns: new[] { "StoreId", "WorkDate", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_staff_schedules_UserId",
                table: "staff_schedules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_Status",
                table: "vouchers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_TenantId_Code",
                table: "vouchers",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_member_packages_MemberPackageId",
                table: "order_items",
                column: "MemberPackageId",
                principalTable: "member_packages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_orders_vouchers_VoucherId",
                table: "orders",
                column: "VoucherId",
                principalTable: "vouchers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_member_packages_MemberPackageId",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_orders_vouchers_VoucherId",
                table: "orders");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "leave_requests");

            migrationBuilder.DropTable(
                name: "member_packages");

            migrationBuilder.DropTable(
                name: "service_package_items");

            migrationBuilder.DropTable(
                name: "service_reviews");

            migrationBuilder.DropTable(
                name: "staff_schedules");

            migrationBuilder.DropTable(
                name: "vouchers");

            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "service_packages");

            migrationBuilder.DropIndex(
                name: "IX_orders_VoucherId",
                table: "orders");

            migrationBuilder.DropIndex(
                name: "IX_order_items_MemberPackageId",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "BlindCertNo",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MaxRoundsPerDay",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Specialties",
                table: "users");

            migrationBuilder.DropColumn(
                name: "TechnicianLevel",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PriceJunior",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "PriceMaster",
                table: "service_items");

            migrationBuilder.DropColumn(
                name: "ReopenReason",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ReopenedAt",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "ReopenedByUserId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "TipAmount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "VoucherId",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "IsAddOn",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "MemberPackageId",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "TipAmount",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "HealthNotes",
                table: "members");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "members");

            migrationBuilder.DropColumn(
                name: "PreferenceNotes",
                table: "members");
        }
    }
}
