using System;
using System.Collections.Generic;
using vcv_etagere_remaster.Core.Interface;
using vcv_etagere_remaster.Core.Utils;

namespace vcv_etagere_remaster.Core.Modules
{
    public enum VcoWaveform
    {
        Sine,
        Triangle,
        Sawtooth,
        Square
    }

    /// <summary>
    /// A Voltage Controlled Oscillator supporting Sine, Triangle, Sawtooth, and Square waves.
    /// </summary>
    public class VcoModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "VCO";

        public IPort FrequencyInput { get; }
        public IPort AudioOutput { get; }

        private double _phase = 0;
        private readonly LinearRamp _baseFrequencyRamp;
        private bool _isBypassed = false;
        private VcoWaveform _waveform = VcoWaveform.Sine;

        public bool IsBypassed
        {
            get => _isBypassed;
            set => _isBypassed = value;
        }

        public VcoWaveform Waveform
        {
            get => _waveform;
            set => _waveform = value;
        }

        public VcoModule()
        {
            FrequencyInput = new SimplePort(Guid.NewGuid().ToString(), "1V/Oct", PortType.Input);
            AudioOutput = new SimplePort(Guid.NewGuid().ToString(), "Audio Out", PortType.Output);
            
            // 50ms ramp time at 44100Hz to smooth out slider movements
            _baseFrequencyRamp = new LinearRamp(44100, 0.05, 440.0);
        }

        public double BaseFrequency
        {
            get => _baseFrequencyRamp.Target;
            set => _baseFrequencyRamp.Target = value;
        }

        public void SetBaseFrequency(double frequency)
        {
            _baseFrequencyRamp.Target = frequency;
        }

        public void Process(float sampleRate)
        {
            // Get the smoothed base frequency for this specific sample frame
            double currentBaseFreq = _baseFrequencyRamp.Next();

            // 1V/Octave calculation
            double currentFrequency = currentBaseFreq * Math.Pow(2.0, FrequencyInput.Value);

            // Calculate wave phase increment
            double phaseIncrement = (currentFrequency * 2.0 * Math.PI) / sampleRate;

            _phase += phaseIncrement;
            if (_phase >= 2.0 * Math.PI)
            {
                _phase -= 2.0 * Math.PI;
            }

            if (_isBypassed)
            {
                AudioOutput.Value = 0f;
                return;
            }

            // Normalize phase to [0, 1] range for wave shape calculations
            double t = _phase / (2.0 * Math.PI);
            float outputSample = 0f;

            switch (_waveform)
            {
                case VcoWaveform.Sine:
                    outputSample = (float)Math.Sin(_phase);
                    break;

                case VcoWaveform.Triangle:
                    if (t < 0.25)
                        outputSample = (float)(4.0 * t);
                    else if (t < 0.75)
                        outputSample = (float)(2.0 - 4.0 * t);
                    else
                        outputSample = (float)(4.0 * t - 4.0);
                    break;

                case VcoWaveform.Sawtooth:
                    outputSample = (float)(2.0 * t - 1.0);
                    break;

                case VcoWaveform.Square:
                    outputSample = t < 0.5 ? 1.0f : -1.0f;
                    break;
            }

            AudioOutput.Value = outputSample;
        }
    }
}
