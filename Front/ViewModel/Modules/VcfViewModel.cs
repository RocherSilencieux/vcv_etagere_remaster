using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class VcfModeDescription
    {
        public VcfMode Mode { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class VcfViewModel : ModuleViewModuleBase
    {
        private readonly VcfModule _vcfModel = null!;

        public double BaseCutoff
        {
            get => _vcfModel != null ? _vcfModel.BaseCutoff : 1000.0;
            set
            {
                if (_vcfModel != null)
                {
                    _vcfModel.BaseCutoff = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Resonance
        {
            get => _vcfModel != null ? _vcfModel.Resonance : 1.0;
            set
            {
                if (_vcfModel != null)
                {
                    _vcfModel.Resonance = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public IEnumerable<VcfModeDescription> AvailableModes { get; } = new[]
        {
            new VcfModeDescription { Mode = VcfMode.LowPass, Name = "Low-Pass (LP)" },
            new VcfModeDescription { Mode = VcfMode.HighPass, Name = "High-Pass (HP)" },
            new VcfModeDescription { Mode = VcfMode.BandPass, Name = "Band-Pass (BP)" },
            new VcfModeDescription { Mode = VcfMode.Notch, Name = "Notch" }
        };

        private VcfModeDescription? _selectedMode;
        public VcfModeDescription? SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (_selectedMode != value)
                {
                    _selectedMode = value;
                    NotifyPropertyChanged();
                    if (_selectedMode != null && _vcfModel != null)
                    {
                        _vcfModel.Mode = _selectedMode.Mode;
                    }
                }
            }
        }

        public VcfViewModel(VcfModule model) : base(model)
        {
            _vcfModel = model;

            InputPorts.Add(new PortViewModelBase(_vcfModel.LeftInput));
            InputPorts.Add(new PortViewModelBase(_vcfModel.RightInput));
            InputPorts.Add(new PortViewModelBase(_vcfModel.CutoffCVInput));
            OutputPorts.Add(new PortViewModelBase(_vcfModel.LeftOutput));
            OutputPorts.Add(new PortViewModelBase(_vcfModel.RightOutput));

            if (_vcfModel != null)
            {
                _selectedMode = AvailableModes.FirstOrDefault(m => m.Mode == _vcfModel.Mode)
                                ?? AvailableModes.FirstOrDefault();
            }
        }
    }
}
