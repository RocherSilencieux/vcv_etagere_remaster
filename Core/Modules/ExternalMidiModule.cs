using System;
using System.Collections.Generic;
using NAudio.Midi;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Modules
{
    public class MidiDevice
    {
        public int DeviceNumber { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ExternalMidiModule : IModule, IDisposable
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "EXT MIDI";

        public IPort VOctOutput { get; }
        public IPort GateOutput { get; }

        private MidiIn? _midiIn;
        private int _selectedDeviceId = -1;

        public event EventHandler? DeviceChanged;

        public ExternalMidiModule()
        {
            VOctOutput = new SimplePort(Guid.NewGuid().ToString(), "V/OCT", PortType.Output);
            GateOutput = new SimplePort(Guid.NewGuid().ToString(), "GATE", PortType.Output);
        }

        public List<MidiDevice> GetAvailableDevices()
        {
            var devices = new List<MidiDevice>();
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                devices.Add(new MidiDevice { DeviceNumber = i, Name = MidiIn.DeviceInfo(i).ProductName });
            }
            return devices;
        }

        public void SelectDevice(int deviceId)
        {
            if (_selectedDeviceId == deviceId) return;

            // Stop and dispose old device
            DisposeMidiIn();

            _selectedDeviceId = deviceId;

            if (deviceId >= 0 && deviceId < MidiIn.NumberOfDevices)
            {
                try
                {
                    _midiIn = new MidiIn(deviceId);
                    _midiIn.MessageReceived += OnMidiMessageReceived;
                    _midiIn.Start();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to open MIDI IN device: {ex.Message}");
                    DisposeMidiIn();
                }
            }

            DeviceChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnMidiMessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MIDI IN: {e.MidiEvent.CommandCode} - {e.MidiEvent}");
            // Process NoteOn and NoteOff
            if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOn)
            {
                var noteOn = (NoteOnEvent)e.MidiEvent;
                if (noteOn.Velocity > 0)
                {
                    // Calculate 1V/Octave (Middle C = 60 = 0V)
                    VOctOutput.Value = (noteOn.NoteNumber - 60) / 12f;
                    GateOutput.Value = 1.0f;
                }
                else
                {
                    // Velocity 0 is effectively NoteOff
                    GateOutput.Value = 0.0f;
                }
            }
            else if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOff)
            {
                GateOutput.Value = 0.0f;
            }
        }

        public void Process(float sampleRate)
        {
            // Outputs are updated asynchronously by the MIDI In callback thread
        }

        private void DisposeMidiIn()
        {
            if (_midiIn != null)
            {
                _midiIn.Stop();
                _midiIn.Dispose();
                _midiIn = null;
            }
        }

        public void Dispose()
        {
            DisposeMidiIn();
        }
    }
}
