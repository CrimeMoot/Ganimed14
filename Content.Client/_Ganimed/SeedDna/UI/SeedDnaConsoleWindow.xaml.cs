using Content.Shared._Ganimed.SeedDna;
using Content.Shared.Atmos;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Ganimed.SeedDna.UI;

[GenerateTypedNameReferences]
public sealed partial class SeedDnaConsoleWindow : DefaultWindow
{
    [Dependency] private readonly ILocalizationManager _localizationManager = default!;

    private readonly List<SeedDnaConsoleWindowRow> _allRows = new();

    private readonly SeedDnaConsoleBoundUserInterface _owner;

    /// <summary>
    /// Флаг немедленной отправки обновлений на сервер.
    /// </summary>
    private bool _flagUpdateImmediately = true;

    private bool _seedIsPresent;
    private bool _dnaDiskIsPresent;
    private SeedDataDto? _seedDataDto;
    private SeedDataDto? _dnaDiskDataDto;

    public SeedDnaConsoleWindow(SeedDnaConsoleBoundUserInterface owner)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _owner = owner;

        ExtractAllButton.OnPressed += _ =>
        {
            _flagUpdateImmediately = false;

            foreach (var row in _allRows)
            {
                row.DoExtract();
            }

            ExtractAllButton.Disabled = true;
            ReplaceAllButton.Disabled = true;

            SubmitUpdateDto(TargetSeedData.DnaDisk);
            _flagUpdateImmediately = true;
        };

