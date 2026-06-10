using System;
using vcv_etagere_remaster.Core.Interface;
using vcv_etagere_remaster.Core.Utils;

namespace vcv_etagere_remaster.Core.Modules
{
    public enum LfoWaveform
    {
        Sine,
        Triangle,
        Sawtooth,
        Square
    }

    public class LfoModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "LFO";

        // --- Ports ---
        public IPort FMInput { get; }
        public IPort LfoOutput { get; }

        // --- Parameters ---
        private double _phase = 0;
        private readonly LinearRamp _frequencyRamp;
        private double _amplitude = 1.0; // Depth (0.0 to 1.0)
        private LfoWaveform _waveform = LfoWaveform.Sine;

        public double Frequency
        {
            get => _frequencyRamp.Target;
            set => _frequencyRamp.Target = Math.Clamp(value, 0.01, 50.0);
        }

        public double Amplitude
        {
            get => _amplitude;
            set => _amplitude = Math.Clamp(value, 0.0, 1.0);
        }

        public LfoWaveform Waveform
        {
            get => _waveform;
            set => _waveform = value;
        }

        public LfoModule()
        {
            FMInput = new SimplePort(Guid.NewGuid().ToString(), "FM IN", PortType.Input);
            LfoOutput = new SimplePort(Guid.NewGuid().ToString(), "CV OUT", PortType.Output);

            // Smooth frequency transitions
            _frequencyRamp = new LinearRamp(44100, 0.05, 2.0); // Default to 2.0 Hz
        }

        public void Process(float sampleRate)
        {
            // Read smoothed base frequency
            double baseFreq = _frequencyRamp.Next();

            // Frequency Modulation calculation (1V/Oct or raw scaling)
            double modValue = FMInput.Value;
            double currentFrequency = baseFreq * Math.Pow(2.0, modValue);
            currentFrequency = Math.Clamp(currentFrequency, 0.01, sampleRate * 0.49);

            // Phase accumulation
            double phaseIncrement = (currentFrequency * 2.0 * Math.PI) / sampleRate;
            _phase += phaseIncrement;
            if (_phase >= 2.0 * Math.PI)
            {
                _phase -= 2.0 * Math.PI;
            }

            // Waveform calculations
            double t = _phase / (2.0 * Math.PI);
            double outputSample = 0.0;

            switch (_waveform)
            {
                case LfoWaveform.Sine:
                    outputSample = Math.Sin(_phase);
                    break;

                case LfoWaveform.Triangle:
                    if (t < 0.25)
                        outputSample = 4.0 * t;
                    else if (t < 0.75)
                        outputSample = 2.0 - 4.0 * t;
                    else
                        outputSample = 4.0 * t - 4.0;
                    break;

                case LfoWaveform.Sawtooth:
                    outputSample = 2.0 * t - 1.0;
                    break;

                case LfoWaveform.Square:
                    outputSample = t < 0.5 ? 1.0 : -1.0;
                    break;
            }

            // Apply amplitude
            LfoOutput.Value = (float)(outputSample * _amplitude);
        }
    }
}
