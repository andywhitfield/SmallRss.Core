using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallRss.Migrations
{
    /// <inheritdoc />
    public partial class RssFeedImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "RssFeeds",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "RssFeeds");
        }
    }
}
