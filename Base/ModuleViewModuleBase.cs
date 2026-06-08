using System;
using System.Collections.ObjectModel;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Front.ViewModel.Base
{
    public abstract class ModuleViewModuleBase : ViewModelBase
    {
        protected readonly IModule _model;

        public string Name => _model.Name;
        public string Id => _model.Id;

        // Using ObservableCollection for UI binding
        public ObservableCollection<PortViewModelBase> InputPorts { get; } = new ObservableCollection<PortViewModelBase>();
        public ObservableCollection<PortViewModelBase> OutputPorts { get; } = new ObservableCollection<PortViewModelBase>();

        public ModuleViewModuleBase(IModule model)
        {
            _model = model;
        }
    }
}
