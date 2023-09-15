using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caniactivity.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class v005_Restore_date_as_string_on_appointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndDate",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StartDate",
                table: "Appointments",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Appointments");
        }
    }
}
