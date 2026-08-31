using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmallRss.Migrations
{
    /// <inheritdoc />
    public partial class RssFeed_ImageUrlUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ImageUrlUpdated",
                table: "RssFeeds",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrlUpdated",
                table: "RssFeeds");
        }
    }
}
