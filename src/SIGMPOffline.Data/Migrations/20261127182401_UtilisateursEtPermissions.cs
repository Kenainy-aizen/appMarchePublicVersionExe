using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIGMPOffline.Data.Migrations
{
    /// <inheritdoc />
    public partial class UtilisateursEtPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Utilisateur",
                columns: table => new
                {
                    IdUtilisateur = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NomUtilisateur = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false, collation: "NOCASE"),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Salt = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EstAdministrateur = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstActif = table.Column<bool>(type: "INTEGER", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateur", x => x.IdUtilisateur);
                });

            migrationBuilder.CreateTable(
                name: "PermissionProvince",
                columns: table => new
                {
                    IdUtilisateur = table.Column<int>(type: "INTEGER", nullable: false),
                    IdProvince = table.Column<int>(type: "INTEGER", nullable: false),
                    PeutModifier = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionProvince", x => new { x.IdUtilisateur, x.IdProvince });
                    table.ForeignKey(
                        name: "FK_PermissionProvince_Province_IdProvince",
                        column: x => x.IdProvince,
                        principalTable: "Province",
                        principalColumn: "IdProvince",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PermissionProvince_Utilisateur_IdUtilisateur",
                        column: x => x.IdUtilisateur,
                        principalTable: "Utilisateur",
                        principalColumn: "IdUtilisateur",
                        onDelete: ReferentialAction.Cascade);
                });

            // Conserve l'accès historique : le compte administrateur PRMP reprend
            // le mot de passe de la première province créée.
            migrationBuilder.Sql(
                """
                INSERT INTO "Utilisateur"
                    ("NomUtilisateur", "PasswordHash", "Salt", "EstAdministrateur", "EstActif", "DateCreation")
                SELECT 'Province-' || p."IdProvince", p."PasswordHash", p."Salt", 0, 1, p."DateCreation"
                FROM "Province" AS p;
                """);
            migrationBuilder.Sql(
                """
                INSERT INTO "PermissionProvince" ("IdUtilisateur", "IdProvince", "PeutModifier")
                SELECT u."IdUtilisateur", p."IdProvince", 1
                FROM "Province" AS p
                INNER JOIN "Utilisateur" AS u ON u."NomUtilisateur" = 'Province-' || p."IdProvince";
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO "Utilisateur"
                    ("NomUtilisateur", "PasswordHash", "Salt", "EstAdministrateur", "EstActif", "DateCreation")
                SELECT COALESCE(NULLIF(TRIM(c."NomProprietairePRMP"), ''), 'PRMP'),
                       p."PasswordHash", p."Salt", 1, 1, p."DateCreation"
                FROM "Province" AS p
                LEFT JOIN "ConfigurationApp" AS c ON 1 = 1
                WHERE p."IdProvince" = (SELECT MIN("IdProvince") FROM "Province");
                """);

            // Migre aussi les accès visiteurs éventuellement créés avant cette évolution
            // en comptes de consultation affectés uniquement à leur province.
            migrationBuilder.Sql(
                """
                INSERT INTO "Utilisateur"
                    ("NomUtilisateur", "PasswordHash", "Salt", "EstAdministrateur", "EstActif", "DateCreation")
                SELECT 'Visiteur-' || p."IdProvince", p."VisitorPasswordHash", p."VisitorSalt", 0, 1, p."DateCreation"
                FROM "Province" AS p
                WHERE p."VisitorPasswordHash" IS NOT NULL AND p."VisitorSalt" IS NOT NULL;
                """);
            migrationBuilder.Sql(
                """
                INSERT INTO "PermissionProvince" ("IdUtilisateur", "IdProvince", "PeutModifier")
                SELECT u."IdUtilisateur", p."IdProvince", 0
                FROM "Province" AS p
                INNER JOIN "Utilisateur" AS u ON u."NomUtilisateur" = 'Visiteur-' || p."IdProvince"
                WHERE p."VisitorPasswordHash" IS NOT NULL AND p."VisitorSalt" IS NOT NULL;
                """);

            migrationBuilder.DropColumn(name: "PasswordHash", table: "Province");
            migrationBuilder.DropColumn(name: "Salt", table: "Province");
            migrationBuilder.DropColumn(name: "VisitorPasswordHash", table: "Province");
            migrationBuilder.DropColumn(name: "VisitorSalt", table: "Province");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionProvince_IdProvince",
                table: "PermissionProvince",
                column: "IdProvince");

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateur_NomUtilisateur",
                table: "Utilisateur",
                column: "NomUtilisateur",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Province",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Salt",
                table: "Province",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitorPasswordHash",
                table: "Province",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitorSalt",
                table: "Province",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            // Le schéma précédent avait un mot de passe par province ; au retour arrière,
            // réutilise le mot de passe admin comme identifiant de secours sur chaque province.
            migrationBuilder.Sql(
                """
                UPDATE "Province"
                SET "PasswordHash" = COALESCE(
                        (SELECT "PasswordHash" FROM "Utilisateur"
                         WHERE "NomUtilisateur" = 'Province-' || "Province"."IdProvince" LIMIT 1),
                        (SELECT "PasswordHash" FROM "Utilisateur" WHERE "EstAdministrateur" = 1 LIMIT 1), ''),
                    "Salt" = COALESCE(
                        (SELECT "Salt" FROM "Utilisateur"
                         WHERE "NomUtilisateur" = 'Province-' || "Province"."IdProvince" LIMIT 1),
                        (SELECT "Salt" FROM "Utilisateur" WHERE "EstAdministrateur" = 1 LIMIT 1), '');
                """);
            migrationBuilder.Sql(
                """
                UPDATE "Province"
                SET "VisitorPasswordHash" = (
                        SELECT "PasswordHash" FROM "Utilisateur"
                        WHERE "NomUtilisateur" = 'Visiteur-' || "Province"."IdProvince" LIMIT 1),
                    "VisitorSalt" = (
                        SELECT "Salt" FROM "Utilisateur"
                        WHERE "NomUtilisateur" = 'Visiteur-' || "Province"."IdProvince" LIMIT 1);
                """);

            migrationBuilder.DropTable(name: "PermissionProvince");
            migrationBuilder.DropTable(name: "Utilisateur");
        }
    }
}
