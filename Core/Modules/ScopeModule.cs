using System;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Modules
{
    public class ScopeModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "SCOPE";

        public IPort LeftInput { get; }
        public IPort RightInput { get; }

        private const int BufferSize = 2048;
        private readonly float[] _leftBuffer = new float[BufferSize];
        private readonly float[] _rightBuffer = new float[BufferSize];
        private int _writeIndex = 0;
        private readonly object _lock = new object();

        public ScopeModule()
        {
            LeftInput = new SimplePort(Guid.NewGuid().ToString(), "L IN", PortType.Input);
            RightInput = new SimplePort(Guid.NewGuid().ToString(), "R IN", PortType.Input);
        }

        public void Process(float sampleRate)
        {
            float lVal = LeftInput.Value;
            float rVal = RightInput.IsConnected ? RightInput.Value : lVal;

            lock (_lock)
            {
                _leftBuffer[_writeIndex] = lVal;
                _rightBuffer[_writeIndex] = rVal;
                _writeIndex = (_writeIndex + 1) % BufferSize;
            }
        }

        public void GetBufferSnapshot(float[] outLeft, float[] outRight)
        {
            lock (_lock)
            {
                int len = Math.Min(BufferSize, outLeft.Length);
                int start = (_writeIndex - len + BufferSize) % BufferSize;

                for (int i = 0; i < len; i++)
                {
                    int idx = (start + i) % BufferSize;
                    outLeft[i] = _leftBuffer[idx];
                    outRight[i] = _rightBuffer[idx];
                }
            }
        }
    }
}
