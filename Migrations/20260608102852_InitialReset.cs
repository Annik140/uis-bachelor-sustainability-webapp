using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace uis_bachelor_sustainability_webapp.Migrations
{
    /// <inheritdoc />
    public partial class InitialReset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClothingBrands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BrandName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ProsSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ConsSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MaterialSustainabilityScore = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    LaborPracticesScore = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    CarbonFootprintScore = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    ProductLongevityScore = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    EvidenceSourceCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SustainabilityScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    TransparencyScore = table.Column<decimal>(type: "numeric(4,1)", precision: 4, scale: 1, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClothingBrands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrandCertifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClothingBrandId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrandCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrandCertifications_ClothingBrands_ClothingBrandId",
                        column: x => x.ClothingBrandId,
                        principalTable: "ClothingBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_BrandCertifications_ClothingBrandId",
                table: "BrandCertifications",
                column: "ClothingBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_BrandCertifications_ClothingBrandId_Name",
                table: "BrandCertifications",
                columns: new[] { "ClothingBrandId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BrandCriterionItems_ClothingBrandId",
                table: "BrandCriterionItems",
                column: "ClothingBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_BrandCriterionItems_ClothingBrandId_Category",
                table: "BrandCriterionItems",
                columns: new[] { "ClothingBrandId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_BrandEvidenceSources_ClothingBrandId",
                table: "BrandEvidenceSources",
                column: "ClothingBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_ClothingBrands_BrandName",
                table: "ClothingBrands",
                column: "BrandName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrandCertifications");

            migrationBuilder.DropTable(
                name: "BrandCriterionItems");

            migrationBuilder.DropTable(
                name: "BrandEvidenceSources");

            migrationBuilder.DropTable(
                name: "ClothingBrands");
        }
    }
}
