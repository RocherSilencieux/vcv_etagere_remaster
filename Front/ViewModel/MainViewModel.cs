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
            var vcoViewModel = new VcoViewModel(vcoModel);
            Modules.Add(vcoViewModel);

            // Create Delay Module
            var delayModel = new DelayModule();
            _engine.AddModule(delayModel);
            var delayViewModel = new DelayViewModel(delayModel);
            Modules.Add(delayViewModel);

            // Create Reverb Module
            var reverbModel = new ReverbModule();
            _engine.AddModule(reverbModel);
            var reverbViewModel = new ReverbViewModel(reverbModel);
            Modules.Add(reverbViewModel);

            // Create Audio Output Module
            var audioOutModel = new AudioOutputModule();
            _engine.AddModule(audioOutModel);
            var audioOutViewModel = new AudioOutputViewModel(audioOutModel);
            Modules.Add(audioOutViewModel);

            // Wire them together: VCO -> Delay -> Reverb -> AudioOutput
            var cableVcoToDelayL = new Cable(vcoModel.AudioOutput, delayModel.LeftInput);
            var cableVcoToDelayR = new Cable(vcoModel.AudioOutput, delayModel.RightInput);
            _engine.AddCable(cableVcoToDelayL);
            _engine.AddCable(cableVcoToDelayR);

            var cableDelayToReverbL = new Cable(delayModel.LeftOutput, reverbModel.LeftInput);
            var cableDelayToReverbR = new Cable(delayModel.RightOutput, reverbModel.RightInput);
            _engine.AddCable(cableDelayToReverbL);
            _engine.AddCable(cableDelayToReverbR);

            var cableReverbToOutL = new Cable(reverbModel.LeftOutput, audioOutModel.LeftInput);
            var cableReverbToOutR = new Cable(reverbModel.RightOutput, audioOutModel.RightInput);
            _engine.AddCable(cableReverbToOutL);
            _engine.AddCable(cableReverbToOutR);
        }

        public void Shutdown()
        {
            _engine.Stop();
            _engine.Dispose();
        }
    }
}
