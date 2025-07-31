using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BrandHandlerWebApp.Migrations
{
    /// <inheritdoc />
    public partial class nextmig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotificationCount",
                table: "Meetings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationCount",
                table: "Meetings");
        }
    }
}
