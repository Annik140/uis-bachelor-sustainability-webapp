using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace uis_bachelor_sustainability_webapp.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandCriteriaRubric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsSummary",
                table: "ClothingBrands",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProsSummary",
                table: "ClothingBrands",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BrandCriterionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClothingBrandId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NumericValue = table.Column<decimal>(type: "numeric", nullable: true),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GoodThreshold = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    WarningThreshold = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    LowerIsBetter = table.Column<bool>(type: "boolean", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 1m),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandCriterionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandCriterionItems_ClothingBrands_ClothingBrandId",
                        column: x => x.ClothingBrandId,
                        principalTable: "ClothingBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandCriterionItems_ClothingBrandId",
                table: "BrandCriterionItems",
                column: "ClothingBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_BrandCriterionItems_ClothingBrandId_Category",
                table: "BrandCriterionItems",
                columns: new[] { "ClothingBrandId", "Category" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandCriterionItems");

            migrationBuilder.DropColumn(
                name: "ConsSummary",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "ProsSummary",
                table: "ClothingBrands");
        }
    }
}
