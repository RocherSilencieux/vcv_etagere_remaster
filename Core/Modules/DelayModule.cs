using System;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Modules
{
    public enum DelayMode
    {
        Simple,
        PingPong,
        Mono
    }

    public class DelayModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "DELAY";

        public IPort LeftInput { get; }
        public IPort RightInput { get; }
        public IPort LeftOutput { get; }
        public IPort RightOutput { get; }

        private readonly DelayLine _leftDelay;
        private readonly DelayLine _rightDelay;

        private double _delayTimeMs = 500.0;
        private double _feedback = 0.5;
        private double _mix = 0.5;
        private DelayMode _mode = DelayMode.Simple;
        private bool _isBypassed = false;

        public double DelayTimeMs
        {
            get => _delayTimeMs;
            set => _delayTimeMs = Math.Clamp(value, 10.0, 2000.0);
        }

        public double Feedback
        {
            get => _feedback;
            set => _feedback = Math.Clamp(value, 0.0, 1.0);
        }

        public double Mix
        {
            get => _mix;
            set => _mix = Math.Clamp(value, 0.0, 1.0);
        }

        public DelayMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public bool IsBypassed
        {
            get => _isBypassed;
            set => _isBypassed = value;
        }

        public DelayModule()
        {
            LeftInput = new SimplePort(Guid.NewGuid().ToString(), "L IN", PortType.Input);
            RightInput = new SimplePort(Guid.NewGuid().ToString(), "R IN", PortType.Input);
            LeftOutput = new SimplePort(Guid.NewGuid().ToString(), "L OUT", PortType.Output);
            RightOutput = new SimplePort(Guid.NewGuid().ToString(), "R OUT", PortType.Output);

            // 192000 samples supports up to 2 seconds of delay even at 96kHz sample rates
            _leftDelay = new DelayLine(192000);
            _rightDelay = new DelayLine(192000);
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

            // Calculate delay samples based on current sample rate
            float delaySamples = (float)(_delayTimeMs * sampleRate / 1000.0);

            // Read delayed signals from delay lines using linear interpolation
            float delayedL = _leftDelay.Read(delaySamples);
            float delayedR = _rightDelay.Read(delaySamples);

            float feedbackGain = (float)_feedback;
            float writeL = 0f;
            float writeR = 0f;

            switch (_mode)
            {
                case DelayMode.Simple:
                    writeL = inputL + delayedL * feedbackGain;
                    writeR = inputR + delayedR * feedbackGain;
                    break;

                case DelayMode.PingPong:
                    // Cross feedback
                    writeL = inputL + delayedR * feedbackGain;
                    writeR = inputR + delayedL * feedbackGain;
                    break;

                case DelayMode.Mono:
                    // Sum inputs to mono and write the same to both delay lines
                    float monoInput = (inputL + inputR) * 0.5f;
                    float monoDelay = (delayedL + delayedR) * 0.5f;
                    writeL = monoInput + monoDelay * feedbackGain;
                    writeR = writeL;
                    break;
            }

            // Write into delay buffers
            _leftDelay.Write(writeL);
            _rightDelay.Write(writeR);

            // Mix dry and wet signals
            float dryWetMix = (float)_mix;
            LeftOutput.Value = (inputL * (1f - dryWetMix)) + (delayedL * dryWetMix);
            RightOutput.Value = (inputR * (1f - dryWetMix)) + (delayedR * dryWetMix);
        }
    }

    public class DelayLine
    {
        private readonly float[] _buffer;
        private int _writeIndex;

        public DelayLine(int maxSamples)
        {
            _buffer = new float[maxSamples];
        }

        public void Write(float value)
        {
            _buffer[_writeIndex] = value;
            _writeIndex = (_writeIndex + 1) % _buffer.Length;
        }

        public float Read(float delaySamples)
        {
            // Calculate read index with fractional part
            float readIndex = _writeIndex - delaySamples;
            while (readIndex < 0) readIndex += _buffer.Length;
            while (readIndex >= _buffer.Length) readIndex -= _buffer.Length;

            int index1 = (int)readIndex;
            int index2 = (index1 + 1) % _buffer.Length;
            float frac = readIndex - index1;

            return _buffer[index1] * (1f - frac) + _buffer[index2] * frac;
        }
    }
}
