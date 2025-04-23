using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigColRename.Migrations
{
    /// <inheritdoc />
    public partial class ColRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Blogs",
                newName: "Name1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name1",
                table: "Blogs",
                newName: "Name");
        }
    }
}
