# Feature Decision Record: Reverb Module

## 1. Introduction and Context
A synthesizer voice without spatial effects can sound very dry and digital. To add stereo depth and ambient space to the modular engine, we implemented a virtual hardware `ReverbModule`. 
This module is positioned in the audio signal path, allowing the user to toggle it on or off (Bypass/Active) and control parameters like wet/dry Mix, Decay time (feedback), and high-frequency Damping.

## 2. Why implement it this way?
- **Schroeder Reverb Design**: We implemented a classic Schroeder Reverb model (similar to Freeverb). It uses 4 parallel Comb Filters followed by 2 series All-Pass Filters per channel. This mimics sound wave reflections off room walls, dispersing the sound to build up density over time.
- **True Stereo Spread**: To prevent a flat mono reverb, the right channel uses slightly larger delay buffers (comb and all-pass delay times offset by a stereo spread factor of 23 samples). This creates phase discrepancies between the left and right speakers, producing a wide and realistic stereo image.
- **Bypass Toggle**: The module includes a bypass switch that lets the user route dry inputs straight to outputs.

## 3. What It Uses
### In the Domain (`Core/Modules/ReverbModule.cs`):
- **`CombFilter`**: A feedback delay line combined with a low-pass filter (damping) to simulate wall surface high-frequency absorption.
- **`AllPassFilter`**: A delay line that passes all frequencies equally but shifts their phase, used to increase the echo density.
- **`LeftInput`/`RightInput` and `LeftOutput`/`RightOutput`**: Stereo input and output ports to process dual-channel signals.
- **`IsBypassed`**: A boolean flag to activate or deactivate the DSP processing.

### In the Presentation:
- **`ReverbViewModel.cs`**:
  - Exposes `RoomSize` (Decay), `Damp`, `Mix`, and `IsActive` (inverted `IsBypassed`) to bind controls to the UI.
- **`ReverbView.xaml`**:
  - Layout matches the modular rack standard (Height `360`, Width `160`).
  - Styled with a dark-violet header theme (`#2e113d`) to visually stand out.
  - Comprises a CheckBox for toggling the reverb state, plus three horizontal sliders to tweak Room Size, Damping, and Mix.

## 4. What Uses It
- **`MainViewModel.cs`**: Instantiates the `ReverbModule` at startup and patches the modules in a serial configuration: `VcoModule` -> `ReverbModule` -> `AudioOutputModule`.

## 5. Detailed Breakdown of the Logic

### 5.1 Filter Mathematics

#### Comb Filter
The output of a comb filter $y(n)$ is a combination of the delayed output fed back through a gain factor $g$ (feedback/room size) and low-pass filtered (damped) by a factor $d$:
$$y(n) = x(n - D) + g \cdot y_{filtered}(n - D)$$
where:
$$y_{filtered}(n) = (1 - d) \cdot y(n) + d \cdot y_{filtered}(n - 1)$$
In code:
```csharp
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
```

#### All-Pass Filter
The all-pass filter shifts phase while keeping a flat frequency response. The output $y(n)$ is defined as:
$$y(n) = -g \cdot x(n) + x(n - D) + g \cdot y(n - D)$$
In code:
```csharp
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
```

### 5.2 Stereo Processing in `Process()`
```csharp
public void Process(float sampleRate)
{
    float inputL = LeftInput.Value;
    float inputR = RightInput.Value;

    if (_isBypassed)
    {
        LeftOutput.Value = inputL;
        RightOutput.Value = inputR;
        return;
    }

    // Process Left Channel through 4 parallel combs and 2 series all-passes
    float combSumL = 0f;
    foreach (var comb in _leftCombs)
    {
        combSumL += comb.Process(inputL);
    }
    combSumL /= 4.0f;

    float wetL = combSumL;
    foreach (var allPass in _leftAllPass)
    {
        wetL = allPass.Process(wetL);
    }

    // Process Right Channel (combs have 23-sample offsets for stereo width)
    float combSumR = 0f;
    foreach (var comb in _rightCombs)
    {
        combSumR += comb.Process(inputR);
    }
    combSumR /= 4.0f;

    float wetR = combSumR;
    foreach (var allPass in _rightAllPass)
    {
        wetR = allPass.Process(wetR);
    }

    // Mix Dry/Wet
    float dryWetMix = (float)_mix;
    LeftOutput.Value = (inputL * (1f - dryWetMix)) + (wetL * dryWetMix);
    RightOutput.Value = (inputR * (1f - dryWetMix)) + (wetR * dryWetMix);
}
```
**Why it's there**: This code implements the actual DSP routing. By calculating separate left and right reflections, it prevents mono-collapsing and provides a lush, deep acoustic space.
