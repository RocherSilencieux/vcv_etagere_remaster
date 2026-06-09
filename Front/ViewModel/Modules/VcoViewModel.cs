using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class VcoViewModel : ModuleViewModuleBase
    {
        private readonly VcoModule _vcoModel;

        public double BaseFrequency
        {
            get => _vcoModel != null ? _vcoModel.BaseFrequency : 0;
            set
            {
                if (_vcoModel != null)
                {
                    _vcoModel.BaseFrequency = value;
                    NotifyPropertyChanged();
                }
            }
        }

        // ─────────────────────────────────────────────
        //  Active / Bypass
        // ─────────────────────────────────────────────

        public bool IsBypassed
        {
            get => _vcoModel.IsBypassed;
            set
            {
                _vcoModel.IsBypassed = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsActive));
            }
        }

        public bool IsActive
        {
            get => !IsBypassed;
            set => IsBypassed = !value;
        }

        /// <summary>Toggle active/bypass depuis le bouton UI.</summary>
        public ICommand ToggleActiveCommand { get; }

        // ─────────────────────────────────────────────
        //  Construction
        // ─────────────────────────────────────────────

        public VcoViewModel(VcoModule model) : base(model)
        {
            _vcoModel = model;

            // Map model ports to view models
            InputPorts.Add(new PortViewModelBase(_vcoModel.FrequencyInput));
            OutputPorts.Add(new PortViewModelBase(_vcoModel.AudioOutput));

            ToggleActiveCommand = new RelayCommand(_ => IsActive = !IsActive);
        }
    }
}
