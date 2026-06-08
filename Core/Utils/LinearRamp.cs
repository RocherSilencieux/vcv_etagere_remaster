using System;

namespace vcv_etagere_remaster.Core.Utils
{
    /// <summary>
    /// A parameter smoother that interpolates between values over a set time
    /// to avoid zipper noise and audio pops when UI values jump abruptly.
    /// </summary>
    public class LinearRamp
    {
        private double _current; 
        private double _target;
        private double _increment; 
        private double _sampleRemaining; 
        private double _time; 
        private double _sampleRate; 

        public double Target 
        {
            get => _target;
            set
            {
                _target = value;
                _sampleRemaining = (_sampleRate * _time);
                
                if (_sampleRemaining > 0)
                {
                    _increment = (_target - _current) / _sampleRemaining;
                }
                else
                {
                    _current = _target;
                }
            }
        }

        public LinearRamp(double sampleRate, double timeInSeconds, double initialValue = 0)
        {
            _time = timeInSeconds;
            _sampleRate = sampleRate;
            _current = initialValue;
            _target = initialValue;
        }

        public double Next()
        {
            if (_sampleRemaining > 0)
            {
                _sampleRemaining--;
                _current += _increment;
            }
            else
            {
                _current = _target;
            }
            
            return _current;
        }
    }
}
