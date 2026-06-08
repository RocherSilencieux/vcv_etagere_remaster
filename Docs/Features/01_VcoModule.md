# Feature Decision Record: Voltage Controlled Oscillator (VCO)

## 1. Introduction and Context
The VCO (Voltage Controlled Oscillator) is the primary sound source in any subtractive synthesizer. It generates a continuously repeating waveform (Sine, Triangle, Sawtooth, or Square) at a specific frequency.
In our digital architecture, the `VcoModule` is the first concrete implementation of the `IModule` contract, serving as the proof-of-concept for our DSP loop and our MVVM UI binding.

## 2. Why is it implemented this way?
The `VcoModule` must perform mathematical calculations to generate a waveform while respecting Eurorack standards, specifically the **1V/Octave** standard for pitch control.
It must also remain decoupled from its UI counterpart, `VcoView`.

## 3. What It Uses
### In the Domain (`Core/Modules/VcoModule.cs`):
- **`System.Math`**: Used heavily for trigonometric functions (`Math.Sin`) and logarithmic pitch calculations (`Math.Pow`).
- **`IModule` and `IPort`**: Inherited from the core architecture.
- **`SimplePort`**: A concrete, lightweight implementation of `IPort` used to instantiate the inputs and outputs of the VCO.

### In the Presentation (`Front/View/Modules/` and `Front/ViewModel/Modules/`):
- **`ModuleViewModelBase`**: To wrap the domain model and handle `INotifyPropertyChanged`.
- **WPF `Slider`**: To provide a temporary visual control for the Base Frequency.
- **WPF `ItemsControl`**: To dynamically render the jacks based on the ports exposed by the ViewModel.

## 4. What Uses It
- **The User**: The user interacts with the `VcoView` to change the frequency.
- **The Audio Engine**: The `Engine` calls `VcoModule.Process()` to request the next audio sample.
- **Other Modules**: Other modules (like a VCA or a Filter) will connect to the VCO's `AudioOutput` port to process its sound.

## 5. Detailed Breakdown of the DSP (Digital Signal Processing)

### 5.1 The 1V/Octave Standard
In Eurorack hardware, pitch is controlled by voltage. An increase of 1 Volt doubles the frequency (which equates to jumping up exactly one octave musically).
In `VcoModule.cs`:
```csharp
double currentFrequency = _baseFrequency * Math.Pow(2.0, FrequencyInput.Value);
```
**Why it's there**: If the user sets the base frequency to 440Hz (A4), and a sequencer sends 1.0f (1 Volt) into the `FrequencyInput` port, the math calculates `440 * 2^1 = 880Hz` (A5). This makes our digital VCO compatible with standard modular logic.

### 5.2 Phase Accumulation
```csharp
double phaseIncrement = (currentFrequency * 2.0 * Math.PI) / sampleRate;
_phase += phaseIncrement;
if (_phase >= 2.0 * Math.PI) _phase -= 2.0 * Math.PI;
```
**Why it's there**: A digital oscillator cannot just ask the CPU for "a frequency". It must calculate the amplitude of a wave at a specific point in time. 
**How it works**: The `_phase` variable tracks where we are in the 360-degree cycle of the wave (from 0 to 2π). Based on the desired frequency and the sample rate (44100), we calculate how much the phase should advance per sample (`phaseIncrement`). We wrap it back to 0 when it exceeds 2π to prevent floating-point overflow and precision loss.

### 5.3 Waveform Generation
```csharp
AudioOutput.Value = (float)Math.Sin(_phase);
```
**Why it's there**: The mathematical `Sin` function takes a phase (0 to 2π) and outputs a value between -1.0 and 1.0. This is assigned directly to the `AudioOutput.Value` property. The `Engine` will later read this property and send it to the speakers.

## 6. The UI Binding (MVVM in Action)
The `VcoViewModel` exposes a `BaseFrequency` property. When the user drags the Slider in `VcoView.xaml`:
1. WPF updates `BaseFrequency` in the ViewModel.
2. The ViewModel calls `_vcoModel.SetBaseFrequency(value)`.
3. The Model's DSP loop instantly uses the new `_baseFrequency` on the next audio tick (less than 0.02ms later).
4. The user hears a smooth sweep in pitch without any UI thread blocking or audio glitching.
