using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SIGMPOffline.Data;

#nullable disable

namespace SIGMPOffline.Data.Migrations
{
    /// <summary>
    /// Complète les lignes du PPM : comptes, dates prévisionnelles, responsable de la procédure
    /// et table des services bénéficiaires.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261121120000_PpmDetailComplet")]
    public partial class PpmDetailComplet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comptes",
                table: "PPMDetail",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateAttributionPrevue",
                table: "PPMDetail",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateLancementPrevue",
                table: "PPMDetail",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOuverturePlisPrevue",
                table: "PPMDetail",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsableProcedure",
                table: "PPMDetail",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PPMBeneficiaire",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NumPPMDetail = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Service = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Montant = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPMBeneficiaire", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PPMBeneficiaire_PPMDetail_NumPPMDetail",
                        column: x => x.NumPPMDetail,
                        principalTable: "PPMDetail",
                        principalColumn: "NumPPMDetail",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PPMBeneficiaire_NumPPMDetail",
                table: "PPMBeneficiaire",
                column: "NumPPMDetail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PPMBeneficiaire");

            migrationBuilder.DropColumn(
                name: "Comptes",
                table: "PPMDetail");

            migrationBuilder.DropColumn(
                name: "DateAttributionPrevue",
                table: "PPMDetail");

            migrationBuilder.DropColumn(
                name: "DateLancementPrevue",
                table: "PPMDetail");

            migrationBuilder.DropColumn(
                name: "DateOuverturePlisPrevue",
                table: "PPMDetail");

            migrationBuilder.DropColumn(
                name: "ResponsableProcedure",
                table: "PPMDetail");
        }
    }
}
