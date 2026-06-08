using System;
using System.Collections.Generic;
using vcv_etagere_remaster.Core.Interface;

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
        private double _baseFrequency = 440.0; // A4

        public VcoModule()
        {
            FrequencyInput = new SimplePort(Guid.NewGuid().ToString(), "1V/Oct", PortType.Input);
            AudioOutput = new SimplePort(Guid.NewGuid().ToString(), "Sine Out", PortType.Output);
        }

        public void SetBaseFrequency(double frequency)
        {
            _baseFrequency = frequency;
        }

        public void Process(float sampleRate)
        {
            // 1V/Octave calculation (simplified)
            // If Input is 1.0V, frequency doubles. If 0.0V, it's base frequency.
            double currentFrequency = _baseFrequency * Math.Pow(2.0, FrequencyInput.Value);

            // Calculate sine wave phase increment
            double phaseIncrement = (currentFrequency * 2.0 * Math.PI) / sampleRate;

            _phase += phaseIncrement;
            if (_phase >= 2.0 * Math.PI)
            {
                _phase -= 2.0 * Math.PI;
            }

            // Output the sine value
            AudioOutput.Value = (float)Math.Sin(_phase);
        }
    }
}
