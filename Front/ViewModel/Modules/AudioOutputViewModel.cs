using System.Collections.ObjectModel;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class AudioOutputViewModel : ModuleViewModuleBase
    {
        private readonly AudioOutputModule _audioOutputModel;

        public double MasterVolume
        {
            get => _audioOutputModel != null ? _audioOutputModel.MasterVolume : 1.0;
            set
            {
                if (_audioOutputModel != null)
                {
                    _audioOutputModel.MasterVolume = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public AudioOutputViewModel(AudioOutputModule model) : base(model)
        {
            _audioOutputModel = model;

            InputPorts.Add(new PortViewModelBase(_audioOutputModel.LeftInput));
            InputPorts.Add(new PortViewModelBase(_audioOutputModel.RightInput));
            // Output modules generally do not have physical output ports in the rack.
        }
    }
}
