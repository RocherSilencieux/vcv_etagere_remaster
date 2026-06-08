# Feature Decision Record: Audio Output Module

## 1. Introduction and Context
In early prototyping, the audio engine (`Engine.cs`) was hardcoded to search for a `VcoModule` and pipe its audio directly to the speaker buffers. While acceptable for a quick test, this violates the principles of a modular synthesizer. 
In a real modular environment, sound only reaches the speakers if a physical cable is plugged into a master output module. We introduced the `AudioOutputModule` to fulfill this role.

## 2. Why implement it this way?
By creating a dedicated `AudioOutputModule`:
- **Decoupling**: The NAudio `Engine` no longer cares what modules exist in the graph. It simply iterates through the modules, finds any `AudioOutputModule`, and reads its input ports. 
- **User Control**: The user gains a master volume fader before the signal leaves the application, preventing clipping and digital distortion.
- **Physical Representation**: It provides a concrete destination for audio cables, reinforcing the "virtual hardware" metaphor.

## 3. What It Uses
### In the Domain (`Core/Modules/AudioOutputModule.cs`):
- **`IModule` and `IPort`**: It implements the standard contracts.
- **`LinearRamp`**: It uses our DSP utility to smooth out adjustments to the Master Volume slider, preventing zipper noise.
- **`LeftInput` and `RightInput`**: Two standard input ports to receive stereo audio.

### In the Presentation (`Front/View/Modules/AudioOutputView.xaml`):
- **WPF DataTemplates**: Handled by `MainWindow.xaml` to render the UI dynamically.
- **Vertical Slider**: Bound to the `MasterVolume` property.

## 4. What Uses It
- **`Engine.cs`**: The core audio loop looks specifically for this module class to extract the final floating-point values and pass them to the `ISampleProvider` buffer.
- **`MainViewModel.cs`**: It is instantiated at startup.
- **`Cable.cs`**: Cables are used to route audio from other modules (like the VCO) into the left and right inputs of this module.

## 5. Detailed Breakdown of the Logic

### 5.1 Volume Scaling in `Process()`
```csharp
public void Process(float sampleRate)
{
    double currentVol = _masterVolumeRamp.Next();
    LeftInput.Value = (float)(LeftInput.Value * currentVol);
    RightInput.Value = (float)(RightInput.Value * currentVol);
}
```
**Why it's there**: The `Engine` calls `Process()` on all modules before reading the final output. Here, the `AudioOutputModule` takes whatever voltage is currently sitting on its input ports (placed there by a cable in the previous step) and multiplies it by the smoothed volume.

### 5.2 NAudio Bridge in `Engine.cs`
```csharp
foreach (var module in _modules)
{
    if (module is AudioOutputModule audioOut)
    {
        leftMix += audioOut.LeftInput.Value;
        rightMix += audioOut.RightInput.Value;
    }
}
```
**Why it's there**: This bridges the gap between the virtual "Control Voltage" domain and the physical hardware buffer required by NAudio. By adding `+=`, it technically allows multiple output modules to exist, acting as a final summing mixer.
