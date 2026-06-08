# Architecture Decision Record: MVVM Pattern

## 1. Introduction and Context
The VCV Etagere Remaster project is fundamentally a UI-heavy application that relies on real-time data processing (audio synthesis). The user interface must dynamically represent the state of the synthesizer—knobs turning, cables connecting, LEDs blinking—while the backend engine processes audio at 44.1kHz. 

To bridge this gap without creating "spaghetti code", a robust architectural pattern is required. The chosen pattern is **Model-View-ViewModel (MVVM)**.

## 2. Why MVVM?
MVVM is the industry standard for WPF (Windows Presentation Foundation) applications. It enforces a strict separation of concerns:
- **Model**: The domain logic. In our case, the DSP (Digital Signal Processing) classes like `VcoModule` or `Engine`. The Model is completely agnostic of the UI.
- **View**: The XAML files (`VcoView.xaml`, `MainWindow.xaml`). The View only contains layout, styles, and data bindings. No business logic resides in the code-behind (`.xaml.cs`).
- **ViewModel**: The intermediary. It wraps the Model and exposes properties that the View can bind to. It implements `INotifyPropertyChanged` to alert the UI when the Model's state changes.

### 2.1 Benefits for VCV Etagere Remaster
- **Testability**: The audio modules and ViewModels can be unit-tested without instantiating a WPF window.
- **Maintainability**: If we want to change how a VCO looks (View), we don't have to touch the math that generates the sine wave (Model).
- **Scalability**: As we add dozens of modules (LFO, VCF, ADSR, Sequencer), the MVVM pattern ensures they all follow the exact same structure.

## 3. What It Uses
The MVVM architecture in this project uses the following components:
- **`System.ComponentModel.INotifyPropertyChanged`**: The core .NET interface that enables the ViewModel to talk to the View.
- **`System.Collections.ObjectModel.ObservableCollection<T>`**: Used in `MainViewModel` and `ModuleViewModuleBase` to hold lists of modules or ports. When an item is added or removed, the UI updates automatically.
- **Data Binding (XAML)**: Used extensively in the Views (e.g., `{Binding BaseFrequency}`, `{Binding InputPorts}`).
- **DataTemplates**: Used in `MainWindow.xaml` to tell WPF, "If you see a `VcoViewModel` in the list, render it using a `VcoView`."

## 4. What Uses It
Every single visual component in the application uses this pattern.
- **`MainWindow`** uses **`MainViewModel`**.
- **`VcoView`** uses **`VcoViewModel`**.
- Future modules (e.g., `VcaView`) will use their respective ViewModels.
- The `ItemsControl` for ports uses `PortViewModelBase`.

## 5. Detailed Implementation Breakdown

### 5.1 `Base/ViewModelBase.cs`
This is the foundational class for all ViewModels.
**Why it's there**: Implementing `INotifyPropertyChanged` repeatedly in every ViewModel is tedious and violates DRY (Don't Repeat Yourself).
**How it works**: It provides a `protected void NotifyPropertyChanged([CallerMemberName] string? name = null)` method. When a property setter is called, it triggers the `PropertyChanged` event, notifying the UI binding engine.

### 5.2 `Base/ModuleViewModuleBase.cs`
**Why it's there**: Every synthesizer module has common traits: an ID, a Name, Input Ports, and Output Ports. This abstract class encapsulates these commonalities.
**How it works**: It inherits from `ViewModelBase`. It holds a protected reference to `IModule` (the Model). It initializes two `ObservableCollection<PortViewModelBase>` for inputs and outputs, ensuring that whenever a port is patched, the UI can reflect it.

### 5.3 `Base/PortViewModelBase.cs`
**Why it's there**: A port (jack) is a distinct interactive element. It needs to reflect its connection state and eventually handle Drag-and-Drop for cables.
**How it works**: It wraps the `IPort` interface. It exposes `Name`, `Type`, and `IsConnected`.

## 6. Architectural Flow and Data Propagation
1. The user turns a slider in `VcoView.xaml`.
2. The WPF Binding system updates the `BaseFrequency` property in `VcoViewModel`.
3. The setter in `VcoViewModel` calls `_vcoModel.SetBaseFrequency(value)`.
4. The Model (`VcoModule`) updates its internal math.
5. The ViewModel calls `NotifyPropertyChanged()`.
6. Any other UI elements listening to `BaseFrequency` are updated.

## 7. Future Considerations
- **Commands**: We currently don't use `ICommand` extensively. As we add buttons (e.g., "Reset Module", "Delete Cable"), we will need to implement a `RelayCommand` or `DelegateCommand` pattern to keep the logic out of the code-behind.
- **Performance**: High-frequency updates (e.g., an LED blinking at audio rate) cannot trigger `NotifyPropertyChanged` 44,100 times a second. We will need an aggressive throttling/decimation layer in the ViewModel to update the UI at a maximum of 60 FPS.
