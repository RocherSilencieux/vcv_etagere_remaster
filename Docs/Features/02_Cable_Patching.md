# Feature Decision Record: Cable Patching System

## 1. Introduction and Context
The defining characteristic of a modular synthesizer is patching. Users connect cables from output jacks to input jacks to route audio and control voltages. 
In software, this physical concept must be translated into memory references and data propagation algorithms. The `Cable` class handles this responsibility.

## 2. Why implement a Cable class?
Initially, one might think to simply have an `InputPort` hold a reference directly to an `OutputPort`. However, having a dedicated `Cable` entity provides several critical advantages:
- **Many-to-One / One-to-Many routing**: Eurorack allows "stackables" or "multiples" where one output goes to multiple inputs. Having discrete cable objects makes managing these complex graphs easier.
- **Visual Representation**: In the UI, a cable is a physical Bezier curve drawn on the screen. Having a `Cable` class in the Domain layer makes it trivial to create a `CableViewModel` that maps to the physical curve on screen.
- **Latency/Delay handling**: In advanced DSP, cables can introduce a 1-sample delay depending on graph execution order. A `Cable` object is the perfect place to implement this logic if needed.

## 3. What It Uses
### In the Domain (`Core/Audio/Cable.cs`):
- **`IPort`**: It holds two references: `Source` (an output port) and `Destination` (an input port).
- **`System.ArgumentException`**: Used for defensive programming to prevent invalid connections (e.g., connecting two outputs together).

## 4. What Uses It
- **`Core/Audio/Engine.cs`**: The audio engine holds a `List<Cable>`. During every sample tick, it iterates over every cable and calls `cable.Process()`.

## 5. Detailed Breakdown of the Cable Logic

### 5.1 Defensive Construction
```csharp
public Cable(IPort source, IPort destination)
{
    if (source.Type != PortType.Output) throw new ArgumentException("...");
    if (destination.Type != PortType.Input) throw new ArgumentException("...");
    
    Source = source;
    Destination = destination;
}
```
**Why it's there**: If a bug in the UI allows a user to draw a cable from an Input to an Input, the Domain layer must reject it immediately. This prevents silent failures and catastrophic audio feedback loops in the DSP engine.

### 5.2 The Propagation Loop (The `Process` Method)
```csharp
public void Process()
{
    Destination.Value = Source.Value;
}
```
**Why it's there**: This is the entirety of the patching logic. 
**How it works**: 
1. The Engine calculates the state of Module A. Module A writes `0.5f` to its Output Port.
2. The Engine calls `cable.Process()`.
3. The cable reads `0.5f` from Module A's Output Port and writes it to Module B's Input Port.
4. The Engine calculates the state of Module B. Module B reads `0.5f` from its Input Port and acts accordingly.

### 5.3 The Graph Execution Order Problem
Currently, the `Engine` processes all cables, and then all modules. This is a naive implementation suitable for early development.
**Why it's a problem**:
If Module B outputs to Module C, and Module C outputs to Module D, the values must flow in order. If the Engine processes Module D before Module C, Module D is processing *old* data from the previous sample frame. This causes a 1-sample delay, which alters phase relationships and can cause destructive interference (comb filtering) when audio signals are mixed back together.

**The Architectural Solution (Planned)**:
We will implement a **Topological Sorting Algorithm** in the `Engine`. When a `Cable` is added, the Engine will analyze the graph, detect the dependencies, and reorder the `_modules` list so that signal flow is respected sequentially from source to destination.

## 6. UI Considerations (Future)
The UI implementation of cables will require a Canvas overlay on top of the module grid.
When a user clicks on an `Ellipse` (representing a `PortViewModel`), a dragging event will initiate, drawing a Bezier curve to the mouse cursor. Upon releasing the mouse over another `PortViewModel`, a command will be sent to the `MainViewModel` to instantiate a new `Cable` in the `Engine`.
