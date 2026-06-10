using System;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;

namespace vcv_etagere_remaster.Front.ViewModel.Modules
{
    public class VcaViewModel : ModuleViewModuleBase
    {
        private readonly VcaModule _vcaModel = null!;

        public double BaseGain
        {
            get => _vcaModel != null ? _vcaModel.BaseGain : 1.0;
            set
            {
                if (_vcaModel != null)
                {
                    _vcaModel.BaseGain = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public VcaViewModel(VcaModule model) : base(model)
        {
            _vcaModel = model;

            InputPorts.Add(new PortViewModelBase(_vcaModel.LeftInput));
            InputPorts.Add(new PortViewModelBase(_vcaModel.RightInput));
            InputPorts.Add(new PortViewModelBase(_vcaModel.CvInput));

            OutputPorts.Add(new PortViewModelBase(_vcaModel.LeftOutput));
            OutputPorts.Add(new PortViewModelBase(_vcaModel.RightOutput));
        }
    }
}
