using System;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Modules
{
    public class ReverbModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "REVERB";

        public IPort LeftInput { get; }
        public IPort RightInput { get; }
        public IPort LeftOutput { get; }
        public IPort RightOutput { get; }

        private readonly CombFilter[] _leftCombs;
        private readonly CombFilter[] _rightCombs;
        private readonly AllPassFilter[] _leftAllPass;
        private readonly AllPassFilter[] _rightAllPass;

        private double _roomSize = 0.5; // feedback (0.0 to 0.95)
        private double _damp = 0.2; // damping (0.0 to 0.5)
        private double _mix = 0.5; // dry/wet (0.0 to 1.0)
        private bool _isBypassed = false;

        public double RoomSize
        {
            get => _roomSize;
            set
            {
                _roomSize = Math.Clamp(value, 0.0, 0.95);
                UpdateReverbParameters();
            }
        }

        public double Damp
        {
            get => _damp;
            set
            {
                _damp = Math.Clamp(value, 0.0, 0.5);
                UpdateReverbParameters();
            }
        }

        public double Mix
        {
            get => _mix;
            set => _mix = Math.Clamp(value, 0.0, 1.0);
        }

        public bool IsBypassed
        {
            get => _isBypassed;
            set => _isBypassed = value;
        }

        public ReverbModule()
        {
            LeftInput = new SimplePort(Guid.NewGuid().ToString(), "L IN", PortType.Input);
            RightInput = new SimplePort(Guid.NewGuid().ToString(), "R IN", PortType.Input);
            LeftOutput = new SimplePort(Guid.NewGuid().ToString(), "L OUT", PortType.Output);
            RightOutput = new SimplePort(Guid.NewGuid().ToString(), "R OUT", PortType.Output);

            // Left delay sizes
            _leftCombs = new[]
            {
                new CombFilter(1116),
                new CombFilter(1188),
                new CombFilter(1277),
                new CombFilter(1356)
            };

            _leftAllPass = new[]
            {
                new AllPassFilter(225),
                new AllPassFilter(341)
            };

            // Right delay sizes (with stereo spread offset of 23 samples)
            _rightCombs = new[]
            {
                new CombFilter(1139),
                new CombFilter(1211),
                new CombFilter(1300),
                new CombFilter(1379)
            };

            _rightAllPass = new[]
            {
                new AllPassFilter(248),
                new AllPassFilter(364)
            };

            UpdateReverbParameters();
        }

        private void UpdateReverbParameters()
        {
            float feedback = (float)_roomSize;
            float damp = (float)_damp;

            foreach (var comb in _leftCombs)
            {
                comb.Feedback = feedback;
                comb.Damp = damp;
            }

            foreach (var comb in _rightCombs)
            {
                comb.Feedback = feedback;
                comb.Damp = damp;
            }
        }

        public void Process(float sampleRate)
        {
            float inputL = LeftInput.Value;
            float inputR = RightInput.IsConnected ? RightInput.Value : inputL;

            if (_isBypassed)
            {
                LeftOutput.Value = inputL;
                RightOutput.Value = inputR;
                return;
            }

            // Process Left Channel Reverb
            float combSumL = 0f;
            foreach (var comb in _leftCombs)
            {
                combSumL += comb.Process(inputL);
            }
            // Average the parallel combs
            combSumL /= 4.0f;

            float wetL = combSumL;
            foreach (var allPass in _leftAllPass)
            {
                wetL = allPass.Process(wetL);
            }

            // Process Right Channel Reverb
            float combSumR = 0f;
            foreach (var comb in _rightCombs)
            {
                combSumR += comb.Process(inputR);
            }
            // Average the parallel combs
            combSumR /= 4.0f;

            float wetR = combSumR;
            foreach (var allPass in _rightAllPass)
            {
                wetR = allPass.Process(wetR);
            }

            // Mix Dry and Wet signals
            float dryWetMix = (float)_mix;
            LeftOutput.Value = (inputL * (1f - dryWetMix)) + (wetL * dryWetMix);
            RightOutput.Value = (inputR * (1f - dryWetMix)) + (wetR * dryWetMix);
        }
    }

    public class CombFilter
    {
        private readonly float[] _buffer;
        private int _bufferIndex;
        public float Feedback { get; set; }
        public float Damp { get; set; }
        private float _filterState;

        public CombFilter(int delaySamples)
        {
            _buffer = new float[delaySamples];
        }

        public float Process(float input)
        {
            float output = _buffer[_bufferIndex];
            _filterState = (output * (1f - Damp)) + (_filterState * Damp);
            _buffer[_bufferIndex] = input + (_filterState * Feedback);

            if (++_bufferIndex >= _buffer.Length)
            {
                _bufferIndex = 0;
            }

            return output;
        }
    }

    public class AllPassFilter
    {
        private readonly float[] _buffer;
        private int _bufferIndex;
        public float Feedback { get; set; } = 0.5f;

        public AllPassFilter(int delaySamples)
        {
            _buffer = new float[delaySamples];
        }

        public float Process(float input)
        {
            float bufOut = _buffer[_bufferIndex];
            float output = -input + bufOut;
            _buffer[_bufferIndex] = input + (bufOut * Feedback);

            if (++_bufferIndex >= _buffer.Length)
            {
                _bufferIndex = 0;
            }

            return output;
        }
    }
}
