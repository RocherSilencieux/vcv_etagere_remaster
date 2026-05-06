using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using vcv_etagere_remaster.Core.Interface;

namespace vcv_etagere_remaster.Front.ViewModel.Base
{
    public abstract class ModuleViewModuleBase : ViewModelBase
    {
        public abstract IAudioEngine Engine {set;}
    }
}
