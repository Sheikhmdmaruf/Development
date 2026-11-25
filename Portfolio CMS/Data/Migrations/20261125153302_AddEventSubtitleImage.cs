using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioCms.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSubtitleImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "CalendarEvents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subtitle",
                table: "CalendarEvents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "Subtitle",
                table: "CalendarEvents");
        }
    }
}
