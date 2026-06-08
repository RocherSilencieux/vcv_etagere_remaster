using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace vcv_etagere_remaster.Front.ViewModel.Base
{
    /// <summary>
    /// Provides a base class for view models that implements property change notification.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        // All models will implement that to notify (UI<>Code) the view when a property changes
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}