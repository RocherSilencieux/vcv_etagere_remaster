# Feature Decision Record: Audio Output Device Selection

## 1. Introduction and Context
By default, the NAudio wave output layer (`WaveOutEvent`) is initialized with `DeviceNumber = -1`, which routes audio through the Windows Sound Mapper (the default OS device). 
In modular synthesis, users frequently need to target specific audio interfaces, external USB DACs, or routing tools (like VB-Cable or Virtual Audio Cable) directly within the application without changing their global Windows default playback device.
This feature enables the user to select their desired PC audio output device from a dropdown menu directly on the `AUDIO OUT` module.

## 2. Why implement it this way?
- **Decoupled Architecture**: The `AudioOutputModule` manages the user interface and domain list of hardware devices, while the `Engine` handles the underlying NAudio driver configuration. The module uses a standard C# event (`DeviceChanged`) to notify the engine when a new device is selected.
- **Dynamic Swapping**: When the device is changed, the audio engine stops the current playback, disposes of the old `WaveOutEvent`, creates a new `WaveOutEvent` targeting the requested `DeviceNumber`, calls `Init` and `Play` on it, and resumes audio seamlessly.
- **Robust Exception Handling**: Swapping hardware drivers at runtime can be prone to device locking or sample rate mismatches. The engine handles initialization failures gracefully without crashing the application.

## 3. What It Uses
### In the Domain (`Core/Modules/AudioOutputModule.cs`):
- **`AudioDevice`**: A small data class wrapping the NAudio device ID (`DeviceNumber`) and descriptive product name (`Name`).
- **`AvailableDevices`**: A read-only list populated in the constructor by querying `WaveOut.DeviceCount` and fetching product details via `WaveOut.GetCapabilities()`.
- **`SelectedDeviceNumber`**: An integer property mapping the selected index, which raises `DeviceChanged` on modification.
- **`DeviceChanged`**: An event of type `EventHandler<int>` fired when a new device number is selected.

### In the Presentation:
- **`AudioOutputViewModel.cs`**:
  - Exposes `AvailableDevices` collection.
  - Exposes `SelectedDevice` property to bind the active selection.
  - Connects the backing field to `SelectedDeviceNumber` on the domain model.
- **`AudioOutputView.xaml`**:
  - A WPF `ComboBox` bound to `AvailableDevices` and `SelectedDevice`.
  - Height adjusted to `360` for both `AudioOutputView` and `VcoView` to give enough room for the dropdown selector.
  - Styled `ComboBoxItem` via `ItemContainerStyle` to set the foreground text color to `Black` for optimal readability against the default Windows light popup background, while maintaining `White` foreground text for the selected item box.

## 4. What Uses It
- **`Engine.cs`**: Subscribes to the `DeviceChanged` event when the `AudioOutputModule` is registered. It handles the dynamic recreation of the output wave device.

## 5. Detailed Breakdown of the Logic

### 5.1 Querying Audio Devices in `AudioOutputModule.cs`
```csharp
public void RefreshDevices()
{
    AvailableDevices.Clear();
    AvailableDevices.Add(new AudioDevice { DeviceNumber = -1, Name = "Périphérique par défaut" });

    try
    {
        int deviceCount = WaveOut.DeviceCount;
        for (int i = 0; i < deviceCount; i++)
        {
            try
            {
                var capabilities = WaveOut.GetCapabilities(i);
                AvailableDevices.Add(new AudioDevice { DeviceNumber = i, Name = capabilities.ProductName });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting capabilities for device {i}: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error querying WaveOut devices: {ex.Message}");
    }
}
```
**Why it's there**: It safely populates the available audio devices. It always provides a fallback default device mapper (`-1`), and wraps the loop in exception handlers to prevent failure if an audio driver is misbehaving.

### 5.2 Dynamic Re-initialization in `Engine.cs`
```csharp
public void ChangeDevice(int deviceNumber)
{
    lock (_modules)
    {
        if (_currentDeviceNumber == deviceNumber)
            return;

        _currentDeviceNumber = deviceNumber;

        if (_isPlaying)
        {
            try
            {
                _waveOut.Stop();
            }
            catch { }

            _waveOut.Dispose();

            _waveOut = new WaveOutEvent()
            {
                DeviceNumber = _currentDeviceNumber,
                DesiredLatency = 100
            };

            try
            {
                _waveOut.Init(this);
                _waveOut.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error starting WaveOut with device {deviceNumber}: {ex.Message}");
            }
        }
        else
        {
            _waveOut.Dispose();
            _waveOut = new WaveOutEvent()
            {
                DeviceNumber = _currentDeviceNumber,
                DesiredLatency = 100
            };
        }
    }
}
```
**Why it's there**: It allows changing the device at any time. If the engine is currently generating audio (`_isPlaying`), it stops, disposes the old player, hooks the sample stream to a new `WaveOutEvent`, and restarts playback on the new hardware channel. If it is stopped, it prepares the device change in the background.
