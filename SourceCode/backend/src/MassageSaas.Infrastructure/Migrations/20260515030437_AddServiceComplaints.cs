using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceComplaints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ComplaintTransferred",
                table: "order_items",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "service_complaints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    StoreId = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    OriginalTechnicianId = table.Column<long>(type: "bigint", nullable: false),
                    MemberId = table.Column<long>(type: "bigint", nullable: true),
                    Tags = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Comment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Resolution = table.Column<int>(type: "int", nullable: true),
                    ReassignedToTechnicianId = table.Column<long>(type: "bigint", nullable: true),
                    ResolutionNote = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResolvedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ResolvedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    RecordedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_complaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_complaints_members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_service_complaints_order_items_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "order_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_complaints_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_complaints_stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_complaints_users_OriginalTechnicianId",
                        column: x => x.OriginalTechnicianId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_complaints_users_ReassignedToTechnicianId",
                        column: x => x.ReassignedToTechnicianId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_service_complaints_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_service_complaints_users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_MemberId",
                table: "service_complaints",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_OrderId",
                table: "service_complaints",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_OrderItemId",
                table: "service_complaints",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_OriginalTechnicianId",
                table: "service_complaints",
                column: "OriginalTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_ReassignedToTechnicianId",
                table: "service_complaints",
                column: "ReassignedToTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_RecordedByUserId",
                table: "service_complaints",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_ResolvedByUserId",
                table: "service_complaints",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_StoreId",
                table: "service_complaints",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_TenantId_OriginalTechnicianId_CreatedAt",
                table: "service_complaints",
                columns: new[] { "TenantId", "OriginalTechnicianId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_service_complaints_TenantId_StoreId_Status",
                table: "service_complaints",
                columns: new[] { "TenantId", "StoreId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_complaints");

            migrationBuilder.DropColumn(
                name: "ComplaintTransferred",
                table: "order_items");
        }
    }
}
