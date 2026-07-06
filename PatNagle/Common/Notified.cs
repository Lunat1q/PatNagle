using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PatNagle.Common;

/// <summary>
///     INotifyPropertyChanged base. Replaces TiqUtils.Wpf.AbstractClasses.Notified (WPF-only).
/// </summary>
public abstract class Notified : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
