using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
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

        // --- Grid position (pixels, multiples of GridSize) ---
        private double _gridX;
        public double GridX
        {
            get => _gridX;
            set { _gridX = value; NotifyPropertyChanged(); }
        }

        private double _gridY;
        public double GridY
        {
            get => _gridY;
            set { _gridY = value; NotifyPropertyChanged(); }
        }

        // --- Commande de suppression (assignée par MainViewModel) ---
        public ICommand? RemoveCommand { get; set; }

        public ModuleViewModuleBase(IModule model)
        {
            _model = model;
        }
    }
}
