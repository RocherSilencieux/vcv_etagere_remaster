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
        }

        public void SendMidiNote(int midiNote)
        {
            foreach (var module in Modules)
            {
                if (module.Model is MidiModule midi)
                {
                    midi.PlayNote(midiNote);
                }
            }
        }

        public void SendMidiRelease()
        {
            foreach (var module in Modules)
            {
                if (module.Model is MidiModule midi)
                {
                    midi.ReleaseNote();
                }
            }
        }
        public event Action<ModuleViewModuleBase>? ModuleRemoving;

        public void RemoveModule(ModuleViewModuleBase vm)
        {
            if (ModuleRemoving != null)
            {
                ModuleRemoving(vm);
            }
            else
            {
                Modules.Remove(vm);
                _engine.RemoveModule(vm.Model);
            }
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

                case "VCF":
                    var vcf = new VcfModule();
                    _engine.AddModule(vcf);
                    vm = new VcfViewModel(vcf) { GridX = x, GridY = y };
                    break;

                case "LFO":
                    var lfo = new LfoModule();
                    _engine.AddModule(lfo);
                    vm = new LfoViewModel(lfo) { GridX = x, GridY = y };
                    break;

                case "Scope":
                    var scope = new ScopeModule();
                    _engine.AddModule(scope);
                    vm = new ScopeViewModel(scope) { GridX = x, GridY = y };
                    break;

                case "Midi":
                    var midi = new MidiModule();
                    _engine.AddModule(midi);
                    vm = new MidiViewModel(midi) { GridX = x, GridY = y };
                    break;

                case "VCA":
                    var vca = new VcaModule();
                    _engine.AddModule(vca);
                    vm = new VcaViewModel(vca) { GridX = x, GridY = y };
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


        // Expose engine cable management for the UI so created cables are registered with the audio engine

    }
}

