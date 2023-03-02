using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace InvoiceApp.Migrations
{
    public partial class CreateCallOfftems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallOffItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallOffId = table.Column<int>(type: "int", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallOffItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallOffItems_CallOffs_CallOffId",
                        column: x => x.CallOffId,
                        principalTable: "CallOffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CallOffItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallOffItems_CallOffId",
                table: "CallOffItems",
                column: "CallOffId");

            migrationBuilder.CreateIndex(
                name: "IX_CallOffItems_ItemId",
                table: "CallOffItems",
                column: "ItemId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallOffItems");
        }
    }
}
