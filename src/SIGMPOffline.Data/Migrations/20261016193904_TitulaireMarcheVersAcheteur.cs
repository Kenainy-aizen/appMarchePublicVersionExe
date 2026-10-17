using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGMPOffline.Data.Migrations
{
    /// <inheritdoc />
    public partial class TitulaireMarcheVersAcheteur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. NumStat doit d'abord devenir unique : c'est la clé parente de la
            //    nouvelle clé étrangère. SQLite exige que la clé référencée soit unique
            //    AVANT la reconstruction de la table qui la référence.
            migrationBuilder.DropIndex(
                name: "IX_AcheteurDAO_NumStat",
                table: "AcheteurDAO");

            migrationBuilder.CreateIndex(
                name: "AK_AcheteurDAO_NumStat",
                table: "AcheteurDAO",
                column: "NumStat",
                unique: true);

            // 2. Reconstruire Marche : suppression de l'ancienne FK/colonne, ajout de la nouvelle.
            migrationBuilder.DropForeignKey(
                name: "FK_Marche_Candidat_IdCandidatTitulaire",
                table: "Marche");

            migrationBuilder.DropIndex(
                name: "IX_Marche_IdCandidatTitulaire",
                table: "Marche");

            migrationBuilder.DropColumn(
                name: "IdCandidatTitulaire",
                table: "Marche");

            migrationBuilder.AddColumn<string>(
                name: "NumStatTitulaire",
                table: "Marche",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marche_NumStatTitulaire",
                table: "Marche",
                column: "NumStatTitulaire");

            migrationBuilder.AddForeignKey(
                name: "FK_Marche_AcheteurDAO_NumStatTitulaire",
                table: "Marche",
                column: "NumStatTitulaire",
                principalTable: "AcheteurDAO",
                principalColumn: "NumStat",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Marche_AcheteurDAO_NumStatTitulaire",
                table: "Marche");

            migrationBuilder.DropIndex(
                name: "IX_Marche_NumStatTitulaire",
                table: "Marche");

            migrationBuilder.DropColumn(
                name: "NumStatTitulaire",
                table: "Marche");

            migrationBuilder.AddColumn<int>(
                name: "IdCandidatTitulaire",
                table: "Marche",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marche_IdCandidatTitulaire",
                table: "Marche",
                column: "IdCandidatTitulaire");

            migrationBuilder.AddForeignKey(
                name: "FK_Marche_Candidat_IdCandidatTitulaire",
                table: "Marche",
                column: "IdCandidatTitulaire",
                principalTable: "Candidat",
                principalColumn: "IdCandidat",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropIndex(
                name: "AK_AcheteurDAO_NumStat",
                table: "AcheteurDAO");

            migrationBuilder.CreateIndex(
                name: "IX_AcheteurDAO_NumStat",
                table: "AcheteurDAO",
                column: "NumStat");
        }
    }
}
