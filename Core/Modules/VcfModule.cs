using System;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Modules
{
    public enum VcfMode
    {
        LowPass,
        HighPass,
        BandPass,
        Notch
    }

    public class VcfModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "VCF";

        // --- Ports ---
        public IPort LeftInput { get; }
        public IPort RightInput { get; }
        public IPort CutoffCVInput { get; }
        public IPort LeftOutput { get; }
        public IPort RightOutput { get; }

        // --- State variables for Andy Simper's SVF (Left & Right) ---
        private float _s1L = 0f;
        private float _s2L = 0f;
        private float _s1R = 0f;
        private float _s2R = 0f;

        // --- Parameters ---
        private double _baseCutoff = 1000.0; // Hz
        private double _resonance = 1.0; // Q (0.5 to 25.0)
        private VcfMode _mode = VcfMode.LowPass;

        public double BaseCutoff
        {
            get => _baseCutoff;
            set => _baseCutoff = Math.Clamp(value, 20.0, 20000.0);
        }

        public double Resonance
        {
            get => _resonance;
            set => _resonance = Math.Clamp(value, 0.5, 25.0);
        }

        public VcfMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public VcfModule()
        {
            LeftInput = new SimplePort(Guid.NewGuid().ToString(), "L IN", PortType.Input);
            RightInput = new SimplePort(Guid.NewGuid().ToString(), "R IN", PortType.Input);
            CutoffCVInput = new SimplePort(Guid.NewGuid().ToString(), "Cutoff CV", PortType.Input);
            LeftOutput = new SimplePort(Guid.NewGuid().ToString(), "L OUT", PortType.Output);
            RightOutput = new SimplePort(Guid.NewGuid().ToString(), "R OUT", PortType.Output);
        }

        public void Process(float sampleRate)
        {
            // Normaling Right input to Left input if not connected
            float inputL = LeftInput.Value;
            float inputR = RightInput.IsConnected ? RightInput.Value : inputL;

            // Apply 1V/Oct modulation or raw CV scaling
            double modValue = CutoffCVInput.Value;
            double currentCutoff = _baseCutoff * Math.Pow(2.0, modValue);
            currentCutoff = Math.Clamp(currentCutoff, 20.0, sampleRate * 0.49); // Keep below Nyquist for safety

            // Andy Simper's SVF implementation
            double g = Math.Tan(Math.PI * currentCutoff / sampleRate);
            double k = 1.0 / _resonance;
            double a1 = 1.0 / (1.0 + g * (g + k));
            double a2 = g * a1;
            double a3 = g * a2;

            // Process Left Channel
            double v3L = inputL - _s2L;
            double v1L = a1 * _s1L + a2 * v3L;
            double v2L = _s2L + a2 * _s1L + a3 * v3L;

            _s1L = (float)(2.0 * v1L - _s1L);
            _s2L = (float)(2.0 * v2L - _s2L);

            // Process Right Channel
            double v3R = inputR - _s2R;
            double v1R = a1 * _s1R + a2 * v3R;
            double v2R = _s2R + a2 * _s1R + a3 * v3R;

            _s1R = (float)(2.0 * v1R - _s1R);
            _s2R = (float)(2.0 * v2R - _s2R);

            // Output based on mode
            double outputL = 0.0;
            double outputR = 0.0;

            switch (_mode)
            {
                case VcfMode.LowPass:
                    outputL = v2L;
                    outputR = v2R;
                    break;
                case VcfMode.HighPass:
                    outputL = inputL - k * v1L - v2L;
                    outputR = inputR - k * v1R - v2R;
                    break;
                case VcfMode.BandPass:
                    outputL = v1L;
                    outputR = v1R;
                    break;
                case VcfMode.Notch:
                    outputL = inputL - k * v1L;
                    outputR = inputR - k * v1R;
                    break;
            }

            // Simple stability check/reset on NaN or Infinity
            if (double.IsNaN(outputL) || double.IsInfinity(outputL))
            {
                _s1L = 0f;
                _s2L = 0f;
                outputL = 0.0;
            }
            if (double.IsNaN(outputR) || double.IsInfinity(outputR))
            {
                _s1R = 0f;
                _s2R = 0f;
                outputR = 0.0;
            }

            LeftOutput.Value = (float)outputL;
            RightOutput.Value = (float)outputR;
        }
    }
}
