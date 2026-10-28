using System.Collections.ObjectModel;

namespace KioskClient.ViewModels;

/// <summary>
/// Non-generic collection of strings usable as an ItemsSource under Native AOT
/// (CsWinRT generates the WinRT marshalling for annotated types).
/// </summary>
[global::WinRT.GeneratedBindableCustomProperty]
public sealed partial class StringCollection : ObservableCollection<string>
{
}

/// <summary>
/// Non-generic collection of editor actions usable as an ItemsSource under Native AOT.
/// </summary>
[global::WinRT.GeneratedBindableCustomProperty]
public sealed partial class EditorActionItemCollection : ObservableCollection<EditorActionItem>
{
}
