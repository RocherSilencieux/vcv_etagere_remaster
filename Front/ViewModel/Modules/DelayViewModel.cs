using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class DelayModeDescription
    {
        public DelayMode Mode { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DelayViewModel : ModuleViewModuleBase
    {
        private readonly DelayModule _delayModel = null!;

        public double DelayTimeMs
        {
            get => _delayModel != null ? _delayModel.DelayTimeMs : 500.0;
            set
            {
                if (_delayModel != null)
                {
                    _delayModel.DelayTimeMs = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Feedback
        {
            get => _delayModel != null ? _delayModel.Feedback : 0.5;
            set
            {
                if (_delayModel != null)
                {
                    _delayModel.Feedback = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Mix
        {
            get => _delayModel != null ? _delayModel.Mix : 0.5;
            set
            {
                if (_delayModel != null)
                {
                    _delayModel.Mix = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsBypassed
        {
            get => _delayModel != null ? _delayModel.IsBypassed : false;
            set
            {
                if (_delayModel != null)
                {
                    _delayModel.IsBypassed = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged(nameof(IsActive));
                }
            }
        }

        public bool IsActive
        {
            get => !IsBypassed;
            set => IsBypassed = !value;
        }

        public ICommand ToggleActiveCommand { get; }

        public IEnumerable<DelayModeDescription> AvailablePresets { get; } = new[]
        {
            new DelayModeDescription { Mode = DelayMode.Simple, Name = "Simple (Stéréo)" },
            new DelayModeDescription { Mode = DelayMode.PingPong, Name = "Ping-Pong" },
            new DelayModeDescription { Mode = DelayMode.Mono, Name = "Mono" }
        };

        private DelayModeDescription? _selectedPreset;
        public DelayModeDescription? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (_selectedPreset != value)
                {
                    _selectedPreset = value;
                    NotifyPropertyChanged();
                    if (_selectedPreset != null && _delayModel != null)
                    {
                        _delayModel.Mode = _selectedPreset.Mode;
                    }
                }
            }
        }

        public DelayViewModel(DelayModule model) : base(model)
        {
            _delayModel = model;

            InputPorts.Add(new PortViewModelBase(_delayModel.LeftInput));
            InputPorts.Add(new PortViewModelBase(_delayModel.RightInput));
            OutputPorts.Add(new PortViewModelBase(_delayModel.LeftOutput));
            OutputPorts.Add(new PortViewModelBase(_delayModel.RightOutput));

            ToggleActiveCommand = new RelayCommand(_ => IsActive = !IsActive);

            if (_delayModel != null)
            {
                _selectedPreset = AvailablePresets.FirstOrDefault(p => p.Mode == _delayModel.Mode)
                                  ?? AvailablePresets.FirstOrDefault();
            }
        }
    }
}

