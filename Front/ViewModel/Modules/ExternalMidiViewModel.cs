using System;
using System.Collections.ObjectModel;
using System.Linq;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class ExternalMidiViewModel : ModuleViewModuleBase, IDisposable
    {
        private readonly ExternalMidiModule _externalMidiModel;

        public ObservableCollection<MidiDevice> AvailableDevices { get; } = new ObservableCollection<MidiDevice>();

        private MidiDevice? _selectedDevice;
        public MidiDevice? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice != value)
                {
                    _selectedDevice = value;
                    NotifyPropertyChanged();
                    if (_selectedDevice != null)
                    {
                        _externalMidiModel.SelectDevice(_selectedDevice.DeviceNumber);
                    }
                }
            }
        }

        public ExternalMidiViewModel(ExternalMidiModule model) : base(model)
        {
            _externalMidiModel = model;

            OutputPorts.Add(new PortViewModelBase(_externalMidiModel.VOctOutput));
            OutputPorts.Add(new PortViewModelBase(_externalMidiModel.GateOutput));

            RefreshDevices();
        }

        private void RefreshDevices()
        {
            AvailableDevices.Clear();
            var devices = _externalMidiModel.GetAvailableDevices();
            foreach (var device in devices)
            {
                AvailableDevices.Add(device);
            }

            SelectedDevice = AvailableDevices.FirstOrDefault();
        }

        public void Dispose()
        {
            _externalMidiModel.Dispose();
        }
    }
}
