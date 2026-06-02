using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uis_bachelor_sustainability_webapp.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CarbonFootprintScore",
                table: "ClothingBrands",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EvidenceSourceCount",
                table: "ClothingBrands",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "LaborPracticesScore",
                table: "ClothingBrands",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaterialSustainabilityScore",
                table: "ClothingBrands",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProductLongevityScore",
                table: "ClothingBrands",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TransparencyScore",
                table: "ClothingBrands",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "ClothingBrands",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarbonFootprintScore",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "EvidenceSourceCount",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "LaborPracticesScore",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "MaterialSustainabilityScore",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "ProductLongevityScore",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "TransparencyScore",
                table: "ClothingBrands");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "ClothingBrands");
        }
    }
}
