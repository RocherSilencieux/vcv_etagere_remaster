using System;
using System.Collections.Generic;

namespace vcv_etagere_remaster.Core.Interface
{
    /// <summary>
    /// Contract for the main audio engine that orchestrates DSP processing.
    /// </summary>
    public interface IAudioEngine : IDisposable
    {
        /// <summary>
        /// Adds a module to the processing graph.
        /// </summary>
        void AddModule(IModule module);

        /// <summary>
        /// Removes a module from the processing graph.
        /// </summary>
        void RemoveModule(IModule module);

        /// <summary>
        /// Starts audio playback/processing.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops audio playback/processing.
        /// </summary>
        void Stop();
    }
}
