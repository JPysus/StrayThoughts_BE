using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrayThoughts_BE.Migrations
{
    /// <inheritdoc />
    public partial class PostFields_Emoji_Theme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeEmoji",
                table: "Posts",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Theme",
                table: "Posts",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeEmoji",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Theme",
                table: "Posts");
        }
    }
}
