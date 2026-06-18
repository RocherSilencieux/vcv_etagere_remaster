using System;
using System.Windows.Input;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Front.ViewModel.Base
{
    /// <summary>
    /// Represents the UI logic for a patch point (jack).
    /// Wraps the IPort core interface.
    /// </summary>
    public class PortViewModelBase : ViewModelBase
    {
        private readonly IPort _model;

        public IPort Model => _model;

        public string Name => _model.Name;
        public PortType Type => _model.Type;

        public bool IsConnected 
        {
            get => _model.IsConnected;
            // The setter will be driven by the Model's state change in the future
        }

        public string TypeDisplay => Type == PortType.Input ? "Entrée (IN)" : "Sortie (OUT)";
        public string StatusDisplay => IsConnected ? "Connecté" : "Déconnecté";
        public string ConnectionColor => IsConnected ? "#4CAF50" : "#F44336"; // Green / Red
        public string ValueDisplay => $"{_model.Value:F3} V";

        public PortViewModelBase(IPort model)
        {
            _model = model;
        }
    }
}
