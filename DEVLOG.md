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
