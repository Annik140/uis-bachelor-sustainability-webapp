using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uis_bachelor_sustainability_webapp.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ClothingBrands",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ClothingBrands");
        }
    }
}
