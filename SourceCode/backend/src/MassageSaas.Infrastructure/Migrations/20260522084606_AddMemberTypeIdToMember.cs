using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MassageSaas.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberTypeIdToMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MemberTypeId",
                table: "members",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_members_MemberTypeId",
                table: "members",
                column: "MemberTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_members_member_types_MemberTypeId",
                table: "members",
                column: "MemberTypeId",
                principalTable: "member_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_members_member_types_MemberTypeId",
                table: "members");

            migrationBuilder.DropIndex(
                name: "IX_members_MemberTypeId",
                table: "members");

            migrationBuilder.DropColumn(
                name: "MemberTypeId",
                table: "members");
        }
    }
}
