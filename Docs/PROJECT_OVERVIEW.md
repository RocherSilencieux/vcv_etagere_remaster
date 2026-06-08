# VCV Etagere Remaster

## Inspiration
This project is deeply inspired by **VCV Rack**, a renowned open-source virtual modular synthesizer. VCV Rack offers an endless playground for sound design through visual, patchable modules that mimic hardware Eurorack synthesizers. Our goal is to capture that essence—the modularity, the visual patching, and the real-time audio synthesis—and bring it into a bespoke, modern C# WPF application with a focus on an impeccable, clean, and "sexy" architecture.

## Overview
**VCV Etagere Remaster** aims to build a robust, maintainable, and highly performant modular audio synthesis engine coupled with a sleek user interface. The project utilizes **NAudio** for low-latency audio processing and playback, and relies heavily on advanced C# features to manage the complex flow of signals and UI interactions.

## Requirements & Architectural Guidelines

### 1. Advanced MVVM Architecture
- **Separation of Concerns:** Strict adherence to the Model-View-ViewModel (MVVM) pattern.
- **Data Binding:** Use WPF's powerful data binding to keep the UI synchronized with the underlying module states.
- **Base Classes:** Utilize `ViewModelBase` for `INotifyPropertyChanged` implementation and `ModuleViewModuleBase` for module-specific logic.

### 2. Event-Driven & Delegate-Based Communication
- **Actions and Delegates:** Use `Action` and `Func` delegates for lightweight callbacks between modules (e.g., signal routing).
- **Events:** Use standard C# events for broader system notifications, such as engine state changes or UI updates.

### 3. Abstract Interface Model
- **Interfaces:** Define clear contracts for components using interfaces (e.g., `IAudioEngine`, `IModule`, `IPort`).
- **Abstraction:** Use abstract base classes to provide common functionality for all synthesizer modules, ensuring new modules can be added with minimal boilerplate.

### 4. Code Quality & Rules
- **KISS (Keep It Simple, Stupid):** Code must be direct, efficient, and over-engineering must be avoided where unnecessary, without sacrificing the architectural integrity.
- **Documentation:** All code must be commented and documented in **English**.
- **No Emojis:** Strict adherence to professional communication without emojis.
- **Verifiable:** Code must build perfectly at all times.
- **DevLog & TODO:** Continuous tracking of progress in `DEVLOG.md` and `TODO.md`.

## Core Components (Planned)
1. **Audio Engine:** The heart of the application, responsible for processing DSP (Digital Signal Processing) loops and interfacing with NAudio.
2. **Module System:** The modular blocks (VCO, VCA, LFO, Filters) that generate or manipulate audio/control signals.
3. **Patching System:** The logic and UI for connecting outputs of one module to inputs of another, routing the signal graph dynamically.
