///// base view model with INotifyPropertyChanged support /////

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AssumptionChecker.WPFApp.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged; // nullable since event handlers can be unsubscribed

        // == helper method to raise PropertyChanged event == //
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // == helper method to set property values and raise PropertyChanged if value changes == //
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false; // no change, do not raise event
            field = value;                                                      // update field value
            OnPropertyChanged(propertyName);                                    // raise PropertyChanged event for the property
            return true;
        }
    }
}