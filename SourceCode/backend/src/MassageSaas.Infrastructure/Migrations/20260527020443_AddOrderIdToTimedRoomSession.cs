using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIdToTimedRoomSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OrderId",
                table: "timed_room_sessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_timed_room_sessions_OrderId",
                table: "timed_room_sessions",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_timed_room_sessions_orders_OrderId",
                table: "timed_room_sessions",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_timed_room_sessions_orders_OrderId",
                table: "timed_room_sessions");

            migrationBuilder.DropIndex(
                name: "IX_timed_room_sessions_OrderId",
                table: "timed_room_sessions");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "timed_room_sessions");
        }
    }
}
