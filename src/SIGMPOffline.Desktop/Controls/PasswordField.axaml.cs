using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using FormeTrait = Avalonia.Controls.Shapes.Path;

namespace SIGMPOffline.Desktop.Controls;

/// <summary>
/// Champ de mot de passe réutilisable : saisie masquée par défaut et bouton
/// « œil » intégré permettant d'afficher ou de masquer la valeur.
/// </summary>
/// <remarks>
/// Le contrôle expose <see cref="Text"/> en deux voies : il reste donc
/// interchangeable avec un <see cref="TextBox"/> classique, sans modifier les
/// ViewModels ni les validations existantes.
/// </remarks>
public partial class PasswordField : UserControl
{
    /// <summary>Texte saisi (relié en deux voies au ViewModel).</summary>
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<PasswordField, string>(
            nameof(Text),
            defaultValue: string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Texte indicatif affiché lorsque le champ est vide.</summary>
    public static readonly StyledProperty<string> PlaceholderTextProperty =
        AvaloniaProperty.Register<PasswordField, string>(nameof(PlaceholderText), string.Empty);

    /// <summary>Nom lu par les technologies d'assistance (lecteurs d'écran).</summary>
    public static readonly StyledProperty<string> NomAccessibleProperty =
        AvaloniaProperty.Register<PasswordField, string>(nameof(NomAccessible), string.Empty);

    /// <summary>Signale visuellement une saisie invalide.</summary>
    public static readonly StyledProperty<bool> EstInvalideProperty =
        AvaloniaProperty.Register<PasswordField, bool>(nameof(EstInvalide));

    private const char CaractereMasque = '●';

    private readonly Border? _cadre;
    private readonly TextBox? _saisie;
    private readonly Button? _bascule;
    private readonly FormeTrait? _oeil;
    private readonly FormeTrait? _oeilBarre;
    private bool _synchronisation;

    public PasswordField()
    {
        InitializeComponent();

        _cadre = this.FindControl<Border>("PART_Cadre");
        _saisie = this.FindControl<TextBox>("PART_Saisie");
        _bascule = this.FindControl<Button>("PART_Bascule");
        _oeil = this.FindControl<FormeTrait>("PART_Oeil");
        _oeilBarre = this.FindControl<FormeTrait>("PART_OeilBarre");

        if (_saisie is not null)
        {
            _saisie.TextChanged += SurSaisieModifiee;
        }

        if (_bascule is not null)
        {
            _bascule.Click += SurBascule;
        }

        MettreAJourVisibilite(visible: false);
    }

    /// <inheritdoc cref="TextProperty" />
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <inheritdoc cref="PlaceholderTextProperty" />
    public string PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <inheritdoc cref="NomAccessibleProperty" />
    public string NomAccessible
    {
        get => GetValue(NomAccessibleProperty);
        set => SetValue(NomAccessibleProperty, value);
    }

    /// <inheritdoc cref="EstInvalideProperty" />
    public bool EstInvalide
    {
        get => GetValue(EstInvalideProperty);
        set => SetValue(EstInvalideProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty)
        {
            SynchroniserSaisie(change.GetNewValue<string>());
        }
        else if (change.Property == PlaceholderTextProperty && _saisie is not null)
        {
            _saisie.PlaceholderText = change.GetNewValue<string>();
        }
        else if (change.Property == NomAccessibleProperty && _saisie is not null)
        {
            AutomationProperties.SetName(_saisie, change.GetNewValue<string>());
        }
        else if (change.Property == EstInvalideProperty && _cadre is not null)
        {
            _cadre.Classes.Set("invalide", change.GetNewValue<bool>());
        }
    }

    private void SynchroniserSaisie(string? valeur)
    {
        if (_saisie is null)
        {
            return;
        }

        var texte = valeur ?? string.Empty;
        if (_saisie.Text == texte)
        {
            return;
        }

        _synchronisation = true;
        _saisie.Text = texte;
        _synchronisation = false;
    }

    private void SurSaisieModifiee(object? sender, TextChangedEventArgs e)
    {
        if (_synchronisation || _saisie is null)
        {
            return;
        }

        Text = _saisie.Text ?? string.Empty;
    }

    private void SurBascule(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_saisie is null)
        {
            return;
        }

        var texteVisible = _saisie.PasswordChar == '\0';
        MettreAJourVisibilite(!texteVisible);

        // Le bouton ne doit pas voler le focus : on le rend à la saisie.
        _saisie.Focus();
        _saisie.CaretIndex = _saisie.Text?.Length ?? 0;
    }

    private void MettreAJourVisibilite(bool visible)
    {
        if (_saisie is not null)
        {
            _saisie.PasswordChar = visible ? '\0' : CaractereMasque;
        }

        if (_oeil is not null)
        {
            _oeil.Opacity = visible ? 0d : 1d;
        }

        if (_oeilBarre is not null)
        {
            _oeilBarre.Opacity = visible ? 1d : 0d;
        }

        var libelle = visible ? "Masquer le mot de passe" : "Afficher le mot de passe";

        if (_bascule is not null)
        {
            ToolTip.SetTip(_bascule, libelle);
            AutomationProperties.SetName(_bascule, libelle);
        }
    }
}
