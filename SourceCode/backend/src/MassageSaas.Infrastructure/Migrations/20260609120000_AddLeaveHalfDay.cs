using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveHalfDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 请假起止半天（0=上午 Morning，1=下午 Afternoon）。
            // 存量请假视作整天：起=上午(0)，止=下午(1)。
            migrationBuilder.AddColumn<int>(
                name: "StartHalf",
                table: "leave_requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EndHalf",
                table: "leave_requests",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // 工资单请假天数改为支持半天的小数。
            migrationBuilder.AlterColumn<decimal>(
                name: "LeaveDays",
                table: "payroll_items",
                type: "decimal(8,1)",
                precision: 8,
                scale: 1,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StartHalf",
                table: "leave_requests");

            migrationBuilder.DropColumn(
                name: "EndHalf",
                table: "leave_requests");

            migrationBuilder.AlterColumn<int>(
                name: "LeaveDays",
                table: "payroll_items",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(8,1)",
                oldPrecision: 8,
                oldScale: 1);
        }
    }
}
