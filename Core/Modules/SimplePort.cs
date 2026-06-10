using System;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Modules
{
    public class SimplePort : IPort
    {
        public string Id { get; }
        public string Name { get; }
        public PortType Type { get; }
        public float Value { get; set; }
        public bool IsConnected { get; set; }

        public SimplePort(string id, string name, PortType type)
        {
            Id = id;
            Name = name;
            Type = type;
        }
    }
}