        ReplaceAllButton.OnPressed += _ =>
        {
            _flagUpdateImmediately = false;

            foreach (var row in _allRows)
            {
                row.DoReplace();
            }

            ExtractAllButton.Disabled = true;
            ReplaceAllButton.Disabled = true;

            SubmitUpdateDto(TargetSeedData.Seed);
            _flagUpdateImmediately = true;
        };
    }

    /// <summary>
    /// Выполняется всегда при открытии или обновлении состояния UI.
    /// </summary>
    public void UpdateState(SeedDnaConsoleBoundUserInterfaceState state)
    {
        UpdateEjectButtons(state);

        if (!ValidateSeed(state))
            return;

        SetupDataDto(state);
        CleanRows();
        FillRows();
        UpdateButtonsAll(state);
    }

    /// <summary>
    /// Обновление кнопок изъятия предпетов
    /// </summary>
    private void UpdateEjectButtons(SeedDnaConsoleBoundUserInterfaceState state)
    {
        _seedIsPresent = state.IsSeedsPresent;
        _dnaDiskIsPresent = state.IsDnaDiskPresent;

        SeedButton.Text = state.IsSeedsPresent
            ? Loc.GetString("seed-dna-eject-btn")
            : Loc.GetString("seed-dna-insert-btn");
        SeedLabel.Text = state.SeedsName;

        DnaDiskButton.Text = state.IsDnaDiskPresent
            ? Loc.GetString("seed-dna-eject-btn")
            : Loc.GetString("seed-dna-insert-btn");
        DnaDiskLabel.Text = state.DnaDiskName;
    }

    /// <summary>
    /// Проверка возможности использования семян
    /// </summary>
    /// <returns>true, если семена можно использовать</returns>
    private bool ValidateSeed(SeedDnaConsoleBoundUserInterfaceState state)
    {
        if (state is { IsSeedsPresent: true, SeedData: null })
        {
            InvalidSeedLabel.Visible = true;
            MutationContainer.Visible = false;
            ExtractAllButton.Disabled = true;
            ReplaceAllButton.Disabled = true;
            return false;
        }

        InvalidSeedLabel.Visible = false;
        MutationContainer.Visible = true;
        return true;
    }

    /// <summary>
    /// Без локализации сервер просто падает. ЭТО УЖАС
    /// </summary>
    private string GetOrPlaceholder(string key)
    {
        if (_localizationManager.TryGetString(key, out var localized))
            return localized;

        return "???";
    }

    /// <summary>
    /// Получаем DTO данные
    /// </summary>
    private void SetupDataDto(SeedDnaConsoleBoundUserInterfaceState state)
    {
        _seedDataDto = state.SeedData ?? new SeedDataDto();
        _dnaDiskDataDto = state.DnaDiskData ?? new SeedDataDto();

        EnsureDtoCollectionsInitialized(_seedDataDto);
        EnsureDtoCollectionsInitialized(_dnaDiskDataDto);
    }

    private void EnsureDtoCollectionsInitialized(SeedDataDto dto)
    {
        if (dto == null) return;

        dto.Chemicals ??= new Dictionary<string, SeedChemQuantityDto>();
        dto.ConsumeGasses ??= new Dictionary<Gas, float>();
        dto.ExudeGasses ??= new Dictionary<Gas, float>();
    }

    /// <summary>
    /// Очистка "таблицы"
    /// </summary>
    private void CleanRows()
    {
        MutationContainer.Children.Clear();
        ExtractAllButton.Disabled = true;
        ReplaceAllButton.Disabled = true;

        _allRows.Clear();
    }

    private void FillRows()
    {
        if (!_seedIsPresent && !_dnaDiskIsPresent)
            return;

        // Chemicals
        if (_seedDataDto?.Chemicals?.Count > 0 || _dnaDiskDataDto?.Chemicals?.Count > 0)
        {
            List<string> processed = new List<string>();

            if (_seedDataDto?.Chemicals != null)
            {
                foreach (var (chemicalName, _) in _seedDataDto!.Chemicals)
                {
                    processed.Add(chemicalName);

                    var title = GetOrPlaceholder($"reagent-name-{chemicalName.ToLower()}");

                    AppendRow(SeedDnaConsoleWindowRow.Create(
                        title: title,
                        seedPresent: _seedIsPresent,
                        dnaDiskPresent: _dnaDiskIsPresent,
                        getterSeedValue: () => _seedDataDto!.Chemicals![chemicalName],
                        setterSeedValue: obj => { _seedDataDto!.Chemicals![chemicalName] = (SeedChemQuantityDto)obj!; },
                        getterDnaDiskValue: () =>
                        {
                            if (_dnaDiskDataDto != null
                                && _dnaDiskDataDto!.Chemicals != null
                                && _dnaDiskDataDto!.Chemicals.TryGetValue(chemicalName, out var value))
                                return value;
                            return null;
                        },
                        setterDnaDiskValue: obj =>
                        {
                            _dnaDiskDataDto!.Chemicals ??= new Dictionary<string, SeedChemQuantityDto>();
                            _dnaDiskDataDto!.Chemicals[chemicalName] = (SeedChemQuantityDto)obj!;
                        },
                        flagUpdateImmediately: () => _flagUpdateImmediately,
                        submit: SubmitUpdateDto
                    ));
                }
            }

            if (_dnaDiskDataDto?.Chemicals != null)
            {
                foreach (var (chemicalName, _) in _dnaDiskDataDto!.Chemicals)
                {
                    if (processed.Remove(chemicalName))
                        continue;

                    var title = GetOrPlaceholder($"reagent-name-{chemicalName.ToLower()}");

                    AppendRow(SeedDnaConsoleWindowRow.Create(
                        title: title,
                        seedPresent: _seedIsPresent,
                        dnaDiskPresent: _dnaDiskIsPresent,
                        getterSeedValue: () =>
                        {
                            if (_seedDataDto != null
                                && _seedDataDto!.Chemicals != null
                                && _seedDataDto!.Chemicals.TryGetValue(chemicalName, out var value))
                                return value;
                            return null;
                        },
                        setterSeedValue: obj =>
                        {
                            _seedDataDto!.Chemicals ??= new Dictionary<string, SeedChemQuantityDto>();
                            _seedDataDto!.Chemicals[chemicalName] = (SeedChemQuantityDto)obj!;
                        },
                        getterDnaDiskValue: () => _dnaDiskDataDto!.Chemicals![chemicalName],
                        setterDnaDiskValue: obj => { _dnaDiskDataDto!.Chemicals![chemicalName] = (SeedChemQuantityDto)obj!; },
                        flagUpdateImmediately: () => _flagUpdateImmediately,
                        submit: SubmitUpdateDto
                    ));
                }
            }
        }

        // ConsumeGasses
        if (_seedDataDto?.ConsumeGasses?.Count > 0 || _dnaDiskDataDto?.ConsumeGasses?.Count > 0)
        {
            foreach (var gas in Enum.GetValues<Gas>())
            {
                var gasLoc = GetOrPlaceholder($"seed-dna-gas-{gas}"); // Локализация изначально неправильная. По этому будет ??? Когда-нибудь починить.

                AppendRow(SeedDnaConsoleWindowRow.Create(
                    title: Loc.GetString("seed-dna-row-consume-gas", ("gas", gasLoc)),
                    seedPresent: _seedIsPresent,
                    dnaDiskPresent: _dnaDiskIsPresent,
                    getterSeedValue: () =>
                    {
                        if (_seedDataDto != null
                            && _seedDataDto!.ConsumeGasses != null
                            && _seedDataDto!.ConsumeGasses!.TryGetValue(gas, out var value))
                            return value;
                        return null;
                    },
                    setterSeedValue: obj =>
                    {
                        _seedDataDto!.ConsumeGasses ??= new Dictionary<Gas, float>();
                        _seedDataDto!.ConsumeGasses[gas] = (float)obj!;
                    },
                    getterDnaDiskValue: () =>
                    {
                        if (_dnaDiskDataDto != null
                            && _dnaDiskDataDto!.ConsumeGasses != null
                            && _dnaDiskDataDto!.ConsumeGasses!.TryGetValue(gas, out var value))
                            return value;
                        return null;
                    },
                    setterDnaDiskValue: obj =>
                    {
                        _dnaDiskDataDto!.ConsumeGasses ??= new Dictionary<Gas, float>();
                        _dnaDiskDataDto!.ConsumeGasses[gas] = (float)obj!;
                    },
                    flagUpdateImmediately: () => _flagUpdateImmediately,
                    submit: SubmitUpdateDto
                ));
            }
        }

        // ExudeGasses
        if (_seedDataDto?.ExudeGasses?.Count > 0 || _dnaDiskDataDto?.ExudeGasses?.Count > 0)
        {
            foreach (var gas in Enum.GetValues<Gas>())
            {
                var gasLoc = GetOrPlaceholder($"seed-dna-gas-{gas}");

                AppendRow(SeedDnaConsoleWindowRow.Create(
                    title: Loc.GetString("seed-dna-row-exude-gas", ("gas", gasLoc)),
                    seedPresent: _seedIsPresent,
                    dnaDiskPresent: _dnaDiskIsPresent,
                    getterSeedValue: () =>
                    {
                        if (_seedDataDto != null
                            && _seedDataDto!.ExudeGasses != null
                            && _seedDataDto!.ExudeGasses!.TryGetValue(gas, out var value))
                            return value;
                        return null;
                    },
                    setterSeedValue: obj =>
                    {
                        _seedDataDto!.ExudeGasses ??= new Dictionary<Gas, float>();
                        _seedDataDto!.ExudeGasses[gas] = (float)obj!;
                    },
                    getterDnaDiskValue: () =>
                    {
                        if (_dnaDiskDataDto != null
                            && _dnaDiskDataDto!.ExudeGasses != null
                            && _dnaDiskDataDto!.ExudeGasses!.TryGetValue(gas, out var value))
                            return value;
                        return null;
                    },
                    setterDnaDiskValue: obj =>
                    {
                        _dnaDiskDataDto!.ExudeGasses ??= new Dictionary<Gas, float>();
                        _dnaDiskDataDto!.ExudeGasses[gas] = (float)obj!;
                    },
                    flagUpdateImmediately: () => _flagUpdateImmediately,
                    submit: SubmitUpdateDto
                ));
            }
        }

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-NutrientConsumption"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.NutrientConsumption,
            setterSeedValue: obj => { _seedDataDto!.NutrientConsumption = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.NutrientConsumption,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.NutrientConsumption = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-WaterConsumption"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.WaterConsumption,
            setterSeedValue: obj => { _seedDataDto!.WaterConsumption = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.WaterConsumption,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.WaterConsumption = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-IdealHeat"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.IdealHeat,
            setterSeedValue: obj => { _seedDataDto!.IdealHeat = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.IdealHeat,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.IdealHeat = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-HeatTolerance"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.HeatTolerance,
            setterSeedValue: obj => { _seedDataDto!.HeatTolerance = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.HeatTolerance,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.HeatTolerance = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-ToxinsTolerance"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.ToxinsTolerance,
            setterSeedValue: obj => { _seedDataDto!.ToxinsTolerance = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.ToxinsTolerance,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.ToxinsTolerance = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-LowPressureTolerance"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.LowPressureTolerance,
            setterSeedValue: obj => { _seedDataDto!.LowPressureTolerance = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.LowPressureTolerance,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.LowPressureTolerance = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-HighPressureTolerance"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.HighPressureTolerance,
            setterSeedValue: obj => { _seedDataDto!.HighPressureTolerance = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.HighPressureTolerance,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.HighPressureTolerance = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-PestTolerance"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.PestTolerance,
            setterSeedValue: obj => { _seedDataDto!.PestTolerance = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.PestTolerance,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.PestTolerance = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-WeedTolerance"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.WeedTolerance,
            setterSeedValue: obj => { _seedDataDto!.WeedTolerance = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.WeedTolerance,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.WeedTolerance = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-Endurance"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.Endurance,
            setterSeedValue: obj => { _seedDataDto!.Endurance = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.Endurance,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.Endurance = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-Yield"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.Yield,
            setterSeedValue: obj => { _seedDataDto!.Yield = (int)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.Yield,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.Yield = (int)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-Lifespan"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.Lifespan,
            setterSeedValue: obj => { _seedDataDto!.Lifespan = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.Lifespan,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.Lifespan = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-Maturation"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.Maturation,
            setterSeedValue: obj => { _seedDataDto!.Maturation = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.Maturation,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.Maturation = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-Production"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.Production,
            setterSeedValue: obj => { _seedDataDto!.Production = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.Production,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.Production = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-HarvestRepeat"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.HarvestRepeat,
            setterSeedValue: obj => { _seedDataDto!.HarvestRepeat = (SharedHarvestTypeDto)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.HarvestRepeat,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.HarvestRepeat = (SharedHarvestTypeDto)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-Potency"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.Potency,
            setterSeedValue: obj => { _seedDataDto!.Potency = (float)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.Potency,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.Potency = (float)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-Seedless"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.Seedless,
            setterSeedValue: obj => { _seedDataDto!.Seedless = (bool)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.Seedless,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.Seedless = (bool)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-Viable"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.Viable,
            setterSeedValue: obj => { _seedDataDto!.Viable = (bool)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.Viable,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.Viable = (bool)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-Ligneous"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.Ligneous,
            setterSeedValue: obj => { _seedDataDto!.Ligneous = (bool)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.Ligneous,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.Ligneous = (bool)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));

        AppendRow(SeedDnaConsoleWindowRow.Create(
            title: Loc.GetString("seed-dna-row-CanScream"),
            seedPresent: _seedIsPresent,
            dnaDiskPresent: _dnaDiskIsPresent,
            getterSeedValue: () => _seedDataDto?.CanScream,
            setterSeedValue: obj => { _seedDataDto!.CanScream = (bool)obj!; },
            getterDnaDiskValue: () => _dnaDiskDataDto?.CanScream,
            setterDnaDiskValue: obj => { _dnaDiskDataDto!.CanScream = (bool)obj!; },
            flagUpdateImmediately: () => _flagUpdateImmediately,
            submit: SubmitUpdateDto
        ));
    }

    /// <summary>
    /// Обновление состояние для "*All" кнопок.
    /// </summary>
    private void UpdateButtonsAll(SeedDnaConsoleBoundUserInterfaceState state)
    {
        if (!state.IsSeedsPresent || !state.IsDnaDiskPresent)
            return;

        if (state.SeedData != null)
            ExtractAllButton.Disabled = false;

        if (state.DnaDiskData != null)
            ReplaceAllButton.Disabled = false;
    }

    private void SubmitUpdateDto(TargetSeedData target)
    {
        var seedDataDto = target == TargetSeedData.Seed
            ? _seedDataDto!
            : _dnaDiskDataDto!;

        _owner.SubmitData(target, seedDataDto);
    }

    private void AppendRow(SeedDnaConsoleWindowRow? row)
    {
        if (row == null) return;
        row.IncludeToContainer(MutationContainer);
        _allRows.Add(row);
    }
}