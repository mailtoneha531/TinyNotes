using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryHashtags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hashtags",
                table: "Notes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "Notes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hashtags",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "Notes");
        }
    }
}
