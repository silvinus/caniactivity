using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Caniactivity.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class v003_improve_statuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Dog",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Dog");
        }
    }
}
