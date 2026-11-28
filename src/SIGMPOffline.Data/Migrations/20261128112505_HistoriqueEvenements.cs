using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGMPOffline.Data.Migrations
{
    /// <inheritdoc />
    public partial class HistoriqueEvenements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistoriqueEvenement",
                columns: table => new
                {
                    IdHistorique = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateEvenement = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IdUtilisateur = table.Column<int>(type: "INTEGER", nullable: true),
                    NomUtilisateur = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IdProvince = table.Column<int>(type: "INTEGER", nullable: true),
                    NomProvince = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    Action = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    TypeEntite = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    ReferenceEntite = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriqueEvenement", x => x.IdHistorique);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueEvenement_DateEvenement",
                table: "HistoriqueEvenement",
                column: "DateEvenement");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueEvenement_IdProvince",
                table: "HistoriqueEvenement",
                column: "IdProvince");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriqueEvenement_IdUtilisateur",
                table: "HistoriqueEvenement",
                column: "IdUtilisateur");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoriqueEvenement");
        }
    }
}
