using Microsoft.EntityFrameworkCore.Migrations;

namespace InvoiceApp.Migrations
{
    public partial class RemoveInvoiceIdFromCallOff : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallOffs_Invoices_InvoiceId",
                table: "CallOffs");

            migrationBuilder.DropIndex(
                name: "IX_CallOffs_InvoiceId",
                table: "CallOffs");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "CallOffs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InvoiceId",
                table: "CallOffs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CallOffs_InvoiceId",
                table: "CallOffs",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CallOffs_Invoices_InvoiceId",
                table: "CallOffs",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
