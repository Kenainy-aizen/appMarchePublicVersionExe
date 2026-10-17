using SIGMPOffline.Domain.Common;

namespace SIGMPOffline.Domain.Tests;

/// <summary>Tests du hachage sécurisé des mots de passe (PBKDF2).</summary>
public class PasswordHasherTests
{
    [Fact]
    public void Verifier_accepte_le_bon_mot_de_passe()
    {
        var (hash, sel) = PasswordHasher.Hacher("secret-2026");

        Assert.True(PasswordHasher.Verifier("secret-2026", hash, sel));
    }

    [Fact]
    public void Verifier_refuse_un_mot_de_passe_incorrect()
    {
        var (hash, sel) = PasswordHasher.Hacher("secret-2026");

        Assert.False(PasswordHasher.Verifier("secret-2027", hash, sel));
    }

    [Fact]
    public void Hacher_genere_un_sel_different_a_chaque_appel()
    {
        var (hash1, sel1) = PasswordHasher.Hacher("meme-mot-de-passe");
        var (hash2, sel2) = PasswordHasher.Hacher("meme-mot-de-passe");

        Assert.NotEqual(sel1, sel2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verifier_retourne_faux_pour_des_donnees_corrompues()
    {
        Assert.False(PasswordHasher.Verifier("secret", "pas-du-base64", "%%%"));
        Assert.False(PasswordHasher.Verifier("secret", string.Empty, string.Empty));
    }

    [Fact]
    public void Hacher_refuse_un_mot_de_passe_vide()
    {
        Assert.Throws<DomainException>(() => PasswordHasher.Hacher("   "));
    }
}
