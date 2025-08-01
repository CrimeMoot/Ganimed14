using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Dataset;
using Content.Shared.Roles;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Prototypes;
using static Content.Shared.Access.Components.IdCardConsoleComponent;

namespace Content.Client.Access.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class IdCardConsoleWindow : DefaultWindow
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ILogManager _logManager = default!;
        private readonly ISawmill _logMill = default!;

        // Ganimed edit Start
        private static ProtoId<AccessGroupPrototype> ExtendedAccessGroupId = "ExtendedAccess";
        private static ProtoId<AccessGroupPrototype> GeneralAccessGroupId = "GeneralAccess";
        private readonly AccessGroupPrototype? _extendedAccessGroup;
        private readonly AccessGroupPrototype? _generalAccessGroup;
        // Ganimed edit End

        private readonly IdCardConsoleBoundUserInterface _owner;

        private AccessLevelControl _accessButtons = new();
        private readonly List<string> _jobPrototypeIds = new();

        private string? _lastFullName;
        private string? _lastJobTitle;
        private string? _lastJobProto;

        // The job that will be picked if the ID doesn't have a job on the station.
        private static ProtoId<JobPrototype> _defaultJob = "Passenger";

        public IdCardConsoleWindow(IdCardConsoleBoundUserInterface owner, IPrototypeManager prototypeManager,
            List<ProtoId<AccessLevelPrototype>> accessLevels)
        {
            RobustXamlLoader.Load(this);
            IoCManager.InjectDependencies(this);
            _logMill = _logManager.GetSawmill(SharedIdCardConsoleSystem.Sawmill);

            _owner = owner;

            FullNameLineEdit.OnTextEntered += _ => SubmitData();
            FullNameLineEdit.OnTextChanged += _ =>
            {
                FullNameSaveButton.Disabled = FullNameSaveButton.Text == _lastFullName;
            };
            FullNameSaveButton.OnPressed += _ => SubmitData();

            JobTitleLineEdit.OnTextEntered += _ => SubmitData();
            JobTitleLineEdit.OnTextChanged += _ =>
            {
                JobTitleSaveButton.Disabled = JobTitleLineEdit.Text == _lastJobTitle;
            };
            JobTitleSaveButton.OnPressed += _ => SubmitData();

            var jobs = _prototypeManager.EnumeratePrototypes<JobPrototype>().ToList();
            jobs.Sort((x, y) => string.Compare(x.LocalizedName, y.LocalizedName, StringComparison.CurrentCulture));

            foreach (var job in jobs)
            {
                if (!job.OverrideConsoleVisibility.GetValueOrDefault(job.SetPreference))
                {
                    continue;
                }

                _jobPrototypeIds.Add(job.ID);
                JobPresetOptionButton.AddItem(Loc.GetString(job.Name), _jobPrototypeIds.Count - 1);
            }

            // Ganimed edit Start
            if (_prototypeManager.TryIndex(ExtendedAccessGroupId, out var extendedAccess))
            {
                _extendedAccessGroup = extendedAccess;
            }
            else
            {
                _logMill.Error($"Unable to find AccessGroup prototype with ID '{ExtendedAccessGroupId}'");
                //ActionGiveExtendedAccessButton.Disabled = true;
            }

            if (_prototypeManager.TryIndex(GeneralAccessGroupId, out var generalAccess))
            {
                _generalAccessGroup = generalAccess;
            }
            else
            {
                _logMill.Error($"Unable to find AccessGroup prototype with ID '{GeneralAccessGroupId}'");
                //ActionGiveGeneralAccessButton.Disabled = true;
            }
            // Ganimed edit End

            JobPresetOptionButton.OnItemSelected += SelectJobPreset;
            _accessButtons.Populate(accessLevels, prototypeManager);
            AccessLevelControlContainer.AddChild(_accessButtons);

            foreach (var (id, button) in _accessButtons.ButtonsList)
            {
                button.OnPressed += _ => SubmitData();
            }

            // Ganimed edit Start
            ActionGiveFullAccessButton.OnPressed += _ => ActionGiveAllAccess();
            ActionGiveExtendedAccessButton.OnPressed += _ => AddAccessGroup(_extendedAccessGroup);
            ActionGiveGeneralAccessButton.OnPressed += _ => AddAccessGroup(_generalAccessGroup);
            ActionClearAccessButton.OnPressed += _ =>
            {
                ClearAllAccess();
                SubmitData();
            };
            ActionSetJobAccessButton.OnPressed += _ => ResetToDefaultJobAccess();
            // Ganimed edit End
        }

        private void ClearAllAccess()
        {
            foreach (var button in _accessButtons.ButtonsList.Values)
            {
                if (button.Pressed)
                {
                    button.Pressed = false;
                }
            }
        }

        private void SelectJobPreset(OptionButton.ItemSelectedEventArgs args)
        {
            if (!_prototypeManager.TryIndex(_jobPrototypeIds[args.Id], out JobPrototype? job))
            {
                return;
            }

            JobTitleLineEdit.Text = Loc.GetString(job.Name);
            args.Button.SelectId(args.Id);

            // Ganimed edit Start
            SetJobAccess(job);

            // If related code below was changed, then you should also put these changes in SetJobAccess(job).
            /*
            ClearAllAccess();

            // this is a sussy way to do this
            foreach (var access in job.Access)
            {
                if (_accessButtons.ButtonsList.TryGetValue(access, out var button) && !button.Disabled)
                {
                    button.Pressed = true;
                }
            }

            foreach (var group in job.AccessGroups)
            {
                if (!_prototypeManager.TryIndex(group, out AccessGroupPrototype? groupPrototype))
                {
                    continue;
                }

                foreach (var access in groupPrototype.Tags)
                {
                    if (_accessButtons.ButtonsList.TryGetValue(access, out var button) && !button.Disabled)
                    {
                        button.Pressed = true;
                    }
                }
            }
            */
            // Ganimed edit End

            SubmitData();
        }

        public void UpdateState(IdCardConsoleBoundUserInterfaceState state)
        {
            PrivilegedIdButton.Text = state.IsPrivilegedIdPresent
                ? Loc.GetString("id-card-console-window-eject-button")
                : Loc.GetString("id-card-console-window-insert-button");

            PrivilegedIdLabel.Text = state.PrivilegedIdName;

            TargetIdButton.Text = state.IsTargetIdPresent
                ? Loc.GetString("id-card-console-window-eject-button")
                : Loc.GetString("id-card-console-window-insert-button");

            TargetIdLabel.Text = state.TargetIdName;

            var interfaceEnabled =
                state.IsPrivilegedIdPresent && state.IsPrivilegedIdAuthorized && state.IsTargetIdPresent;

            var fullNameDirty = _lastFullName != null && FullNameLineEdit.Text != state.TargetIdFullName;
            var jobTitleDirty = _lastJobTitle != null && JobTitleLineEdit.Text != state.TargetIdJobTitle;

            FullNameLabel.Modulate = interfaceEnabled ? Color.White : Color.Gray;
            FullNameLineEdit.Editable = interfaceEnabled;
            if (!fullNameDirty)
            {
                FullNameLineEdit.Text = state.TargetIdFullName ?? string.Empty;
            }

            FullNameSaveButton.Disabled = !interfaceEnabled || !fullNameDirty;

            JobTitleLabel.Modulate = interfaceEnabled ? Color.White : Color.Gray;
            JobTitleLineEdit.Editable = interfaceEnabled;
            if (!jobTitleDirty)
            {
                JobTitleLineEdit.Text = state.TargetIdJobTitle ?? string.Empty;
            }

            JobTitleSaveButton.Disabled = !interfaceEnabled || !jobTitleDirty;

            JobPresetOptionButton.Disabled = !interfaceEnabled;

            // Ganimed edit Start
            ActionGiveFullAccessButton.Disabled = !interfaceEnabled;
            ActionGiveGeneralAccessButton.Disabled = !interfaceEnabled;
            ActionGiveExtendedAccessButton.Disabled = !interfaceEnabled;
            ActionClearAccessButton.Disabled = !interfaceEnabled;
            ActionSetJobAccessButton.Disabled = !interfaceEnabled;
            // Ganimed edit End

            _accessButtons.UpdateState(state.TargetIdAccessList?.ToList() ??
                                       new List<ProtoId<AccessLevelPrototype>>(),
                                       state.AllowedModifyAccessList?.ToList() ??
                                       new List<ProtoId<AccessLevelPrototype>>());

            var jobIndex = _jobPrototypeIds.IndexOf(state.TargetIdJobPrototype);
            // If the job index is < 0 that means they don't have a job registered in the station records
            // or the IdCardComponent's JobPrototype field.
            // For example, a new ID from a box would have no job index.
            if (jobIndex < 0)
            {
                jobIndex = _jobPrototypeIds.IndexOf(_defaultJob);
            }

            JobPresetOptionButton.SelectId(jobIndex);

            _lastFullName = state.TargetIdFullName;
            _lastJobTitle = state.TargetIdJobTitle;
            _lastJobProto = state.TargetIdJobPrototype;
        }

        private void SubmitData()
        {
            // Don't send this if it isn't dirty.
            var jobProtoDirty = _lastJobProto != null &&
                                _jobPrototypeIds[JobPresetOptionButton.SelectedId] != _lastJobProto;

            _owner.SubmitData(
                FullNameLineEdit.Text,
                JobTitleLineEdit.Text,
                // Iterate over the buttons dictionary, filter by `Pressed`, only get key from the key/value pair
                _accessButtons.ButtonsList.Where(x => x.Value.Pressed).Select(x => x.Key).ToList(),
                jobProtoDirty ? _jobPrototypeIds[JobPresetOptionButton.SelectedId] : string.Empty);
        }

        // Ganimed edit Start
        private void ActionGiveAllAccess()
        {
            foreach (var button in _accessButtons.ButtonsList.Values)
            {
                if (!button.Disabled && button.Pressed == false)
                {
                    button.Pressed = true;
                }
            }

            SubmitData();
        }

        private void AddAccessGroup(AccessGroupPrototype? group)
        {
            if (group == null)
            {
                _logMill.Error($"Attempted to add null access group");
                return;
            }

            foreach (var pair in _accessButtons.ButtonsList)
            {
                if (group.Tags.Contains(pair.Key))
                {
                    if (!pair.Value.Disabled && pair.Value.Pressed == false)
                    {
                        pair.Value.Pressed = true;
                    }
                }
            }
            SubmitData();
        }

        public void SetJobAccess(JobPrototype job)
        {
            ClearAllAccess();

            // this is a sussy way to do this
            foreach (var access in job.Access)
            {
                if (_accessButtons.ButtonsList.TryGetValue(access, out var button) && !button.Disabled)
                {
                    button.Pressed = true;
                }
            }

            foreach (var group in job.AccessGroups)
            {
                if (!_prototypeManager.TryIndex(group, out AccessGroupPrototype? groupPrototype))
                {
                    continue;
                }

                foreach (var access in groupPrototype.Tags)
                {
                    if (_accessButtons.ButtonsList.TryGetValue(access, out var button) && !button.Disabled)
                    {
                        button.Pressed = true;
                    }
                }
            }

            SubmitData();
        }

        public void ResetToDefaultJobAccess()
        {
            JobPrototype job;
            if (_lastJobProto != null && _prototypeManager.TryIndex<JobPrototype>(_lastJobProto, out var nullableJob))
            {
                job = nullableJob;
            }
            else
            {
                if (!_prototypeManager.TryIndex(_defaultJob, out var defaultJob))
                {
                    _logMill.Error($"Couldn't assign default job access, because default job not found");
                    return;
                }
                job = defaultJob;
            }

            SetJobAccess(job);
        }

        // Ganimed edit End
    }
}