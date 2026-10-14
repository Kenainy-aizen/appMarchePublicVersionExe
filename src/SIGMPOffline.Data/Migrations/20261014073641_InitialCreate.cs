using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGMPOffline.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PPMEntete",
                columns: table => new
                {
                    NumPPM = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AnneeExercice = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoriteContractante = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DateElaboration = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    DateDerniereMAJ = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NumDerniereMAJ = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPMEntete", x => x.NumPPM);
                });

            migrationBuilder.CreateTable(
                name: "PPMDetail",
                columns: table => new
                {
                    NumPPMDetail = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumPPMEntete = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NatureMarche = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ObjetProjet = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MontantEstimatif = table.Column<long>(type: "INTEGER", nullable: false),
                    ModePassation = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    SourceFinancement = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PPMDetail", x => x.NumPPMDetail);
                    table.ForeignKey(
                        name: "FK_PPMDetail_PPMEntete_NumPPMEntete",
                        column: x => x.NumPPMEntete,
                        principalTable: "PPMEntete",
                        principalColumn: "NumPPM",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DossierDAO",
                columns: table => new
                {
                    RefDossier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumPPMDetail = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ObjetDossier = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CoutCahierCharges = table.Column<long>(type: "INTEGER", nullable: false),
                    MontantCaution = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DossierDAO", x => x.RefDossier);
                    table.ForeignKey(
                        name: "FK_DossierDAO_PPMDetail_NumPPMDetail",
                        column: x => x.NumPPMDetail,
                        principalTable: "PPMDetail",
                        principalColumn: "NumPPMDetail",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AcheteurDAO",
                columns: table => new
                {
                    IdAcheteur = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefDossier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumStat = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumNIF = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RaisonSociale = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Adresse = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    DateAchat = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcheteurDAO", x => x.IdAcheteur);
                    table.ForeignKey(
                        name: "FK_AcheteurDAO_DossierDAO_RefDossier",
                        column: x => x.RefDossier,
                        principalTable: "DossierDAO",
                        principalColumn: "RefDossier",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Candidat",
                columns: table => new
                {
                    IdCandidat = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefDossier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumStat = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumNIF = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NomEntreprise = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MontantOffre = table.Column<long>(type: "INTEGER", nullable: false),
                    EstRetenu = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidat", x => x.IdCandidat);
                    table.ForeignKey(
                        name: "FK_Candidat_DossierDAO_RefDossier",
                        column: x => x.RefDossier,
                        principalTable: "DossierDAO",
                        principalColumn: "RefDossier",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Marche",
                columns: table => new
                {
                    RefMarche = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RefDossier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IdCandidatTitulaire = table.Column<int>(type: "INTEGER", nullable: true),
                    ObjetMarche = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MontantHT = table.Column<long>(type: "INTEGER", nullable: false),
                    MontantTTC = table.Column<long>(type: "INTEGER", nullable: false),
                    DelaiExecutionJours = table.Column<int>(type: "INTEGER", nullable: false),
                    StatutExecution = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marche", x => x.RefMarche);
                    table.ForeignKey(
                        name: "FK_Marche_Candidat_IdCandidatTitulaire",
                        column: x => x.IdCandidatTitulaire,
                        principalTable: "Candidat",
                        principalColumn: "IdCandidat",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Marche_DossierDAO_RefDossier",
                        column: x => x.RefDossier,
                        principalTable: "DossierDAO",
                        principalColumn: "RefDossier",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Avenant",
                columns: table => new
                {
                    IdAvenant = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefMarche = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NumAvenant = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Motif = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    VariationMontant = table.Column<long>(type: "INTEGER", nullable: false),
                    ProlongationDelaiJours = table.Column<int>(type: "INTEGER", nullable: false),
                    DateAvenant = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Avenant", x => x.IdAvenant);
                    table.ForeignKey(
                        name: "FK_Avenant_Marche_RefMarche",
                        column: x => x.RefMarche,
                        principalTable: "Marche",
                        principalColumn: "RefMarche",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ElementMarche",
                columns: table => new
                {
                    CodeElement = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RefMarche = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Libelle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    PrixUnitaire = table.Column<long>(type: "INTEGER", nullable: false),
                    Quantite = table.Column<long>(type: "INTEGER", nullable: false),
                    Unite = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElementMarche", x => x.CodeElement);
                    table.ForeignKey(
                        name: "FK_ElementMarche_Marche_RefMarche",
                        column: x => x.RefMarche,
                        principalTable: "Marche",
                        principalColumn: "RefMarche",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuiviExecution",
                columns: table => new
                {
                    IdSuivi = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefMarche = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RefTEF = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    DateOS = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DateReceptionProvisoire = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    DateReceptionDefinitive = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EstCloture = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCloture = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuiviExecution", x => x.IdSuivi);
                    table.ForeignKey(
                        name: "FK_SuiviExecution_Marche_RefMarche",
                        column: x => x.RefMarche,
                        principalTable: "Marche",
                        principalColumn: "RefMarche",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcheteurDAO_NumStat",
                table: "AcheteurDAO",
                column: "NumStat");

            migrationBuilder.CreateIndex(
                name: "IX_AcheteurDAO_RefDossier",
                table: "AcheteurDAO",
                column: "RefDossier");

            migrationBuilder.CreateIndex(
                name: "IX_Avenant_RefMarche",
                table: "Avenant",
                column: "RefMarche");

            migrationBuilder.CreateIndex(
                name: "IX_Candidat_EstRetenu",
                table: "Candidat",
                column: "EstRetenu");

            migrationBuilder.CreateIndex(
                name: "IX_Candidat_RefDossier",
                table: "Candidat",
                column: "RefDossier");

            migrationBuilder.CreateIndex(
                name: "IX_DossierDAO_NumPPMDetail",
                table: "DossierDAO",
                column: "NumPPMDetail");

            migrationBuilder.CreateIndex(
                name: "IX_ElementMarche_RefMarche",
                table: "ElementMarche",
                column: "RefMarche");

            migrationBuilder.CreateIndex(
                name: "IX_Marche_IdCandidatTitulaire",
                table: "Marche",
                column: "IdCandidatTitulaire");

            migrationBuilder.CreateIndex(
                name: "IX_Marche_RefDossier",
                table: "Marche",
                column: "RefDossier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marche_StatutExecution",
                table: "Marche",
                column: "StatutExecution");

            migrationBuilder.CreateIndex(
                name: "IX_PPMDetail_NumPPMEntete",
                table: "PPMDetail",
                column: "NumPPMEntete");

            migrationBuilder.CreateIndex(
                name: "IX_PPMEntete_AnneeExercice",
                table: "PPMEntete",
                column: "AnneeExercice");

            migrationBuilder.CreateIndex(
                name: "IX_SuiviExecution_RefMarche",
                table: "SuiviExecution",
                column: "RefMarche",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcheteurDAO");

            migrationBuilder.DropTable(
                name: "Avenant");

            migrationBuilder.DropTable(
                name: "ElementMarche");

            migrationBuilder.DropTable(
                name: "SuiviExecution");

            migrationBuilder.DropTable(
                name: "Marche");

            migrationBuilder.DropTable(
                name: "Candidat");

            migrationBuilder.DropTable(
                name: "DossierDAO");

            migrationBuilder.DropTable(
                name: "PPMDetail");

            migrationBuilder.DropTable(
                name: "PPMEntete");
        }
    }
}
