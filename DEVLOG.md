# DevLog

## 2026-06-03
- Initialization of DEVLOG.md to track all future changes and additions.
- Initialization of TODO.md to track ongoing and upcoming tasks.
- Created `/commit` workflow for Antigravity: When triggered, it will analyze git modifications, generate a clean commit, and push to the active branch.
- Saved AI behavior rules to `.cursorrules` in the project and added them to the editor's global Knowledge Items.
- Configured `.vscode/settings.json` for C# WPF (Font, Colors, Zoom, Typing feel, formatting).
- Added `.vscode/extensions.json` and requested installation for C# Dev Kit, IntelliCode, XML formatter, and One Dark Pro theme.
- Fixed compilation errors in `VcoView.xaml.cs` (missing event handlers) and added `.vscode/launch.json` and `.vscode/tasks.json` to enable the Play/Debug button (F5).
- Read NAudio documentation and integrated basic knowledge into the implementation plan.
- Created `Docs/PROJECT_OVERVIEW.md` to outline the VCV Etagere Remaster project requirements, MVVM architecture, and VCV Rack inspiration.
- Fixed nullable reference warnings in `Base/ViewModelBase.cs` to ensure a pristine 0-warning build.
- Executed the proposed MVVM abstract architecture for audio modules.
- Created `IModule`, `IPort`, `IAudioEngine` core interfaces.
- Installed `NAudio` (v2.3.0) via `vcv_etagere_remaster.csproj`.
- Built the core `Engine` using NAudio `ISampleProvider` with thread-safe data flow.
- Created `VcoModule` DSP block generating simple sine waves (1V/Oct emulation).
- Created `VcoViewModel` and `PortViewModelBase` adhering to the strict `MVVM` standards.
- Updated `MainWindow.xaml` with a clean `ItemsControl` to dynamically display the modular chain.
- Ensured 100% build validity (0 Errors, 0 Warnings).
- Added Rule 9 to `.cursorrules`: Mandating ultra-detailed Markdown documentation for every feature and architectural decision.
- Generated comprehensive architectural documentation in `Docs/Architecture/` (`01_MVVM_Pattern.md`, `02_NAudio_Engine.md`, `03_Abstract_Interfaces.md`).
- Generated comprehensive feature documentation in `Docs/Features/` (`01_VcoModule.md`, `02_Cable_Patching.md`, `03_UI_Rendering.md`).

## 2026-06-08
- Fixed `VcoViewModel.cs` property binding so the slider correctly alters the `BaseFrequency` property.
- Routed the `VcoModule` audio output to the master bus inside `Engine.cs` (attenuated to 0.1f volume).
- Integrated `LinearRamp` utility class to handle parameter smoothing.
- Applied `LinearRamp` to `VcoModule.BaseFrequency` to prevent audio pops and zipper noise when interacting with the UI slider.
- Added `Docs/Features/04_LinearRamp.md` to document the architectural utility of the parameter smoother in accordance with Rule 9.
- Created `audio_output` branch.
- Created `AudioOutputModule` to serve as the exclusive gateway to the NAudio hardware buffer.
- Removed hardcoded VCO audio routing from `Engine.cs` and replaced it with dynamic `AudioOutputModule` detection.
- Created `AudioOutputViewModel` and `AudioOutputView.xaml` with a Master Volume slider.
- Updated `MainViewModel` to hard-patch the VCO to the Audio Output module via `Cable` objects for testing.
- Generated `Docs/Features/05_AudioOutputModule.md` to document the new architecture.
- Implemented Windows audio output device query logic in `AudioOutputModule` utilizing NAudio `WaveOut.DeviceCount` and `WaveOut.GetCapabilities`.
- Added a `DeviceChanged` event to `AudioOutputModule` to notify the engine when the user changes selection.
- Updated `Engine.cs` to listen to the `DeviceChanged` event and dynamically recreate `WaveOutEvent` with the selected device ID.
- Added `AvailableDevices` list and `SelectedDevice` property bindings in `AudioOutputViewModel`.
- Implemented a `DEVICE` ComboBox in `AudioOutputView.xaml` and increased views' height to `360` to accommodate the selection dropdown.
- Fixed the `CS8618` compiler warning by initializing the `_audioOutputModel` field with `null!` to satisfy the WPF markup compilation analyzer.
- Generated `Docs/Features/06_AudioDeviceSelection.md` to document the audio device selection architecture.
- Added `ComboBox.ItemContainerStyle` to `AudioOutputView.xaml` to set the text color of the dropdown choices (`ComboBoxItem`) to `Black`, improving contrast and readability against the system default light background of the dropdown popup.
- Implemented `ReverbModule` DSP block using a Schroeder Reverb architecture (4 parallel comb filters and 2 series all-pass filters per channel).
- Configured a stereo spread layout by offsetting the right channel's delay line buffer lengths by 23 samples to create phase width.
- Created `ReverbViewModel` to bind Room Size, Damping, Mix, and Active/Bypass toggles.
- Created `ReverbView.xaml` layout themed with a deep-violet style header (`#2e113d`) and matching height/width dimensions (`360`x`160`).
- Registered `ReverbViewModel` to `ReverbView` DataTemplate mappings in `MainWindow.xaml`.
- Updated `MainViewModel` to instantiate and patch the Reverb module directly into the core audio loop: `VcoModule` -> `ReverbModule` -> `AudioOutputModule`.
- Created `Docs/Features/07_ReverbModule.md` detailing the mathematical formulas and implementation structure.
- Implemented `DelayModule` DSP block utilizing a fractional `DelayLine` with linear interpolation to eliminate parameter adjustment click noise.
- Created three delay modes: Simple Stereo, Ping-Pong (crossed feedback), and Mono.
- Created `DelayViewModel` mapping time, feedback, mix, bypass, and enum presets.
- Designed `DelayView.xaml` layout themed with a dark-teal header (`#0d2830`) and matching height/width dimensions (`360`x`160`).
- Styled the preset selection dropdown items in `DelayView.xaml` with black text for readability.
- Registered `DelayViewModel` to `DelayView` DataTemplate mappings in `MainWindow.xaml`.
- Patched the signal chain in `MainViewModel`: `VcoModule` -> `DelayModule` -> `ReverbModule` -> `AudioOutputModule`.
- Created `Docs/Features/08_DelayModule.md` outlining the interpolation math, delay structures, and MVVM routing.

