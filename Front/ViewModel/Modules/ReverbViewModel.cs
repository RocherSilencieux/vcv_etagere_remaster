using System.Collections.ObjectModel;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class ReverbViewModel : ModuleViewModuleBase
    {
        private readonly ReverbModule _reverbModel = null!;

        public double RoomSize
        {
            get => _reverbModel != null ? _reverbModel.RoomSize : 0.5;
            set
            {
                if (_reverbModel != null)
                {
                    _reverbModel.RoomSize = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Damp
        {
            get => _reverbModel != null ? _reverbModel.Damp : 0.2;
            set
            {
                if (_reverbModel != null)
                {
                    _reverbModel.Damp = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public double Mix
        {
            get => _reverbModel != null ? _reverbModel.Mix : 0.5;
            set
            {
                if (_reverbModel != null)
                {
                    _reverbModel.Mix = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsBypassed
        {
            get => _reverbModel != null ? _reverbModel.IsBypassed : false;
            set
            {
                if (_reverbModel != null)
                {
                    _reverbModel.IsBypassed = value;
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

        public ReverbViewModel(ReverbModule model) : base(model)
        {
            _reverbModel = model;

            InputPorts.Add(new PortViewModelBase(_reverbModel.LeftInput));
            InputPorts.Add(new PortViewModelBase(_reverbModel.RightInput));
            OutputPorts.Add(new PortViewModelBase(_reverbModel.LeftOutput));
            OutputPorts.Add(new PortViewModelBase(_reverbModel.RightOutput));
        }
    }
}
