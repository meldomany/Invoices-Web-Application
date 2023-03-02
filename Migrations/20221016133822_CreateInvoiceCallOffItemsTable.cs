using Microsoft.EntityFrameworkCore.Migrations;

namespace InvoiceApp.Migrations
{
    public partial class CreateInvoiceCallOffItemsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceCallOffItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InvoiceId = table.Column<int>(type: "int", nullable: false),
                    CallOffItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceCallOffItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceCallOffItems_CallOffItems_CallOffItemId",
                        column: x => x.CallOffItemId,
                        principalTable: "CallOffItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvoiceCallOffItems_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceCallOffItems_CallOffItemId",
                table: "InvoiceCallOffItems",
                column: "CallOffItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceCallOffItems_InvoiceId",
                table: "InvoiceCallOffItems",
                column: "InvoiceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceCallOffItems");
        }
    }
}
