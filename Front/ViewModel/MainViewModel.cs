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

        public ObservableCollection<ModuleViewModuleBase> Modules { get; } = new ObservableCollection<ModuleViewModuleBase>();

        public MainViewModel()
        {
            // Initialize Audio Engine
            _engine = new Engine();
            _engine.Start();

            // Create a test VCO Module
            var vcoModel = new VcoModule();
            _engine.AddModule(vcoModel);

            // Create ViewModel wrapper and expose it to the View
            var vcoViewModel = new VcoViewModel(vcoModel);
            Modules.Add(vcoViewModel);
        }

        public void Shutdown()
        {
            _engine.Stop();
            _engine.Dispose();
        }
    }
}
