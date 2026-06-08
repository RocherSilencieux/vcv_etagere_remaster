using System;
using System.Collections.Generic;
using NAudio.Wave;
using vcv_etagere_remaster.Core.Interface;
using vcv_etagere_remaster.Core.Utils;

namespace vcv_etagere_remaster.Core.Modules
{
    public class AudioDevice
    {
        public int DeviceNumber { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AudioOutputModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "AUDIO OUT";

        public IPort LeftInput { get; }
        public IPort RightInput { get; }

        private readonly LinearRamp _masterVolumeRamp;

        public event EventHandler<int>? DeviceChanged;

        private int _selectedDeviceNumber = -1;
        public int SelectedDeviceNumber
        {
            get => _selectedDeviceNumber;
            set
            {
                if (_selectedDeviceNumber != value)
                {
                    _selectedDeviceNumber = value;
                    DeviceChanged?.Invoke(this, value);
                }
            }
        }

        public List<AudioDevice> AvailableDevices { get; } = new List<AudioDevice>();

        public AudioOutputModule()
        {
            LeftInput = new SimplePort(Guid.NewGuid().ToString(), "L IN", PortType.Input);
            RightInput = new SimplePort(Guid.NewGuid().ToString(), "R IN", PortType.Input);

            // Default volume is 1.0 (100%), 50ms ramp
            _masterVolumeRamp = new LinearRamp(44100, 0.05, 1.0);

            RefreshDevices();
        }

        public double MasterVolume
        {
            get => _masterVolumeRamp.Target;
            set => _masterVolumeRamp.Target = value;
        }

        public void RefreshDevices()
        {
            AvailableDevices.Clear();
            // Default system audio mapper
            AvailableDevices.Add(new AudioDevice { DeviceNumber = -1, Name = "Périphérique par défaut" });

            try
            {
                int deviceCount = WaveOut.DeviceCount;
                for (int i = 0; i < deviceCount; i++)
                {
                    try
                    {
                        var capabilities = WaveOut.GetCapabilities(i);
                        AvailableDevices.Add(new AudioDevice { DeviceNumber = i, Name = capabilities.ProductName });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error getting capabilities for device {i}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error querying WaveOut devices: {ex.Message}");
            }
        }

        public void Process(float sampleRate)
        {
            // Just tick the ramp to smooth the volume changes
            double currentVol = _masterVolumeRamp.Next();

            // We don't actually modify the port values here because they are Inputs.
            // The Engine will read from LeftInput.Value and RightInput.Value and multiply by the current volume.
            // We can scale them here directly so the engine doesn't have to think about volume.
            LeftInput.Value = (float)(LeftInput.Value * currentVol);
            RightInput.Value = (float)(RightInput.Value * currentVol);
        }
    }
}

