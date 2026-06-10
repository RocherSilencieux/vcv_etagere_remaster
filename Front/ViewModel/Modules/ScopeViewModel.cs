using System;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class ScopeViewModel : ModuleViewModuleBase
    {
        public new ScopeModule Model { get; }

        public ScopeViewModel(ScopeModule model) : base(model)
        {
            Model = model;

            InputPorts.Add(new PortViewModelBase(Model.LeftInput));
            InputPorts.Add(new PortViewModelBase(Model.RightInput));
        }
    }
}
