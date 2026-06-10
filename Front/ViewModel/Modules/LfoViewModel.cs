using System;
using System.Collections.Generic;
using System.Linq;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class LfoWaveformDescription
    {
        public LfoWaveform Waveform { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class LfoViewModel : ModuleViewModuleBase
    {
        private readonly LfoModule _lfoModel = null!;

        public double Frequency
        {
            get => _lfoModel != null ? _lfoModel.Frequency : 2.0;
            set
            {
                if (_lfoModel != null)
                {
                    _lfoModel.Frequency = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Amplitude
        {
            get => _lfoModel != null ? _lfoModel.Amplitude : 1.0;
            set
            {
                if (_lfoModel != null)
                {
                    _lfoModel.Amplitude = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public IEnumerable<LfoWaveformDescription> AvailableWaveforms { get; } = new[]
        {
            new LfoWaveformDescription { Waveform = LfoWaveform.Sine, Name = "Sinusoïdale (Sine)" },
            new LfoWaveformDescription { Waveform = LfoWaveform.Triangle, Name = "Triangulaire (Triangle)" },
            new LfoWaveformDescription { Waveform = LfoWaveform.Sawtooth, Name = "Dent de scie (Sawtooth)" },
            new LfoWaveformDescription { Waveform = LfoWaveform.Square, Name = "Carrée (Square)" }
        };

        private LfoWaveformDescription? _selectedWaveform;
        public LfoWaveformDescription? SelectedWaveform
        {
            get => _selectedWaveform;
            set
            {
                if (_selectedWaveform != value)
                {
                    _selectedWaveform = value;
                    NotifyPropertyChanged();
                    if (_selectedWaveform != null && _lfoModel != null)
                    {
                        _lfoModel.Waveform = _selectedWaveform.Waveform;
                    }
                }
            }
        }

        public LfoViewModel(LfoModule model) : base(model)
        {
            _lfoModel = model;

            InputPorts.Add(new PortViewModelBase(_lfoModel.FMInput));
            OutputPorts.Add(new PortViewModelBase(_lfoModel.LfoOutput));

            if (_lfoModel != null)
            {
                _selectedWaveform = AvailableWaveforms.FirstOrDefault(w => w.Waveform == _lfoModel.Waveform)
                                    ?? AvailableWaveforms.FirstOrDefault();
            }
        }
    }
}
