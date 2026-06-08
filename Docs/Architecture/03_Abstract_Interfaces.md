# Architecture Decision Record: Abstract Interfaces (The Core Domain)

## 1. Introduction and Context
In an application like VCV Rack, the core engine must interact with hundreds of different modules (VCO, VCA, LFO, Filters, Sequencers) without knowing what they are. If the Engine had to know about `VcoModule` or `VcaModule` specifically, the codebase would become an unmaintainable monolith.
To solve this, we rely heavily on **Interfaces** and **Polymorphism**. The Core domain defines *contracts* that any module must fulfill, and the Engine only talks to these contracts.

## 2. Why Abstract Interfaces?
- **Inversion of Control (IoC)**: High-level modules (the Engine) do not depend on low-level modules (the specific synthesizer algorithms). Both depend on abstractions (`IModule`).
- **Extensibility**: Anyone can create a new module by simply implementing `IModule`. The Engine will immediately know how to process it without requiring a single line of code change in `Engine.cs`.
- **Decoupling**: The Audio Engine knows nothing about the UI, and the UI knows nothing about the Audio Engine. They only communicate through the shared language of the Interfaces.

## 3. What It Uses
The Abstract Interfaces layer (`Core/Interface/`) relies strictly on pure C# primitives and namespaces. It does **not** reference WPF (`System.Windows`), and it does **not** reference NAudio. 
This is a critical architectural decision: The Domain layer must be completely independent of the presentation and infrastructure layers.

## 4. What Uses It
- **`Core/Audio/Engine.cs`**: Uses `IModule` to loop through DSP logic and `IPort` to route signals.
- **`Core/Modules/*`**: Every synthesizer module implements `IModule`.
- **`Base/ModuleViewModelBase.cs`**: The UI wrapper holds a reference to an `IModule` to expose its properties to the View.

## 5. Detailed Breakdown of Interfaces

### 5.1 `IModule.cs`
**The Contract**:
```csharp
public interface IModule
{
    string Id { get; }
    string Name { get; }
    void Process(float sampleRate);
}
```
**Why it's there**: It defines the absolute minimum requirements for an object to be considered a "Synthesizer Module" by the engine.
**How it works**: The `Id` is used to uniquely identify instances (crucial when saving/loading a patch state with multiple identical VCOs). The `Process` method is called 44,100 times a second. Inside this method, the module reads its input ports, performs its mathematical algorithm, and writes to its output ports.

### 5.2 `IPort.cs`
**The Contract**:
```csharp
public enum PortType { Input, Output }

public interface IPort
{
    string Id { get; }
    string Name { get; }
    PortType Type { get; }
    float Value { get; set; }
    bool IsConnected { get; }
}
```
**Why it's there**: Eurorack synthesizers communicate via Control Voltage (CV) over 3.5mm TS cables. In the digital realm, a voltage is just a `float` (floating-point number). `IPort` represents the physical jack.
**How it works**: 
- `Value` holds the current voltage. For audio, this fluctuates rapidly between -1.0f and 1.0f. For CV (like 1V/Octave), it might be a static value like 2.5f.
- `Type` enforces directional flow. Audio cannot flow out of an `Input` port.
- `IsConnected` allows modules to adapt their behavior based on whether a cable is plugged in (a technique known as "normaling" in hardware synths).

### 5.3 `IAudioEngine.cs`
**The Contract**:
```csharp
public interface IAudioEngine : IDisposable
{
    void AddModule(IModule module);
    void RemoveModule(IModule module);
    void Start();
    void Stop();
}
```
**Why it's there**: It provides a safe API for the UI to control the audio hardware without exposing the messy details of thread management or buffer allocation.

## 6. The "Domain Model" Purity
By restricting the `Core.Interface` namespace to only basic types, we achieve pure Domain-Driven Design (DDD). If we ever decide to port VCV Etagere Remaster to Unity, MAUI, or a headless server, the `Core` library can be copy-pasted without modification. This is the hallmark of a "sexy", well-coded architecture.
