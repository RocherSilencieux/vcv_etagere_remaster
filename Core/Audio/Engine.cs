using System;
using System.Collections.Generic;
using NAudio.Wave;
using vcv_etagere_remaster.Core.Interface;
using vcv_etagere_remaster.Core.Modules;

namespace vcv_etagere_remaster.Core.Audio
{
    /// <summary>
    /// The main Audio Engine using NAudio to process the module graph.
    /// Implements ISampleProvider to feed audio to NAudio's output devices.
    /// </summary>
    public class Engine : IAudioEngine, ISampleProvider
    {
        private readonly List<IModule> _modules = new List<IModule>();
        private readonly List<Cable> _cables = new List<Cable>();
        
        // You can change to AsioOut if ASIO is required for ultra low latency
        private IWavePlayer _waveOut; 
        private int _currentDeviceNumber = -1;
        private bool _isPlaying = false;

        public WaveFormat WaveFormat { get; }

        public Engine()
        {
            // Standard stereo 44.1kHz floating point audio
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);
            _waveOut = new WaveOutEvent() { DeviceNumber = _currentDeviceNumber, DesiredLatency = 100 }; // 100ms latency for safety without ASIO
        }

        public void AddModule(IModule module)
        {
            lock (_modules)
            {
                _modules.Add(module);
                if (module is AudioOutputModule audioOut)
                {
                    audioOut.DeviceChanged += OnAudioOutputDeviceChanged;
                    if (audioOut.SelectedDeviceNumber != _currentDeviceNumber)
                    {
                        ChangeDevice(audioOut.SelectedDeviceNumber);
                    }
                }
            }
        }

        public void RemoveModule(IModule module)
        {
            lock (_modules)
            {
                _modules.Remove(module);
                if (module is AudioOutputModule audioOut)
                {
                    audioOut.DeviceChanged -= OnAudioOutputDeviceChanged;
                }
            }
        }

        private void OnAudioOutputDeviceChanged(object? sender, int deviceNumber)
        {
            ChangeDevice(deviceNumber);
        }

        public void ChangeDevice(int deviceNumber)
        {
            lock (_modules)
            {
                if (_currentDeviceNumber == deviceNumber)
                    return;

                _currentDeviceNumber = deviceNumber;

                if (_isPlaying)
                {
                    try
                    {
                        _waveOut.Stop();
                    }
                    catch { }

                    _waveOut.Dispose();

                    _waveOut = new WaveOutEvent()
                    {
                        DeviceNumber = _currentDeviceNumber,
                        DesiredLatency = 100
                    };

                    try
                    {
                        _waveOut.Init(this);
                        _waveOut.Play();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error starting WaveOut with device {deviceNumber}: {ex.Message}");
                    }
                }
                else
                {
                    _waveOut.Dispose();
                    _waveOut = new WaveOutEvent()
                    {
                        DeviceNumber = _currentDeviceNumber,
                        DesiredLatency = 100
                    };
                }
            }
        }

        public void AddCable(Cable cable)
        {
            if (cable == null || cable.Source == null || cable.Destination == null) return;

            lock (_cables)
            {
                if (_cables.Contains(cable)) return;

                _cables.Add(cable);
                cable.Source.IsConnected = true;
                cable.Destination.IsConnected = true;
            }
        }

        public void RemoveCable(Cable cable)
        {
            if (cable == null) return;

            lock (_cables)
            {
                if (_cables.Contains(cable))
                {
                    _cables.Remove(cable);
                }
                if (cable.Destination != null)
                {
                    cable.Destination.IsConnected = _cables.Any(c => c.Destination == cable.Destination);
                    if (!cable.Destination.IsConnected)
                    {
                        cable.Destination.Value = 0f;
                    }
                }
                if (cable.Source != null)
                {
                    cable.Source.IsConnected = _cables.Any(c => c.Source == cable.Source);
                }
            }
        }

        public void Start()
        {
            lock (_modules)
            {
                if (_isPlaying) return;

                try
                {
                    _waveOut.Dispose();
                    _waveOut = new WaveOutEvent()
                    {
                        DeviceNumber = _currentDeviceNumber,
                        DesiredLatency = 100
                    };
                    _waveOut.Init(this);
                    _waveOut.Play();
                    _isPlaying = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to start audio engine: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            lock (_modules)
            {
                if (!_isPlaying) return;

                try
                {
                    _waveOut.Stop();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to stop waveOut: {ex.Message}");
                }
                _isPlaying = false;
            }
        }


        /// <summary>
        /// This method is called by NAudio to request the next block of audio.
        /// </summary>
        public int Read(float[] buffer, int offset, int count)
        {
            // Process per sample frame (left + right = 1 frame for stereo)
            int channels = WaveFormat.Channels;
            int frames = count / channels;

            lock (_modules)
            {
                for (int n = 0; n < frames; n++)
                {
                    foreach (var cable in _cables)
                    {
                        if (cable.Destination != null)
                        {
                            cable.Destination.Value = 0f;
                        }
                    }
                    // 1. Process all cables (route voltages from outputs to inputs)
                    foreach (var cable in _cables)
                    {

                        cable.Process();
                    }

                    // 2. Process all modules for this single sample frame
                    foreach (var module in _modules)
                    {
                        module.Process(WaveFormat.SampleRate);
                    }

                    // 3. Collect master output.
                    // For now, we assume the first module is outputting to the master bus.
                    // In a real VCV rack, you'd have an Audio Interface module.
                    // Here we'll just sum all module outputs simply for testing, or rely on a designated master module later.
                    
                    float leftMix = 0f;
                    float rightMix = 0f;

                    // Read from the dedicated Audio Output Module
                    foreach (var module in _modules)
                    {
                        if (module is vcv_etagere_remaster.Core.Modules.AudioOutputModule audioOut)
                        {
                            leftMix += audioOut.LeftInput.Value;
                            rightMix += audioOut.RightInput.Value;
                        }
                    }
                    
                    buffer[offset + n * channels] = leftMix;     // Left
                    buffer[offset + n * channels + 1] = rightMix; // Right
                }
            }

            return count; // Always return count, meaning we infinitely generate audio
        }

        public void Dispose()
        {
            Stop();
            _waveOut?.Dispose();
        }
    }
}
