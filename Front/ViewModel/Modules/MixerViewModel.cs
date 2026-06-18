using System;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    /// <summary>
    /// ViewModel that binds MixerModule variables and ports to the WPF User Interface.
    /// </summary>
    public class MixerViewModel : ModuleViewModuleBase
    {
        private readonly MixerModule _mixerModel = null!;

        public double Level1
        {
            get => _mixerModel != null ? _mixerModel.Level1 : 0.8;
            set
            {
                if (_mixerModel != null)
                {
                    _mixerModel.Level1 = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Level2
        {
            get => _mixerModel != null ? _mixerModel.Level2 : 0.8;
            set
            {
                if (_mixerModel != null)
                {
                    _mixerModel.Level2 = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Pan1
        {
            get => _mixerModel != null ? _mixerModel.Pan1 : 0.0;
            set
            {
                if (_mixerModel != null)
                {
                    _mixerModel.Pan1 = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Pan2
        {
            get => _mixerModel != null ? _mixerModel.Pan2 : 0.0;
            set
            {
                if (_mixerModel != null)
                {
                    _mixerModel.Pan2 = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsMuted1
        {
            get => _mixerModel != null && _mixerModel.IsMuted1;
            set
            {
                if (_mixerModel != null)
                {
                    _mixerModel.IsMuted1 = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsMuted2
        {
            get => _mixerModel != null && _mixerModel.IsMuted2;
            set
            {
                if (_mixerModel != null)
                {
                    _mixerModel.IsMuted2 = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double MasterLevel
        {
            get => _mixerModel != null ? _mixerModel.MasterLevel : 0.8;
            set
            {
                if (_mixerModel != null)
                {
                    _mixerModel.MasterLevel = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public MixerViewModel(MixerModule model) : base(model)
        {
            _mixerModel = model;

            // Register input jacks in collection for visual rendering
            InputPorts.Add(new PortViewModelBase(_mixerModel.Input1));
            InputPorts.Add(new PortViewModelBase(_mixerModel.Input2));

            // Register output jacks in collection for visual rendering
            OutputPorts.Add(new PortViewModelBase(_mixerModel.OutputL));
            OutputPorts.Add(new PortViewModelBase(_mixerModel.OutputR));
        }
    }
}
