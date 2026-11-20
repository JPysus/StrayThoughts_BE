using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrayThoughts_BE.Migrations
{
    /// <inheritdoc />
    public partial class Posts_and_Themes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Themes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LightBg = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false),
                    LightBorder = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false),
                    LightText = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false),
                    DarkBg = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false),
                    DarkBorder = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false),
                    DarkText = table.Column<string>(type: "varchar(7)", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                    table.CheckConstraint("CK_Themes_DisplayName_Lowercase", "[DisplayName] = LOWER([DisplayName])");
                    table.CheckConstraint("CK_Themes_Key_Lowercase", "[Key] = LOWER([Key])");
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", maxLength: 30, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", maxLength: 1000, nullable: false),
                    BadgeEmoji = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ThemeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdminHideDatetime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ThemeId",
                table: "Posts",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Themes_DisplayName",
                table: "Themes",
                column: "DisplayName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Themes_Key",
                table: "Themes",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Themes");
        }
    }
}
