using System;

namespace vcv_etagere_remaster.Core.Interface
{
    /// <summary>
    /// Represents the base contract for an audio/DSP module.
    /// </summary>
    public interface IModule
    {
        string Id { get; }
        string Name { get; }

        /// <summary>
        /// Processes a single sample or a block of audio.
        /// </summary>
        /// <param name="sampleRate">The sample rate (e.g., 44100)</param>
        void Process(float sampleRate);
    }
}
