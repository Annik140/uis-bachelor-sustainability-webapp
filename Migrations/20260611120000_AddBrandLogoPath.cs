using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uis_bachelor_sustainability_webapp.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandLogoPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoPath",
                table: "ClothingBrands",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoPath",
                table: "ClothingBrands");
        }
    }
}
