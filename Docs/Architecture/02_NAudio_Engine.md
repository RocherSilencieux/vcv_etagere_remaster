# Architecture Decision Record: NAudio Engine

## 1. Introduction and Context
A virtual modular synthesizer lives and dies by its audio engine. It requires a continuous, uninterrupted stream of floating-point numbers to be calculated and sent to the computer's sound card with minimal latency. 
The chosen library for this task is **NAudio**, a mature and robust open-source audio library for .NET.

## 2. Why NAudio?
- **Low Latency Options**: NAudio supports `WaveOut`, `DirectSound`, `WASAPI`, and critically, `ASIO`. ASIO is the professional standard for low-latency audio on Windows, making it perfect for real-time synthesis.
- **ISampleProvider Interface**: NAudio operates natively on 32-bit IEEE floating-point audio, which is exactly the mathematical format we need for DSP (Digital Signal Processing).
- **C# Native**: It avoids the need for complex C++ interoperability (P/Invoke) for basic audio I/O.

## 3. What It Uses
The Audio Engine component (`Core/Audio/Engine.cs`) relies on:
- **`NAudio.Wave.ISampleProvider`**: The contract NAudio uses to pull audio data.
- **`NAudio.Wave.IWavePlayer`**: The interface for the audio output device (e.g., `WaveOutEvent` or `AsioOut`).
- **`NAudio.Wave.WaveFormat`**: Defines the audio format (Sample Rate: 44100Hz, Channels: 2, Format: IEEE Float).
- **`System.Collections.Generic.List<T>`**: To hold the active `IModule` and `Cable` instances.

## 4. What Uses It
- **`Front/ViewModel/MainViewModel.cs`**: Instantiates the `Engine`, adds modules to it, and starts/stops the engine during the application lifecycle.
- **The Core Framework**: Every `IModule` relies on the Engine calling its `Process()` method at the correct audio rate.

## 5. Detailed Implementation Breakdown

### 5.1 `Core/Interface/IAudioEngine.cs`
**Why it's there**: To abstract the NAudio implementation away from the rest of the application. If we ever decide to switch to a different audio library (e.g., Csound, FMOD, or a custom C++ DLL), we only have to rewrite the class implementing this interface.
**What it does**: Exposes `AddModule`, `RemoveModule`, `Start`, and `Stop`.

### 5.2 `Core/Audio/Engine.cs`
**Why it's there**: This is the beating heart of the synthesizer.
**How it works**:
1. **Initialization**: In the constructor, it creates a `WaveFormat` of 44.1kHz Stereo. It instantiates `WaveOutEvent` as the default output device.
2. **The `Read` Method**: This is where the magic happens. NAudio calls `Read(float[] buffer, int offset, int count)` whenever the sound card needs more data.
3. **The DSP Loop**:
   - The engine determines how many "frames" of audio it needs to generate (`count / channels`).
   - It iterates sample by sample.
   - For every sample, it first tells all `Cable` instances to propagate voltages (`cable.Process()`).
   - Then, it tells every `IModule` to calculate its next state (`module.Process(sampleRate)`).
   - Finally, it collects the output (currently hardcoded as a sum, to be replaced by a Master Audio Interface module) and writes it into the `buffer`.
4. **Thread Safety**: The `Read` method runs on a high-priority background thread spawned by NAudio. The UI thread can add or remove modules concurrently. To prevent race conditions and crashes (e.g., modifying a list while iterating over it), the `lock (_modules)` and `lock (_cables)` blocks are used.

## 6. Future Considerations and Bottlenecks
- **ASIO Support**: Currently, it uses `WaveOutEvent` for maximum out-of-the-box compatibility on all Windows machines. We need to implement a Settings window to let the user select `AsioOut` for sub-10ms latency.
- **Lock Contention**: Using standard `lock` inside an audio callback is generally a bad practice in C++ due to unbounded priority inversion. In C#, it's acceptable for early prototyping, but as the graph grows, we may need to implement a lock-free ring buffer or concurrent queues for module addition/removal to guarantee audio thread stability without glitching (buffer underruns).
- **Graph Sorting**: Right now, modules are processed in the order they were added. In a modular synth, if Module A modulates Module B, Module A must be processed *before* Module B in the loop to avoid a 1-sample delay. We will need a Topological Sort algorithm to process the module graph in the correct execution order based on cable connections.
