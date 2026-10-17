using CommunityToolkit.Mvvm.ComponentModel;
using KioskClient.Core.Models;

namespace KioskClient.ViewModels;

/// <summary>
/// List item wrapper used by the orchestration editor to display an action.
/// </summary>
public class EditorActionItem : ObservableObject
{
    public EditorActionItem(ActionBase action)
    {
        Action = action;
    }

    public ActionBase Action { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Action.Name) ? "(unnamed)" : Action.Name;

    public string TypeLabel => Action switch
    {
        ImageAction => "Image",
        WebsiteAction => "Website",
        VideoAction => "Video",
        _ => "Action"
    };

    public void Refresh() => OnPropertyChanged(nameof(DisplayName));
}
