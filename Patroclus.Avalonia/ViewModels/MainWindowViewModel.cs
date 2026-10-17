using ReactiveUI;
using System.Collections.Generic;

namespace Patroclus.Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private FakeRadio _radio;
        public FakeRadio radio
        {
            get { return _radio; }
            private set { this.RaiseAndSetIfChanged(ref _radio, value); }
        }

        public IReadOnlyList<RadioProfile> RadioProfiles => RadioProfile.All;

        private RadioProfile _selectedProfile;
        public RadioProfile SelectedProfile
        {
            get { return _selectedProfile; }
            set
            {
                if (value == null || value == _selectedProfile) return;
                LoadRadio(value);
                this.RaiseAndSetIfChanged(ref _selectedProfile, value);
            }
        }

        public MainWindowViewModel()
        {
            SelectedProfile = RadioProfiles[0];
        }

        private void LoadRadio(RadioProfile profile)
        {
            radio?.Stop();
            if (profile.Protocol == 1)
            {
                var hermes = new FakeHermes { Profile = profile, port = 1024 };
                hermes.start();
                radio = hermes;
            }
            else
            {
                var hermes = new FakeHermesNewProtocol { Profile = profile, port = 1024 };
                hermes.start();
                radio = hermes;
            }
        }
    }
}
