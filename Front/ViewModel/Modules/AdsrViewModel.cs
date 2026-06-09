using System.Windows.Input;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class AdsrViewModel : ModuleViewModuleBase
    {
        private readonly AdsrModule _adsrModel;

        // ─────────────────────────────────────────────
        //  Paramètres ADSR
        // ─────────────────────────────────────────────

        public double Attack
        {
            get => _adsrModel.Attack;
            set
            {
                _adsrModel.Attack = (float)value;
                NotifyPropertyChanged();
            }
        }

        public double Decay
        {
            get => _adsrModel.Decay;
            set
            {
                _adsrModel.Decay = (float)value;
                NotifyPropertyChanged();
            }
        }

        public double Sustain
        {
            get => _adsrModel.Sustain;
            set
            {
                _adsrModel.Sustain = (float)value;
                NotifyPropertyChanged();
            }
        }

        public double Release
        {
            get => _adsrModel.Release;
            set
            {
                _adsrModel.Release = (float)value;
                NotifyPropertyChanged();
            }
        }

        // ─────────────────────────────────────────────
        //  Gate manuel (bouton TEST dans l'UI)
        // ─────────────────────────────────────────────

        private bool _isGateHeld;
        public bool IsGateHeld
        {
            get => _isGateHeld;
            set
            {
                _isGateHeld = value;
                NotifyPropertyChanged();
                if (_isGateHeld)
                    _adsrModel.ManualGateOn();
                else
                    _adsrModel.ManualGateOff();
            }
        }

        /// <summary>Toggle Gate On/Off via bouton UI.</summary>
        public ICommand ToggleGateCommand { get; }

        // ─────────────────────────────────────────────
        //  Construction
        // ─────────────────────────────────────────────

        public AdsrViewModel(AdsrModule model) : base(model)
        {
            _adsrModel = model;

            // Ports
            InputPorts.Add(new PortViewModelBase(_adsrModel.AudioInput));
            InputPorts.Add(new PortViewModelBase(_adsrModel.GateInput));
            OutputPorts.Add(new PortViewModelBase(_adsrModel.AudioOutput));
            OutputPorts.Add(new PortViewModelBase(_adsrModel.EnvOutput));

            // Commande toggle gate
            ToggleGateCommand = new RelayCommand(_ => IsGateHeld = !IsGateHeld);
        }
    }
}
