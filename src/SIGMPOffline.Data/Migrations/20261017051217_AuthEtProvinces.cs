using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGMPOffline.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuthEtProvinces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdProvince",
                table: "PPMEntete",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdProvince",
                table: "Marche",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdProvince",
                table: "DossierDAO",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfigurationApp",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomProprietairePRMP = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DateInstallation = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationApp", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Province",
                columns: table => new
                {
                    IdProvince = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomProvince = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Salt = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Province", x => x.IdProvince);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PPMEntete_IdProvince",
                table: "PPMEntete",
                column: "IdProvince");

            migrationBuilder.CreateIndex(
                name: "IX_Marche_IdProvince",
                table: "Marche",
                column: "IdProvince");

            migrationBuilder.CreateIndex(
                name: "IX_DossierDAO_IdProvince",
                table: "DossierDAO",
                column: "IdProvince");

            migrationBuilder.CreateIndex(
                name: "IX_Province_NomProvince",
                table: "Province",
                column: "NomProvince",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DossierDAO_Province_IdProvince",
                table: "DossierDAO",
                column: "IdProvince",
                principalTable: "Province",
                principalColumn: "IdProvince",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Marche_Province_IdProvince",
                table: "Marche",
                column: "IdProvince",
                principalTable: "Province",
                principalColumn: "IdProvince",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PPMEntete_Province_IdProvince",
                table: "PPMEntete",
                column: "IdProvince",
                principalTable: "Province",
                principalColumn: "IdProvince",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DossierDAO_Province_IdProvince",
                table: "DossierDAO");

            migrationBuilder.DropForeignKey(
                name: "FK_Marche_Province_IdProvince",
                table: "Marche");

            migrationBuilder.DropForeignKey(
                name: "FK_PPMEntete_Province_IdProvince",
                table: "PPMEntete");

            migrationBuilder.DropTable(
                name: "ConfigurationApp");

            migrationBuilder.DropTable(
                name: "Province");

            migrationBuilder.DropIndex(
                name: "IX_PPMEntete_IdProvince",
                table: "PPMEntete");

            migrationBuilder.DropIndex(
                name: "IX_Marche_IdProvince",
                table: "Marche");

            migrationBuilder.DropIndex(
                name: "IX_DossierDAO_IdProvince",
                table: "DossierDAO");

            migrationBuilder.DropColumn(
                name: "IdProvince",
                table: "PPMEntete");

            migrationBuilder.DropColumn(
                name: "IdProvince",
                table: "Marche");

            migrationBuilder.DropColumn(
                name: "IdProvince",
                table: "DossierDAO");
        }
    }
}
