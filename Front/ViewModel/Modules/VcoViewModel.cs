using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class VcoWaveformDescription
    {
        public VcoWaveform Waveform { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class VcoViewModel : ModuleViewModuleBase
    {
        private readonly VcoModule _vcoModel = null!;

        public double BaseFrequency
        {
            get
            {
                if (_vcoModel == null) return 0.5;
                return Math.Log(_vcoModel.BaseFrequency / 20.0) / Math.Log(2000.0 / 20.0);
            }
            set
            {
                if (_vcoModel != null)
                {
                    _vcoModel.BaseFrequency = 20.0 * Math.Pow(2000.0 / 20.0, value);
                    NotifyPropertyChanged();
                }
            }
        }

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

        public ICommand ToggleActiveCommand { get; }

        public IEnumerable<VcoWaveformDescription> AvailableWaveforms { get; } = new[]
        {
            new VcoWaveformDescription { Waveform = VcoWaveform.Sine, Name = "Sinusoïdale (Sine)" },
            new VcoWaveformDescription { Waveform = VcoWaveform.Triangle, Name = "Triangulaire (Triangle)" },
            new VcoWaveformDescription { Waveform = VcoWaveform.Sawtooth, Name = "Dent de scie (Sawtooth)" },
            new VcoWaveformDescription { Waveform = VcoWaveform.Square, Name = "Carrée (Square)" }
        };

        private VcoWaveformDescription? _selectedWaveform;
        public VcoWaveformDescription? SelectedWaveform
        {
            get => _selectedWaveform;
            set
            {
                if (_selectedWaveform != value)
                {
                    _selectedWaveform = value;
                    NotifyPropertyChanged();
                    if (_selectedWaveform != null && _vcoModel != null)
                    {
                        _vcoModel.Waveform = _selectedWaveform.Waveform;
                    }
                }
            }
        }

        public VcoViewModel(VcoModule model) : base(model)
        {
            _vcoModel = model;

            InputPorts.Add(new PortViewModelBase(_vcoModel.FrequencyInput));
            OutputPorts.Add(new PortViewModelBase(_vcoModel.AudioOutput));

            ToggleActiveCommand = new RelayCommand(_ => IsActive = !IsActive);

            if (_vcoModel != null)
            {
                _selectedWaveform = AvailableWaveforms.FirstOrDefault(w => w.Waveform == _vcoModel.Waveform)
                                    ?? AvailableWaveforms.FirstOrDefault();
            }
        }
    }
}
