using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staff_transfers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<long>(type: "bigint", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    FromStoreId = table.Column<long>(type: "bigint", nullable: false),
                    ToStoreId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpectedReturnAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_staff_transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_staff_transfers_stores_FromStoreId",
                        column: x => x.FromStoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_transfers_stores_ToStoreId",
                        column: x => x.ToStoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_staff_transfers_users_OperatorUserId",
                        column: x => x.OperatorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_staff_transfers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_staff_transfers_FromStoreId",
                table: "staff_transfers",
                column: "FromStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_transfers_OperatorUserId",
                table: "staff_transfers",
                column: "OperatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_transfers_TenantId_ToStoreId",
                table: "staff_transfers",
                columns: new[] { "TenantId", "ToStoreId" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_transfers_TenantId_UserId_Status",
                table: "staff_transfers",
                columns: new[] { "TenantId", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_staff_transfers_ToStoreId",
                table: "staff_transfers",
                column: "ToStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_staff_transfers_UserId",
                table: "staff_transfers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_transfers");
        }
    }
}
