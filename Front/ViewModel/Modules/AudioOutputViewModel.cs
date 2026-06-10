using System.Collections.ObjectModel;
using System.Linq;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class AudioOutputViewModel : ModuleViewModuleBase
    {
        private readonly AudioOutputModule _audioOutputModel = null!;

        public double MasterVolume
        {
            get => _audioOutputModel != null ? _audioOutputModel.MasterVolume * 4.0 : 0.4;
            set
            {
                if (_audioOutputModel != null)
                {
                    _audioOutputModel.MasterVolume = value * 0.25;
                    NotifyPropertyChanged();
                }
            }
        }

        public ObservableCollection<AudioDevice> AvailableDevices { get; } = new ObservableCollection<AudioDevice>();

        private AudioDevice? _selectedDevice;
        public AudioDevice? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    NotifyPropertyChanged();
                    if (_selectedDevice != null && _audioOutputModel != null)
                    {
                        _audioOutputModel.SelectedDeviceNumber = _selectedDevice.DeviceNumber;
                    }
                }
            }
        }

        public AudioOutputViewModel(AudioOutputModule model) : base(model)
        {
            _audioOutputModel = model;

            InputPorts.Add(new PortViewModelBase(_audioOutputModel.LeftInput));
            InputPorts.Add(new PortViewModelBase(_audioOutputModel.RightInput));
            // Output modules generally do not have physical output ports in the rack.

            if (_audioOutputModel != null)
            {
                foreach (var device in _audioOutputModel.AvailableDevices)
                {
                    AvailableDevices.Add(device);
                }

                _selectedDevice = AvailableDevices.FirstOrDefault(d => d.DeviceNumber == _audioOutputModel.SelectedDeviceNumber)
                                  ?? AvailableDevices.FirstOrDefault();
            }
        }
    }
}

