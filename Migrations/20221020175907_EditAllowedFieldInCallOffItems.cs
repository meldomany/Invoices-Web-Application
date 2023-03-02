﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace InvoiceApp.Migrations
{
    public partial class EditAllowedFieldInCallOffItems : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Allowed",
                table: "CallOffItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "Allowed",
                table: "CallOffItems",
                type: "bit",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
