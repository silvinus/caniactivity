using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caniactivity.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class v006_add_outbox_for_mail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RegisteredById",
                table: "Appointments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Outbox",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    To = table.Column<string>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outbox", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RegisteredById",
                table: "Appointments",
                column: "RegisteredById");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AspNetUsers_RegisteredById",
                table: "Appointments",
                column: "RegisteredById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AspNetUsers_RegisteredById",
                table: "Appointments");

            migrationBuilder.DropTable(
                name: "Outbox");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_RegisteredById",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RegisteredById",
                table: "Appointments");
        }
    }
}
