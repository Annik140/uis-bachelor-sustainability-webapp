using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uis_bachelor_sustainability_webapp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyRubricFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropColumn(
                name: "GoodThreshold",
                table: "BrandCriterionItems");

            migrationBuilder.DropColumn(
                name: "LowerIsBetter",
                table: "BrandCriterionItems");

            migrationBuilder.DropColumn(
                name: "WarningThreshold",
                table: "BrandCriterionItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<decimal>(
                name: "GoodThreshold",
                table: "BrandCriterionItems",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LowerIsBetter",
                table: "BrandCriterionItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "WarningThreshold",
                table: "BrandCriterionItems",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);
        }
    }
}
