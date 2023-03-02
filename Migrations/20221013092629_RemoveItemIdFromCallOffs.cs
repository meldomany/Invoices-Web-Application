using Microsoft.EntityFrameworkCore.Migrations;

namespace InvoiceApp.Migrations
{
    public partial class RemoveItemIdFromCallOffs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallOffs_Items_ItemId",
                table: "CallOffs");

            migrationBuilder.DropIndex(
                name: "IX_CallOffs_ItemId",
                table: "CallOffs");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "CallOffs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "CallOffs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CallOffs_ItemId",
                table: "CallOffs",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_CallOffs_Items_ItemId",
                table: "CallOffs",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
