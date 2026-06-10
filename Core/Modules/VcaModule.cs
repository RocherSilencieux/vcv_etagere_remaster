using System;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Modules
{
    public class VcaModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "VCA";

        public IPort LeftInput { get; }
        public IPort RightInput { get; }
        public IPort CvInput { get; }
        public IPort LeftOutput { get; }
        public IPort RightOutput { get; }

        private double _baseGain = 1.0; // range 0.0 to 1.0

        public double BaseGain
        {
            get => _baseGain;
            set => _baseGain = Math.Clamp(value, 0.0, 1.0);
        }

        public VcaModule()
        {
            LeftInput = new SimplePort(Guid.NewGuid().ToString(), "L IN", PortType.Input);
            RightInput = new SimplePort(Guid.NewGuid().ToString(), "R IN", PortType.Input);
            CvInput = new SimplePort(Guid.NewGuid().ToString(), "CV IN", PortType.Input);
            LeftOutput = new SimplePort(Guid.NewGuid().ToString(), "L OUT", PortType.Output);
            RightOutput = new SimplePort(Guid.NewGuid().ToString(), "R OUT", PortType.Output);
        }

        public void Process(float sampleRate)
        {
            float inputL = LeftInput.Value;
            float inputR = RightInput.IsConnected ? RightInput.Value : inputL;

            double currentGain = _baseGain;

            // If CV input is connected, it scales the gain (acting as an attenuator/multiplier)
            if (CvInput.IsConnected)
            {
                currentGain = CvInput.Value * _baseGain;
            }

            // Clamp gain to safe audio levels
            currentGain = Math.Clamp(currentGain, 0.0, 2.0);

            LeftOutput.Value = (float)(inputL * currentGain);
            RightOutput.Value = (float)(inputR * currentGain);
        }
    }
}
