using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caniactivity.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class v002_add_appointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentDog",
                columns: table => new
                {
                    AppointmentsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DogsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentDog", x => new { x.AppointmentsId, x.DogsId });
                    table.ForeignKey(
                        name: "FK_AppointmentDog_Appointments_AppointmentsId",
                        column: x => x.AppointmentsId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentDog_Dog_DogsId",
                        column: x => x.DogsId,
                        principalTable: "Dog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentDog_DogsId",
                table: "AppointmentDog",
                column: "DogsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentDog");

            migrationBuilder.DropTable(
                name: "Appointments");
        }
    }
}
