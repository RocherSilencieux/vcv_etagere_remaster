using System;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Core.Modules
{
    public class MidiModule : IModule
    {
        public string Id { get; } = Guid.NewGuid().ToString();
        public string Name => "MIDI > CV";

        public IPort VOctOutput { get; }
        public IPort GateOutput { get; }

        public MidiModule()
        {
            VOctOutput = new SimplePort(Guid.NewGuid().ToString(), "V/OCT", PortType.Output);
            GateOutput = new SimplePort(Guid.NewGuid().ToString(), "GATE", PortType.Output);
        }

        public void Process(float sampleRate)
        {
            // Voltages are updated directly via the UI MIDI keyboard
        }

        public void PlayNote(int midiNote)
        {
            // 1V/Octave standard: MIDI 60 (Middle C) = 0.0V.
            VOctOutput.Value = (midiNote - 60) / 12f;
            GateOutput.Value = 1.0f;
        }

        public void ReleaseNote()
        {
            GateOutput.Value = 0.0f;
        }
    }
}
