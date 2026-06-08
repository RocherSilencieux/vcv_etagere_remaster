# Feature Decision Record: Linear Ramp Parameter Smoother

## 1. Introduction and Context
When a user interacts with a UI element (like a slider knob) in a digital synthesizer, the value jumps abruptly from one number to another. If this raw value is fed directly into a DSP (Digital Signal Processing) loop—such as the frequency of an oscillator—it causes an instantaneous discontinuity in the waveform. 
To the human ear, these discontinuities sound like harsh "clicks," "pops," or "zipper noise."
To solve this, we implemented the `LinearRamp` utility class.

## 2. Why implement it this way?
The standard solution in audio programming is "parameter smoothing." Instead of jumping instantly to a new target value, the system smoothly interpolates from the current value to the target value over a set amount of time (e.g., 50 milliseconds).
The `LinearRamp` class handles this math efficiently per-sample, ensuring that any abrupt change is transformed into a clean, smooth curve.

## 3. What It Uses
The `LinearRamp` class (`Core/Utils/LinearRamp.cs`) uses purely mathematical primitives (`double`).
- **`_current`**: The actual value being used right now in the audio loop.
- **`_target`**: The destination value requested by the UI.
- **`_increment`**: The step size per sample needed to reach the target exactly when the time is up.
- **`_sampleRemaining`**: A counter to know when interpolation is complete.

## 4. What Uses It
- **`Core/Modules/VcoModule.cs`**: The VCO uses it to smooth out its `BaseFrequency`.

## 5. Detailed Breakdown of the Logic

### 5.1 Setting the Target
```csharp
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
    }
}
```
**Why it's there**: When the UI slider moves, it sets `Target`. The class immediately calculates how many samples it will take to cross the designated time (e.g., 44100 Hz * 0.05 seconds = 2205 samples). It then calculates the `_increment` required per sample to arrive precisely at the target.

### 5.2 The DSP Loop (`Next()`)
```csharp
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
```
**Why it's there**: This method is called 44,100 times per second per module. It must be highly optimized. It simply adds the `_increment` to the `_current` value until `_sampleRemaining` hits zero. At that point, it locks to the `_target` to prevent floating-point drift.

## 6. Architectural Benefits
By encapsulating this into a generic `LinearRamp` class inside `Core.Utils`, we can reuse this exact same parameter smoother for *any* DSP parameter in the future:
- VCA Volume controls
- Filter Cutoff frequencies
- ADSR envelope amounts
This keeps the `VcoModule` code incredibly clean, as the VCO doesn't have to manage the interpolation math itself. It just calls `.Next()`.
