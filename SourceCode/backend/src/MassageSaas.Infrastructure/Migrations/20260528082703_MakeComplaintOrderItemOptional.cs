using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeComplaintOrderItemOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_service_complaints_order_items_OrderItemId",
                table: "service_complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_service_complaints_orders_OrderId",
                table: "service_complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_service_complaints_users_OriginalTechnicianId",
                table: "service_complaints");

            migrationBuilder.AlterColumn<long>(
                name: "OriginalTechnicianId",
                table: "service_complaints",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "OrderItemId",
                table: "service_complaints",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "OrderId",
                table: "service_complaints",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_service_complaints_order_items_OrderItemId",
                table: "service_complaints",
                column: "OrderItemId",
                principalTable: "order_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_service_complaints_orders_OrderId",
                table: "service_complaints",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_service_complaints_users_OriginalTechnicianId",
                table: "service_complaints",
                column: "OriginalTechnicianId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_service_complaints_order_items_OrderItemId",
                table: "service_complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_service_complaints_orders_OrderId",
                table: "service_complaints");

            migrationBuilder.DropForeignKey(
                name: "FK_service_complaints_users_OriginalTechnicianId",
                table: "service_complaints");

            migrationBuilder.AlterColumn<long>(
                name: "OriginalTechnicianId",
                table: "service_complaints",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "OrderItemId",
                table: "service_complaints",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "OrderId",
                table: "service_complaints",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_service_complaints_order_items_OrderItemId",
                table: "service_complaints",
                column: "OrderItemId",
                principalTable: "order_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_service_complaints_orders_OrderId",
                table: "service_complaints",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_service_complaints_users_OriginalTechnicianId",
                table: "service_complaints",
                column: "OriginalTechnicianId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
