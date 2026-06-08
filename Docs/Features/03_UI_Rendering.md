# Feature Decision Record: Dynamic UI Rendering

## 1. Introduction and Context
In VCV Rack, the user can add any number of modules to their rack in any order. The UI must be completely dynamic, capable of rendering different user controls (a VCO, a Mixer, an Oscilloscope) based on the underlying data models currently active in the engine.
Hardcoding the layout is impossible. We must use WPF's data-driven rendering capabilities to generate the visual rack automatically.

## 2. Why implement it this way?
The MVVM pattern dictates that the View is a reflection of the ViewModel. If the `MainViewModel` has a list of 5 modules, the View should display 5 modules. We use the WPF **`ItemsControl`** combined with **`DataTemplates`** to achieve this dynamic generation.

## 3. What It Uses
- **WPF `ItemsControl`**: A control that generates visual items based on a data collection.
- **WPF `DataTemplate`**: A definition that tells WPF how to visually represent a specific C# Type.
- **WPF `WrapPanel`**: A layout panel that positions children horizontally, wrapping them to a new line when it runs out of space (perfect for a modular rack).
- **`System.Collections.ObjectModel.ObservableCollection<T>`**: A special collection that automatically notifies the UI when items are added or removed.

## 4. What Uses It
- **`MainWindow.xaml`**: Contains the root `ItemsControl` that renders the entire rack.
- **`VcoView.xaml`**: Contains nested `ItemsControl`s to dynamically render the input and output jacks based on the ports exposed by the `VcoModule`.

## 5. Detailed Breakdown of Dynamic Rendering

### 5.1 The Master Rack (`MainWindow.xaml`)
```xml
<Window.Resources>
    <DataTemplate DataType="{x:Type viewmodels:VcoViewModel}">
        <views:VcoView />
    </DataTemplate>
</Window.Resources>
```
**Why it's there**: This is the core secret to dynamic WPF UIs. We tell the Window: "If you ever encounter an object of type `VcoViewModel` in your data, don't just print its namespace. Instead, instantiate a `VcoView` UserControl and give it the `VcoViewModel` as its DataContext."
As we add more modules (e.g., `VcaViewModel`), we simply add one line to this Resource dictionary. The UI framework handles the rest.

### 5.2 The Layout Engine
```xml
<ItemsControl ItemsSource="{Binding Modules}">
    <ItemsControl.ItemsPanel>
        <ItemsPanelTemplate>
            <WrapPanel Orientation="Horizontal" Margin="20"/>
        </ItemsPanelTemplate>
    </ItemsControl.ItemsPanel>
</ItemsControl>
```
**Why it's there**: `MainViewModel.Modules` is an `ObservableCollection<ModuleViewModuleBase>`. The `ItemsControl` watches this list.
**How it works**:
1. The user clicks "Add VCO".
2. `MainViewModel` instantiates `VcoModule` and `VcoViewModel`, and adds them to the `Modules` list.
3. The `ObservableCollection` fires a `CollectionChanged` event.
4. The `ItemsControl` receives the event and asks the layout engine for space.
5. It looks up the `DataTemplate` for `VcoViewModel` and instantiates a `VcoView`.
6. The `WrapPanel` places the new module next to the previous one, simulating a physical Eurorack row.

### 5.3 Nested Dynamic Generation (Ports in `VcoView.xaml`)
```xml
<ItemsControl ItemsSource="{Binding InputPorts}">
    <ItemsControl.ItemTemplate>
        <DataTemplate>
            <StackPanel Orientation="Vertical" Margin="0,5">
                <TextBlock Text="{Binding Name}" />
                <Ellipse Width="20" Height="20" Fill="#111" Stroke="Gray" />
            </StackPanel>
        </DataTemplate>
    </ItemsControl.ItemTemplate>
</ItemsControl>
```
**Why it's there**: Even within a single module, we don't want to hardcode 5 different `<Ellipse>` tags for 5 jacks. If the DSP engineer adds a "PWM Input" to the `VcoModule` in C#, the UI should update automatically.
**How it works**: `ModuleViewModelBase` exposes `InputPorts`. The `ItemsControl` in `VcoView` iterates over these ports and draws a Jack (the `Ellipse`) and a label for each one dynamically.

## 6. Architectural Brilliance
Because of this nested dynamic generation, the entire UI is a slave to the Domain logic. 
If we decide that the VCO needs 3 outputs instead of 1, we literally just add `OutputPorts.Add(new SimplePort(...))` in the C# Model. We do not have to touch XAML. We do not have to reposition UI elements. The `ItemsControl` will simply draw a third jack automatically. This is the pinnacle of the "Keep It Simple, Stupid" (KISS) rule combined with advanced MVVM.
