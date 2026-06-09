using System.Collections.ObjectModel;
using vcv_etagere_remaster.Core.Audio;
using vcv_etagere_remaster.Core.Modules;
using vcv_etagere_remaster.Front.ViewModel.Base;
using vcv_etagere_remaster.Front.ViewModel.Modules;

namespace vcv_etagere_remaster.Front.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly Engine _engine;

        // Expose engine so the UI can call engine-level operations (cable management, stop, dispose)
        public Engine Engine => _engine;

        public ObservableCollection<ModuleViewModuleBase> Modules { get; } = new ObservableCollection<ModuleViewModuleBase>();

        public MainViewModel()
        {
            // Initialize Audio Engine
            _engine = new Engine();
            _engine.Start();

            // Create a test VCO Module
            var vcoModel = new VcoModule();
            _engine.AddModule(vcoModel);
            var vcoViewModel = new VcoViewModel(vcoModel);
            Modules.Add(vcoViewModel);

            // Create Audio Output Module
            var audioOutModel = new AudioOutputModule();
            _engine.AddModule(audioOutModel);
            var audioOutViewModel = new AudioOutputViewModel(audioOutModel);
            Modules.Add(audioOutViewModel);

            // Wire them together
            //var cableLeft = new Cable(vcoModel.AudioOutput, audioOutModel.LeftInput);
            //var cableRight = new Cable(vcoModel.AudioOutput, audioOutModel.RightInput);
            //_engine.AddCable(cableLeft);
            //_engine.AddCable(cableRight);
        }

        // NOTE: Cable management and engine lifecycle are handled by Engine directly.
    }
}
