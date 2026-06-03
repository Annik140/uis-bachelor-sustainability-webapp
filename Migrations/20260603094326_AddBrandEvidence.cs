using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace uis_bachelor_sustainability_webapp.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EvidenceSummary",
                table: "ClothingBrands",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PrimarySourcePublishedAtUtc",
                table: "ClothingBrands",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimarySourceTitle",
                table: "ClothingBrands",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimarySourceUrl",
                table: "ClothingBrands",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BrandEvidenceSources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClothingBrandId = table.Column<int>(type: "integer", nullable: false),
                    SourceTitle = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandEvidenceSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandEvidenceSources_ClothingBrands_ClothingBrandId",
                        column: x => x.ClothingBrandId,
                        principalTable: "ClothingBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrandEvidenceSources_ClothingBrandId",
                table: "BrandEvidenceSources",
                column: "ClothingBrandId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandEvidenceSources");

            migrationBuilder.DropColumn(
                name: "EvidenceSummary",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "PrimarySourcePublishedAtUtc",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "PrimarySourceTitle",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "PrimarySourceUrl",
                table: "ClothingBrands");
        }
    }
}
