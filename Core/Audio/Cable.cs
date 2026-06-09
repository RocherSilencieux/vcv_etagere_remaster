using System;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Audio
{
    /// <summary>
    /// Represents a patch cable connecting an output port to an input port.
    /// </summary>
    public class Cable
    {
        public IPort Source { get; }
        public IPort Destination { get; }
        public System.Windows.Shapes.Path Visual { get; set; }
        public Cable(IPort source, IPort destination)
        {
            if (source.Type != PortType.Output)
                throw new ArgumentException("Source must be an Output port.");
            
            if (destination.Type != PortType.Input)
                throw new ArgumentException("Destination must be an Input port.");

            Source = source;
            Destination = destination;
        }

        /// <summary>
        /// Transfers the signal value from the source to the destination.
        /// Should be called on every audio frame update.
        /// </summary>
        public void Process()
        {
            Destination.Value = Source.Value;
        }

        public void AddCable(Engine engine)
        {
            engine?.AddCable(this);
        }


        public void RemoveCable(Engine engine)
        {
            engine?.RemoveCable(this);
        }
        public static void Stop(Engine engine)
        {
            if (engine == null) return;
            try { engine.Stop(); } catch { }
            try { engine.Dispose(); } catch { }
        }
    }
}
