using Microsoft.EntityFrameworkCore.Migrations;

namespace InvoiceApp.Migrations
{
    public partial class RemoveUniqueItemNoInItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_ItemNo",
                table: "Items");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemNo",
                table: "Items",
                column: "ItemNo",
                unique: true);
        }
    }
}
