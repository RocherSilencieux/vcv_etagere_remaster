using System;
using vcv_etagere_remaster.Core.Interface;
using vcv_etagere_remaster.Core.Utils;

namespace vcv_etagere_remaster.Core.Modules
{
    public class AudioOutputModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "AUDIO OUT";

        public IPort LeftInput { get; }
        public IPort RightInput { get; }

        private readonly LinearRamp _masterVolumeRamp;

        public AudioOutputModule()
        {
            LeftInput = new SimplePort(Guid.NewGuid().ToString(), "L IN", PortType.Input);
            RightInput = new SimplePort(Guid.NewGuid().ToString(), "R IN", PortType.Input);

            // Default volume is 1.0 (100%), 50ms ramp
            _masterVolumeRamp = new LinearRamp(44100, 0.05, 1.0);
        }

        public double MasterVolume
        {
            get => _masterVolumeRamp.Target;
            set => _masterVolumeRamp.Target = value;
        }

        public void Process(float sampleRate)
        {
            // Just tick the ramp to smooth the volume changes
            double currentVol = _masterVolumeRamp.Next();

            // We don't actually modify the port values here because they are Inputs.
            // The Engine will read from LeftInput.Value and RightInput.Value and multiply by the current volume.
            // Wait, we can scale them here directly so the engine doesn't have to think about volume.
            LeftInput.Value = (float)(LeftInput.Value * currentVol);
            RightInput.Value = (float)(RightInput.Value * currentVol);
        }
    }
}
