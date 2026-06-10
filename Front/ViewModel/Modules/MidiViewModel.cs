using System;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class MidiViewModel : ModuleViewModuleBase
    {
        public MidiViewModel(MidiModule model) : base(model)
        {
            OutputPorts.Add(new PortViewModelBase(model.VOctOutput));
            OutputPorts.Add(new PortViewModelBase(model.GateOutput));
        }
    }
}