## 2026-06-15
- Pulled `master` branch and merged it into the `midi` branch.
- Verified build validity (0 errors, 0 warnings).
- Pulled `presentation` branch and merged it into the `midi` branch.
- Created `ExternalMidiModule` using `NAudio.Midi` to capture real MIDI events from virtual or hardware inputs.
- Created `ExternalMidiViewModel` and `ExternalMidiView` to integrate the external MIDI capabilities into the application with dynamic device selection.

## 2026-06-16
- Removed invalid empty `<DataTemplate DataType="">` from `App.xaml` to resolve silent WPF markup compilation errors and the resulting CS5001 missing entry point error.
- Implemented a gravity-simulated Bézier curve approximation in `MainWindow.xaml.cs` to model natural cable drape based on horizontal span and distance.
- Implemented circular port-wrapping loops around module ports at the start and end of each cable in `MainWindow.xaml.cs` using WPF `ArcSegment`s.
- Adjusted port loops to be strictly fixed to actual module ports (removing the loop at the mouse cursor during dragging).
- Aligned start and end coordinates of the cable to the bottom border of the port circles so the line begins and ends on the port edges instead of the center.
- Created and updated highly detailed feature documentation in `Docs/Features/09_Gravity_Cables.md` detailing the gravity and wrapping loop implementation.
- Verified project compilation (0 build errors, 0 warnings).

## 2026-06-18
- Upgraded cable rendering system from a single Path to a composite FrameworkElement container Canvas.
- Implemented visual optimizations: shared single PathGeometry object among shadow, border, main, and highlight paths to avoid redundant Bézier math on the UI thread.
- Replaced CPU-expensive DropShadowEffect with a TranslateTransform translation running entirely on the GPU for the cable shadow.
- Rendered premium 3D patch plugs at both cable endpoints with multi-layered Ellipse geometries representing outer metal barrels, colored sleeves, rubber boots, and specular reflections.
- Updated module drag updating loop (UpdateCablesPosition) and modules collection changed handler (OnModulesCollectionChanged) to cleanly manipulate and dispose of the new Canvas containers.
- Re-structured MainWindow.xaml using Grid RowDefinitions to add a dark-themed main Menu bar (File, View, Engine).
- Integrated command click handlers: File (Clear Patch, Exit), View (Toggle Perf Monitor, Toggle Piano), and Engine (Toggle Audio Status).
- Configured Performance Monitor overlay in the top-right corner inside the canvas region, tracking FPS, DSP Load %, module count, and active cable count.
- Verified build compiles cleanly with 0 warnings and 0 errors.
- Created MixerModule class to mix 2 audio input signals with independent level, constant-power panning, and smooth mute transitions using LinearRamp smoothers.
- Created MixerViewModel wrapping the MixerModule properties and exposing visual ports.
- Designed MixerView.xaml with sliders for levels and panning, custom styled mute buttons, and input/output jack connectors.
- Integrated the Mixer module case inside MainViewModel and registered the DataTemplate and context menu item in MainWindow.xaml.
- Clarified rule 1 in .cursorrules to state that the emoji restriction only applies to chat responses.
- Added a slider emoji to the Mixer menu item in MainWindow.xaml.
- Created custom global flat dark theme in Front/Themes/DarkTheme.xaml styling ComboBox, ComboBoxItem, Menu, ContextMenu, MenuItem, and Separator.
- Merged the new stylesheet in App.xaml to apply it application-wide.
- Cleaned up inline styles and ItemContainerStyle overrides in all module views (Vco, Vcf, Lfo, Delay, ExternalMidi, AudioOutput) to inherit the clean dark theme.
- Simplified MainWindow.xaml by removing local Menu resources.

## 2026-06-18 (Investigation & Hotfix)
- Clean compiled the codebase (0 errors, 0 warnings).
- Prevented potential startup crashes in `Engine.cs` constructor where `new WaveOutEvent()` was instantiated before runtime safety checks. Modified it to be null-safe and instantiated dynamically during playback start or device changes.
- Wrapped `MidiIn` queries in `ExternalMidiModule.cs` inside defensive `try-catch` blocks to prevent crash failures when running on target environments without sound/MIDI cards or broken audio drivers.
- Ran a clean rebuild after clearing `obj` and `bin` cache directories, verifying compilation successfully completes with 0 errors and 0 warnings.
- Cleaned up inline `Foreground` and `Background` property overrides from the canvas `ContextMenu` and its child `MenuItem` elements in `MainWindow.xaml` so that they correctly inherit the modern flat style definitions from the global `DarkTheme.xaml`.
- Replaced the default `ContextMenu` control template in `DarkTheme.xaml` with a clean custom template containing only a dark `Border` and `ScrollViewer`, completely eliminating the default WPF vertical separator line/gutter.
- Refactored `MenuItem` template columns and added a trigger on `Icon` being null to collapse the icon column space when no icon is set, aligning all menu item text flush to the left.
