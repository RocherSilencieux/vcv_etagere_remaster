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
        public Engine Engine => _engine; // Expose Engine instance so callers can use Cable methods directly
        private const double GridSize = 20.0;

        public ObservableCollection<ModuleViewModuleBase> Modules { get; } = new ObservableCollection<ModuleViewModuleBase>();

        public MainViewModel()
        {
            // Initialize Audio Engine
            _engine = new Engine();
            _engine.Start();

            // Create initial modules positionnés sur la grille
            var vcoModel = new VcoModule();
            _engine.AddModule(vcoModel);
            var vcoVm = new VcoViewModel(vcoModel) { GridX = 20, GridY = 20 };
            RegisterRemove(vcoVm);
            Modules.Add(vcoVm);

            var adsrModel = new AdsrModule();
            _engine.AddModule(adsrModel);
            var adsrVm = new AdsrViewModel(adsrModel) { GridX = 200, GridY = 20 };
            RegisterRemove(adsrVm);
            Modules.Add(adsrVm);

            var delayModel = new DelayModule();
            _engine.AddModule(delayModel);
            var delayVm = new DelayViewModel(delayModel) { GridX = 380, GridY = 20 };
            RegisterRemove(delayVm);
            Modules.Add(delayVm);

            var reverbModel = new ReverbModule();
            _engine.AddModule(reverbModel);
            var reverbVm = new ReverbViewModel(reverbModel) { GridX = 560, GridY = 20 };
            RegisterRemove(reverbVm);
            Modules.Add(reverbVm);

            var audioOutModel = new AudioOutputModule();
            _engine.AddModule(audioOutModel);
            var audioOutVm = new AudioOutputViewModel(audioOutModel) { GridX = 740, GridY = 20 };
            RegisterRemove(audioOutVm);
            Modules.Add(audioOutVm);

            // Wire them together: VCO -> ADSR -> Delay -> Reverb -> AudioOutput
            _engine.AddCable(new Cable(vcoModel.AudioOutput, adsrModel.AudioInput));
            _engine.AddCable(new Cable(adsrModel.AudioOutput, delayModel.LeftInput));
            _engine.AddCable(new Cable(adsrModel.AudioOutput, delayModel.RightInput));
            
            _engine.AddCable(new Cable(delayModel.LeftOutput, reverbModel.LeftInput));
            _engine.AddCable(new Cable(delayModel.RightOutput, reverbModel.RightInput));
            _engine.AddCable(new Cable(reverbModel.LeftOutput, audioOutModel.LeftInput));
            _engine.AddCable(new Cable(reverbModel.RightOutput, audioOutModel.RightInput));
        }

        /// <summary>
        /// Assigne le RemoveCommand au ViewModel (appelé après chaque création de module).
        /// </summary>
        private void RegisterRemove(ModuleViewModuleBase vm)
        {
            vm.RemoveCommand = new RelayCommand(_ => RemoveModule(vm));
        }

        /// <summary>
        /// Crée et ajoute un nouveau module au rack à la position (x, y) sur la grille.
        /// </summary>
        public void AddModule(string type, double x, double y)
        {
            ModuleViewModuleBase? vm = null;

            switch (type)
            {
                case "VCO":
                    var vco = new VcoModule();
                    _engine.AddModule(vco);
                    vm = new VcoViewModel(vco) { GridX = x, GridY = y };
                    break;

                case "Delay":
                    var delay = new DelayModule();
                    _engine.AddModule(delay);
                    vm = new DelayViewModel(delay) { GridX = x, GridY = y };
                    break;

                case "Reverb":
                    var reverb = new ReverbModule();
                    _engine.AddModule(reverb);
                    vm = new ReverbViewModel(reverb) { GridX = x, GridY = y };
                    break;

                case "AudioOutput":
                    var audioOut = new AudioOutputModule();
                    _engine.AddModule(audioOut);
                    vm = new AudioOutputViewModel(audioOut) { GridX = x, GridY = y };
                    break;

                case "ADSR":
                    var adsr = new AdsrModule();
                    _engine.AddModule(adsr);
                    vm = new AdsrViewModel(adsr) { GridX = x, GridY = y };
                    break;
            }

            if (vm != null)
            {
                RegisterRemove(vm);
                Modules.Add(vm);
            }
        }

        /// <summary>
        /// Supprime un module du rack et de la collection.
        /// </summary>
        public void RemoveModule(ModuleViewModuleBase vm)
        {
            Modules.Remove(vm);
            // Wire them together
            //var cableLeft = new Cable(vcoModel.AudioOutput, audioOutModel.LeftInput);
            //var cableRight = new Cable(vcoModel.AudioOutput, audioOutModel.RightInput);
            //_engine.AddCable(cableLeft);
            //_engine.AddCable(cableRight);
        }

        // Expose engine cable management for the UI so created cables are registered with the audio engine
        
    }
}

