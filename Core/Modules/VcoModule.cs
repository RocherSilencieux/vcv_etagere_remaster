using System;
using System.Collections.Generic;
using vcv_etagere_remaster.Core.Interface;
using vcv_etagere_remaster.Core.Utils;

namespace vcv_etagere_remaster.Core.Modules
{
    public class SimplePort : IPort
    {
        public string Id { get; }
        public string Name { get; }
        public PortType Type { get; }
        public float Value { get; set; }
        public bool IsConnected { get; set; } // Simplified for now

        public SimplePort(string id, string name, PortType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// A simple Voltage Controlled Oscillator outputting a Sine wave.
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

        public bool IsBypassed
        {
            get => _isBypassed;
            set => _isBypassed = value;
        }

        public VcoModule()
        {
            FrequencyInput = new SimplePort(Guid.NewGuid().ToString(), "1V/Oct", PortType.Input);
            AudioOutput = new SimplePort(Guid.NewGuid().ToString(), "Sine Out", PortType.Output);
            
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

            // Calculate sine wave phase increment
            double phaseIncrement = (currentFrequency * 2.0 * Math.PI) / sampleRate;

            _phase += phaseIncrement;
            if (_phase >= 2.0 * Math.PI)
            {
                _phase -= 2.0 * Math.PI;
            }

            // Output the sine value (muted if bypassed)
            AudioOutput.Value = _isBypassed ? 0f : (float)Math.Sin(_phase);
        }
    }
}
