using System;

namespace vcv_etagere_remaster.Core.Interface
{
    public enum PortType
    {
        Input,
        Output
    }

    /// <summary>
    /// Represents a connection point (jack) for a module.
    /// </summary>
    public interface IPort
    {
        string Id { get; }
        string Name { get; }
        PortType Type { get; }
        
        /// <summary>
        /// The current voltage/signal value of the port.
        /// </summary>
        float Value { get; set; }
        
        bool IsConnected { get; }
    }
}
