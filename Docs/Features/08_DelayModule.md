# Feature Decision Record: Delay Module

## 1. Introduction and Context
To enrich the sound synthesis capability and allow users to create rhythmic echoes and stereo-alternating effects, we implemented a parameterizable `DelayModule`. 
This module is positioned in the audio signal path before the reverb, allowing the user to configure the delay time (in milliseconds), feedback ratio, mix level, and preset modes (Simple Stereo, Ping-Pong, Mono). It also features a Bypass toggle.

## 2. Why implement it this way?
- **Linear-Interpolated Fractional Delay Line**: Standard digital delay lines read at integer sample offsets. When the user changes the delay time parameter via the UI slider, this causes clicking artifacts due to instantaneous jumps in the read pointer. We implemented a custom `DelayLine` buffer that performs linear interpolation between adjacent samples. This enables smooth time transitions and vintage "pitch-bending" tape delay artifacts when modifying time.
- **Cross-Channel Feedback (Ping-Pong)**: In Ping-Pong mode, the feedback loops are crossed. The echo of the left channel is fed back into the right delay buffer, and the echo of the right channel is fed back into the left delay buffer. This creates a stereo ping-pong effect where echoes bounce between left and right speakers.
- **WPF ComboBox Preset Styling**: Standard ComboBox dropdown items default to white text which contrasts poorly against light system dropdown backgrounds. We styled the items to have black text in the dropdown for optimal readability.

## 3. What It Uses
### In the Domain (`Core/Modules/DelayModule.cs`):
- **`DelayLine`**: A ring buffer supporting fractional read offsets with linear interpolation.
- **`DelayMode`**: An enum defining `Simple` (independent stereo channels), `PingPong` (crossed feedback), and `Mono` (summed input and joint delay lines).
- **`IsBypassed`**: A boolean flag to activate or bypass the delay effect.

### In the Presentation:
- **`DelayViewModel.cs`**:
  - Exposes `DelayTimeMs`, `Feedback`, `Mix`, and `IsActive` (inverted bypass).
  - Exposes `AvailablePresets` linking descriptions of simple, ping-pong, and mono modes.
  - Exposes a `SelectedPreset` property that updates the model's delay mode.
- **`DelayView.xaml`**:
  - Layout matches the modular rack standard (Height `360`, Width `160`).
  - Styled with a dark-teal header theme (`#0d2830`) to visually identify it.
  - Comprises a CheckBox, a preset ComboBox, and sliders to adjust delay time, feedback gain, and mix.

## 4. What Uses It
- **`MainViewModel.cs`**: Instantiates the `DelayModule` at startup and patches: `VcoModule` -> `DelayModule` -> `ReverbModule` -> `AudioOutputModule`.

## 5. Detailed Breakdown of the Logic

### 5.1 DSP Calculations

#### Linear Interpolation
To read a fractional sample offset $t$, we compute the integer part $i = \lfloor t \rfloor$ and the fractional part $f = t - i$:
$$x(t) = (1 - f) \cdot x(i) + f \cdot x(i + 1)$$
In code:
```csharp
public float Read(float delaySamples)
{
    float readIndex = _writeIndex - delaySamples;
    while (readIndex < 0) readIndex += _buffer.Length;
    while (readIndex >= _buffer.Length) readIndex -= _buffer.Length;

    int index1 = (int)readIndex;
    int index2 = (index1 + 1) % _buffer.Length;
    float frac = readIndex - index1;

    return _buffer[index1] * (1f - frac) + _buffer[index2] * frac;
}
```

#### Ping-Pong Delay Routing
In Ping-Pong mode, the feedback terms are swapped:
$$\text{write}_L(n) = \text{input}_L(n) + \text{delayed}_R(n - D) \cdot g$$
$$\text{write}_R(n) = \text{input}_R(n) + \text{delayed}_L(n - D) \cdot g$$
In code:
```csharp
case DelayMode.PingPong:
    writeL = inputL + delayedR * feedbackGain;
    writeR = inputR + delayedL * feedbackGain;
    break;
```
This causes any audio signal fed into the Left input to echo first on the Left output, then feed into the Right buffer, echoing next on the Right output, then feed back to Left, alternating infinitely.
