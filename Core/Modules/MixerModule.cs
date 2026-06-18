using System;
using vcv_etagere_remaster.Core.Interface;
using vcv_etagere_remaster.Core.Utils;

namespace vcv_etagere_remaster.Core.Modules
{
    /// <summary>
    /// A 2-Channel audio mixer module that sums two mono input signals
    /// into a stereo output pair with independent gain, pan, mute controls,
    /// and master output volume.
    /// </summary>
    public class MixerModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "MIXER";

        // Input ports for audio sources
        public IPort Input1 { get; }
        public IPort Input2 { get; }

        // Stereo output ports
        public IPort OutputL { get; }
        public IPort OutputR { get; }

        // Parameter smoothers to prevent clicks/zipper noise
        private readonly LinearRamp _level1Ramp;
        private readonly LinearRamp _level2Ramp;
        private readonly LinearRamp _pan1Ramp;
        private readonly LinearRamp _pan2Ramp;
        private readonly LinearRamp _masterLevelRamp;

        // Private variables storing user parameter settings
        private double _level1 = 0.8;
        private double _level2 = 0.8;
        private double _pan1 = 0.0;
        private double _pan2 = 0.0;
        private bool _isMuted1 = false;
        private bool _isMuted2 = false;
        private double _masterLevel = 0.8;

        public double Level1
        {
            get => _level1;
            set
            {
                _level1 = Math.Clamp(value, 0.0, 1.5);
                _level1Ramp.Target = _isMuted1 ? 0.0 : _level1;
            }
        }

        public double Level2
        {
            get => _level2;
            set
            {
                _level2 = Math.Clamp(value, 0.0, 1.5);
                _level2Ramp.Target = _isMuted2 ? 0.0 : _level2;
            }
        }

        public double Pan1
        {
            get => _pan1;
            set
            {
                _pan1 = Math.Clamp(value, -1.0, 1.0);
                _pan1Ramp.Target = _pan1;
            }
        }

        public double Pan2
        {
            get => _pan2;
            set
            {
                _pan2 = Math.Clamp(value, -1.0, 1.0);
                _pan2Ramp.Target = _pan2;
            }
        }

        public bool IsMuted1
        {
            get => _isMuted1;
            set
            {
                _isMuted1 = value;
                _level1Ramp.Target = _isMuted1 ? 0.0 : _level1;
            }
        }

        public bool IsMuted2
        {
            get => _isMuted2;
            set
            {
                _isMuted2 = value;
                _level2Ramp.Target = _isMuted2 ? 0.0 : _level2;
            }
        }

        public double MasterLevel
        {
            get => _masterLevel;
            set
            {
                _masterLevel = Math.Clamp(value, 0.0, 1.5);
                _masterLevelRamp.Target = _masterLevel;
            }
        }

        public MixerModule()
        {
            Input1 = new SimplePort(Guid.NewGuid().ToString(), "IN 1", PortType.Input);
            Input2 = new SimplePort(Guid.NewGuid().ToString(), "IN 2", PortType.Input);
            OutputL = new SimplePort(Guid.NewGuid().ToString(), "OUT L", PortType.Output);
            OutputR = new SimplePort(Guid.NewGuid().ToString(), "OUT R", PortType.Output);

            // Initializing ramps with 50ms transition time at 44.1kHz sample rate
            _level1Ramp = new LinearRamp(44100, 0.05, _level1);
            _level2Ramp = new LinearRamp(44100, 0.05, _level2);
            _pan1Ramp = new LinearRamp(44100, 0.05, _pan1);
            _pan2Ramp = new LinearRamp(44100, 0.05, _pan2);
            _masterLevelRamp = new LinearRamp(44100, 0.05, _masterLevel);
        }

        public void Process(float sampleRate)
        {
            // Read input signal levels (using 0.0f if the port is not patched/connected)
            float sample1 = Input1.Value;
            float sample2 = Input2.Value;

            // Step smoothers forward by 1 sample frame
            double level1 = _level1Ramp.Next();
            double level2 = _level2Ramp.Next();
            double pan1 = _pan1Ramp.Next();
            double pan2 = _pan2Ramp.Next();
            double master = _masterLevelRamp.Next();

            // Calculate constant-power panning coefficients for Channel 1
            double angle1 = (pan1 + 1.0) * Math.PI / 4.0;
            double pan1L = Math.Cos(angle1);
            double pan1R = Math.Sin(angle1);

            // Calculate constant-power panning coefficients for Channel 2
            double angle2 = (pan2 + 1.0) * Math.PI / 4.0;
            double pan2L = Math.Cos(angle2);
            double pan2R = Math.Sin(angle2);

            // Sum mixed signals for Left and Right channels
            double mixedL = (sample1 * level1 * pan1L) + (sample2 * level2 * pan2L);
            double mixedR = (sample1 * level1 * pan1R) + (sample2 * level2 * pan2R);

            // Output the final master-scaled signal
            OutputL.Value = (float)(mixedL * master);
            OutputR.Value = (float)(mixedR * master);
        }
    }
}
