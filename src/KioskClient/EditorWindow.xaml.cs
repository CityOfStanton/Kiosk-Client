using System.Collections.ObjectModel;
using KioskClient.Core.Models;
using KioskClient.Core.Serialization;
using KioskClient.Core.Services;
using KioskClient.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace KioskClient;

/// <summary>
/// Editor for creating, converting, validating, and saving orchestration files.
/// </summary>
public sealed partial class EditorWindow : Window
{
    private readonly ObservableCollection<EditorActionItem> _items = [];
    private Orchestration _orchestration = CreateNew();
    private StorageFile? _file;
    private bool _isXml;
    private bool _dirty;
    private bool _loading;
    private bool _allowClose;
    private int _testVersion;

    private const int MaxHistory = 200;
    private static readonly long CoalesceTicks = 700;
    private readonly List<string> _history = [];
    private int _historyIndex;
    private int _savedIndex;
    private bool _restoring;
    private long _lastRecordTick;
    private int _lastRecordSelected;

    public EditorWindow()
    {
        this.InitializeComponent();

        ActionList.ItemsSource = _items;
        LifecycleBox.ItemsSource = Enum.GetNames<LifecycleBehavior>();
        OrderBox.ItemsSource = Enum.GetNames<Ordering>();
        StretchBox.ItemsSource = Enum.GetNames<ImageStretch>();
        PlayModeBox.ItemsSource = Enum.GetNames<VideoPlayMode>();

        AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 850));
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        AppWindow.Closing += AppWindow_Closing;

        SetOwner();

        LoadOrchestration(CreateNew(), null, false);
    }

    private const int GWLP_HWNDPARENT = -8;

    [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    /// <summary>Makes the main window the owner so this window always stays above it.</summary>
    private void SetOwner()
    {
        try
        {
            var owner = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            var self = WinRT.Interop.WindowNative.GetWindowHandle(this);
            if (owner != IntPtr.Zero && self != IntPtr.Zero)
                SetWindowLongPtr(self, GWLP_HWNDPARENT, owner);
        }
        catch
        {
            // Fall back to normal window ordering.
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    /// <summary>Shows the window and gives it focus, in front of the other app windows.</summary>
    public void BringToFront()
    {
        AppWindow.Show();
        Activate();
        SetForegroundWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));
    }

    private static Orchestration CreateNew() => new()
    {
        Name = "New Orchestration",
        Version = OrchestrationMigrator.CurrentVersion
    };

    #region Loading

    /// <summary>
    /// Loads a copy of an orchestration that was already loaded elsewhere in the app.
    /// </summary>
    public async Task LoadFromOrchestrationAsync(Orchestration source)
    {
        var copy = OrchestrationSerializer.DeserializeJson(OrchestrationSerializer.SerializeJson(source));
        if (copy is null) return;

        copy.UsedDeprecatedPollingField = source.UsedDeprecatedPollingField;
        copy.Source = source.Source;
        copy.SourcePath = source.SourcePath;

        StorageFile? file = null;
        var isXml = false;
        if (source.Source == OrchestrationSource.File && !string.IsNullOrWhiteSpace(source.SourcePath))
        {
            try
            {
                file = await StorageFile.GetFileFromPathAsync(source.SourcePath);
                isXml = file.FileType.Equals(".xml", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                file = null;
            }
        }

        LoadOrchestration(copy, file, isXml);
        if (file is null && !string.IsNullOrWhiteSpace(source.SourcePath))
            FilePathBox.Text = source.SourcePath;
    }

    private async Task LoadFileAsync(StorageFile file)
    {
        try
        {
            var text = await FileIO.ReadTextAsync(file);
            var orchestration = OrchestrationSerializer.Deserialize(text);
            if (orchestration is null)
            {
                await ShowMessageAsync("Unable to open file", "The file is not a valid JSON or XML orchestration.");
                return;
            }

            orchestration.Source = OrchestrationSource.File;
            orchestration.SourcePath = file.Path;
            var isXml = file.FileType.Equals(".xml", StringComparison.OrdinalIgnoreCase)
                        || (!file.FileType.Equals(".json", StringComparison.OrdinalIgnoreCase) && text.TrimStart().StartsWith('<'));
            LoadOrchestration(orchestration, file, isXml);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Unable to open file", ex.Message);
        }
    }

    private void LoadOrchestration(Orchestration orchestration, StorageFile? file, bool isXml)
    {
        _orchestration = orchestration;
        _file = file;
        FilePathBox.Text = file?.Path ?? string.Empty;
        _isXml = isXml;

        var changes = OrchestrationMigrator.Migrate(orchestration);

        _loading = true;
        NameBox.Text = orchestration.Name;
        VersionText.Text = $"Format version: {orchestration.Version}";
        LifecycleBox.SelectedItem = orchestration.Lifecycle.ToString();
        OrderBox.SelectedItem = orchestration.Order.ToString();
        _loading = false;

        _items.Clear();
        foreach (var action in orchestration.Actions)
            _items.Add(new EditorActionItem(action));

        ValidationBar.IsOpen = false;
        if (changes.Count > 0)
        {
            MigrationBar.Message = string.Join(Environment.NewLine, changes.Select(c => "• " + c))
                                   + Environment.NewLine + "Save the file to write the converted version.";
            MigrationBar.IsOpen = true;
        }
        else
        {
            MigrationBar.IsOpen = false;
        }

        SetDirty(changes.Count > 0);

        ResetHistory(changes.Count > 0);

        if (_items.Count > 0)
            ActionList.SelectedIndex = 0;
        else
            LoadActionForm(null);
    }

    #endregion

    #region Commands

    private async void NewButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardAsync()) return;
        LoadOrchestration(CreateNew(), null, false);
    }

    private async void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmDiscardAsync()) return;

        var picker = new FileOpenPicker { SuggestedStartLocation = PickerLocationId.DocumentsLibrary };
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".xml");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
            await LoadFileAsync(file);
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e) => await SaveAsync(false);

    private async void SaveAsButton_Click(object sender, RoutedEventArgs e) => await SaveAsync(true);

    private async Task<bool> SaveAsync(bool saveAs)
    {
        if (saveAs || _file is null)
        {
            var name = string.Concat((_orchestration.Name ?? string.Empty).Split(Path.GetInvalidFileNameChars()));
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = string.IsNullOrWhiteSpace(name) ? "Orchestration" : name
            };

            if (_isXml)
            {
                picker.FileTypeChoices.Add("XML Files", new List<string> { ".xml" });
                picker.FileTypeChoices.Add("JSON Files", new List<string> { ".json" });
            }
            else
            {
                picker.FileTypeChoices.Add("JSON Files", new List<string> { ".json" });
                picker.FileTypeChoices.Add("XML Files", new List<string> { ".xml" });
            }

            WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(this));

            var picked = await picker.PickSaveFileAsync();
            if (picked is null) return false;

            _file = picked;
            FilePathBox.Text = picked.Path;
            _isXml = picked.FileType.Equals(".xml", StringComparison.OrdinalIgnoreCase);
        }

        OrchestrationMigrator.Migrate(_orchestration);
        VersionText.Text = $"Format version: {_orchestration.Version}";

        try
        {
            var content = _isXml
                ? OrchestrationSerializer.SerializeXml(_orchestration)
                : OrchestrationSerializer.SerializeJson(_orchestration);
            await FileIO.WriteTextAsync(_file!, content);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Unable to save file", ex.Message);
            return false;
        }

        MigrationBar.IsOpen = false;
        SetDirty(false);
        _savedIndex = _historyIndex;
        StatusText.Text = $"Saved to {_file!.Path}";
        return true;
    }

    private void ValidateButton_Click(object sender, RoutedEventArgs e)
    {
        var failures = new List<string>();
        Collect(_orchestration.Validate(), failures);

        if (failures.Count == 0)
        {
            ValidationBar.Severity = InfoBarSeverity.Success;
            ValidationBar.Title = "Validation passed";
            ValidationBar.Message = "No problems were found. Use the Test button on each action to verify its path.";
        }
        else
        {
            ValidationBar.Severity = InfoBarSeverity.Error;
            ValidationBar.Title = $"{failures.Count} validation problem(s)";
            ValidationBar.Message = string.Join(Environment.NewLine, failures);
        }
        ValidationBar.IsOpen = true;
    }

    private static void Collect(ValidationResult result, List<string> failures)
    {
        if (result.IsValid == false)
        {
            var text = $"{result.Identifier}: {result.Message}";
            if (!string.IsNullOrWhiteSpace(result.Guidance))
                text += $" {result.Guidance}";
            failures.Add(text);
        }

        foreach (var child in result.Children)
            Collect(child, failures);
    }

    #endregion

    #region Action list

    private ActionBase? SelectedAction => (ActionList.SelectedItem as EditorActionItem)?.Action;

    private void ActionList_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadActionForm(SelectedAction);

    private void AddAction_Click(object sender, RoutedEventArgs e)
    {
        var version = OrchestrationMigrator.CurrentVersion;
        ActionBase action = (sender as MenuFlyoutItem)?.Tag as string switch
        {
            "Image" => new ImageAction { Name = "New Image", Version = version, Duration = 10 },
            "Website" => new WebsiteAction { Name = "New Website", Version = version, Duration = 30 },
            _ => new VideoAction { Name = "New Video", Version = version, Duration = 30 }
        };

        _orchestration.Actions.Add(action);
        var item = new EditorActionItem(action);
        _items.Add(item);
        ActionList.SelectedItem = item;
        MarkDirty();
    }

    private void RemoveAction_Click(object sender, RoutedEventArgs e)
    {
        var index = ActionList.SelectedIndex;
        if (index < 0) return;

        _orchestration.Actions.RemoveAt(index);
        _items.RemoveAt(index);
        if (_items.Count > 0)
            ActionList.SelectedIndex = Math.Min(index, _items.Count - 1);
        MarkDirty();
    }

    private void ActionList_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Delete && ActionList.SelectedItem is not null)
        {
            RemoveAction_Click(sender, e);
            e.Handled = true;
        }
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) => MoveSelected(-1);

    private void MoveDown_Click(object sender, RoutedEventArgs e) => MoveSelected(1);

    private void ActionList_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        var reordered = _items.Select(i => i.Action).ToList();
        if (reordered.SequenceEqual(_orchestration.Actions)) return;

        var selected = SelectedAction;
        _orchestration.Actions.Clear();
        _orchestration.Actions.AddRange(reordered);

        if (selected is not null)
            ActionList.SelectedItem = _items.FirstOrDefault(i => i.Action == selected);
        MarkDirty();
    }

    private void MoveSelected(int delta)
    {
        var index = ActionList.SelectedIndex;
        var target = index + delta;
        if (index < 0 || target < 0 || target >= _items.Count) return;

        var action = _orchestration.Actions[index];
        _orchestration.Actions.RemoveAt(index);
        _orchestration.Actions.Insert(target, action);
        _items.Move(index, target);
        ActionList.SelectedIndex = target;
        MarkDirty();
    }

    #endregion

    #region Orchestration form

    private void NameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading) return;
        _orchestration.Name = NameBox.Text;
        MarkDirty();
    }

    private void LifecycleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LifecycleBox.SelectedItem is not string value) return;
        _orchestration.Lifecycle = Enum.Parse<LifecycleBehavior>(value);
        MarkDirty();
    }

    private void OrderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || OrderBox.SelectedItem is not string value) return;
        _orchestration.Order = Enum.Parse<Ordering>(value);
        MarkDirty();
    }

    #endregion

    #region Action form

    private static string GetPath(ActionBase action) => action switch
    {
        ImageAction i => i.Path,
        WebsiteAction w => w.Path,
        VideoAction v => v.Path,
        _ => string.Empty
    };

    private static void SetPath(ActionBase action, string path)
    {
        switch (action)
        {
            case ImageAction i: i.Path = path; break;
            case WebsiteAction w: w.Path = path; break;
            case VideoAction v: v.Path = path; break;
        }
    }

    private void LoadActionForm(ActionBase? action)
    {
        _testVersion++;
        _loading = true;
        try
        {
            ActionPanel.Visibility = action is null ? Visibility.Collapsed : Visibility.Visible;
            NoSelectionText.Visibility = action is null ? Visibility.Visible : Visibility.Collapsed;
            ActionPreview.Clear();
            PreviewHintBanner.Visibility = Visibility.Visible;
            PathStatus.Text = string.Empty;

            if (action is null) return;

            ActionTypeText.Text = action switch
            {
                ImageAction => "Image action",
                WebsiteAction => "Website action",
                VideoAction => "Video action",
                _ => "Action"
            };
            ActionNameBox.Text = action.Name;
            DurationBox.Value = action.Duration ?? double.NaN;
            PathBox.Text = GetPath(action);

            StretchBox.Visibility = action is WebsiteAction ? Visibility.Collapsed : Visibility.Visible;
            VideoPanel.Visibility = action is VideoAction ? Visibility.Visible : Visibility.Collapsed;
            MuteCheck.Visibility = action is VideoAction ? Visibility.Visible : Visibility.Collapsed;
            WebsitePanel.Visibility = action is WebsiteAction ? Visibility.Visible : Visibility.Collapsed;

            switch (action)
            {
                case ImageAction image:
                    StretchBox.SelectedItem = image.Stretch.ToString();
                    break;
                case VideoAction video:
                    StretchBox.SelectedItem = video.Stretch.ToString();
                    PlayModeBox.SelectedItem = video.PlayMode.ToString();
                    MuteCheck.IsChecked = video.IsMuted;
                    DurationBox.IsEnabled = video.PlayMode == VideoPlayMode.DurationLoop;
                    break;
                case WebsiteAction website:
                    AutoScrollCheck.IsChecked = website.AutoScroll;
                    ScrollingStartDelayBox.Value = website.ScrollingStartDelay;
                    ScrollSpeedBox.Value = website.ScrollSpeed;
                    SettingsDisplayTimeBox.Value = website.SettingsDisplayTime;
                    break;
            }

            if (action is not VideoAction)
                DurationBox.IsEnabled = true;
        }
        finally
        {
            _loading = false;
        }
    }

    private void ActionNameBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || ActionList.SelectedItem is not EditorActionItem item) return;
        item.Action.Name = ActionNameBox.Text;
        item.Refresh();
        MarkDirty();
    }

    private void DurationBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_loading || SelectedAction is not { } action) return;
        action.Duration = double.IsNaN(sender.Value) ? null : (int)sender.Value;
        MarkDirty();
    }

    private void PathBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_loading || SelectedAction is not { } action) return;
        SetPath(action, PathBox.Text);
        PathStatus.Text = string.Empty;
        MarkDirty();
    }

    private void StretchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || StretchBox.SelectedItem is not string value) return;
        var stretch = Enum.Parse<ImageStretch>(value);
        switch (SelectedAction)
        {
            case ImageAction image: image.Stretch = stretch; break;
            case VideoAction video: video.Stretch = stretch; break;
            default: return;
        }
        MarkDirty();
    }

    private void PlayModeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || SelectedAction is not VideoAction video || PlayModeBox.SelectedItem is not string value) return;
        video.PlayMode = Enum.Parse<VideoPlayMode>(value);
        DurationBox.IsEnabled = video.PlayMode == VideoPlayMode.DurationLoop;
        MarkDirty();
    }

    private void MuteCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_loading || SelectedAction is not VideoAction video) return;
        video.IsMuted = MuteCheck.IsChecked == true;
        MarkDirty();
    }

    private void AutoScrollCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_loading || SelectedAction is not WebsiteAction website) return;
        website.AutoScroll = AutoScrollCheck.IsChecked == true;
        MarkDirty();
    }

    private void WebsiteNumber_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_loading || SelectedAction is not WebsiteAction website) return;
        var value = double.IsNaN(sender.Value) ? 0 : (int)sender.Value;

        if (sender == ScrollingStartDelayBox) website.ScrollingStartDelay = value;
        else if (sender == SettingsDisplayTimeBox) website.SettingsDisplayTime = value;

        MarkDirty();
    }

    private void ScrollSpeedBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (_loading || SelectedAction is not WebsiteAction website) return;
        website.ScrollSpeed = double.IsNaN(sender.Value) ? 1.0 : sender.Value;
        MarkDirty();
    }

    private async void TestPath_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedAction is not { } action) return;

        var version = ++_testVersion;
        TestPathButton.IsEnabled = false;
        LoadPreviewButton.IsEnabled = false;
        PreviewHintBanner.Visibility = Visibility.Collapsed;
        PathStatus.Text = "Testing path...";
        PathStatus.ClearValue(TextBlock.ForegroundProperty);

        var error = await ActionPreview.ShowAsync(action);
        if (version != _testVersion) return;

        TestPathButton.IsEnabled = true;
        LoadPreviewButton.IsEnabled = true;
        if (error is null)
        {
            PathStatus.Text = "Path loaded successfully.";
            PathStatus.Foreground = new SolidColorBrush(Colors.ForestGreen);
        }
        else
        {
            PathStatus.Text = error;
            PathStatus.Foreground = new SolidColorBrush(Colors.OrangeRed);
        }
    }

    #endregion

    #region Dirty tracking and dialogs

    private void MarkDirty()
    {
        if (_loading || _restoring) return;
        RecordHistory();
        SetDirty(_historyIndex != _savedIndex);
    }

    private string Snapshot() => OrchestrationSerializer.SerializeJson(_orchestration);

    private void ResetHistory(bool startDirty)
    {
        _history.Clear();
        _history.Add(Snapshot());
        _historyIndex = 0;
        _savedIndex = startDirty ? -1 : 0;
        _lastRecordTick = 0;
        UpdateUndoRedoButtons();
    }

    private void RecordHistory()
    {
        var json = Snapshot();
        if (_history[_historyIndex] == json) return;

        var now = Environment.TickCount64;
        var selected = ActionList.SelectedIndex;

        // Merge rapid edits to the same action (such as typing) into one undo step.
        var coalesce = _historyIndex == _history.Count - 1
                       && _historyIndex != _savedIndex
                       && _historyIndex > 0
                       && selected == _lastRecordSelected
                       && now - _lastRecordTick < CoalesceTicks;

        if (coalesce)
        {
            _history[_historyIndex] = json;
        }
        else
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
            _history.Add(json);
            _historyIndex++;

            if (_history.Count > MaxHistory)
            {
                _history.RemoveAt(0);
                _historyIndex--;
                _savedIndex--;
            }
        }

        _lastRecordTick = now;
        _lastRecordSelected = selected;
        UpdateUndoRedoButtons();
    }

    private void UpdateUndoRedoButtons()
    {
        UndoButton.IsEnabled = _historyIndex > 0;
        RedoButton.IsEnabled = _historyIndex < _history.Count - 1;
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e) => StepHistory(-1);

    private void RedoButton_Click(object sender, RoutedEventArgs e) => StepHistory(1);

    private void StepHistory(int delta)
    {
        var target = _historyIndex + delta;
        if (target < 0 || target >= _history.Count) return;

        var restored = OrchestrationSerializer.DeserializeJson(_history[target]);
        if (restored is null) return;

        _historyIndex = target;
        _lastRecordTick = 0;
        RestoreOrchestration(restored);
        UpdateUndoRedoButtons();
        SetDirty(_historyIndex != _savedIndex);
    }

    private void RestoreOrchestration(Orchestration restored)
    {
        var selectedIndex = ActionList.SelectedIndex;
        _restoring = true;
        try
        {
            restored.Source = _orchestration.Source;
            restored.SourcePath = _orchestration.SourcePath;
            _orchestration = restored;

            NameBox.Text = restored.Name;
            VersionText.Text = $"Format version: {restored.Version}";
            LifecycleBox.SelectedItem = restored.Lifecycle.ToString();
            OrderBox.SelectedItem = restored.Order.ToString();

            _items.Clear();
            foreach (var action in restored.Actions)
                _items.Add(new EditorActionItem(action));

            if (_items.Count > 0)
                ActionList.SelectedIndex = Math.Clamp(selectedIndex, 0, _items.Count - 1);
            else
                LoadActionForm(null);
        }
        finally
        {
            _restoring = false;
        }

        _lastRecordSelected = ActionList.SelectedIndex;
    }

    private void SetDirty(bool dirty)
    {
        _dirty = dirty;
        Title = $"{(dirty ? "* " : string.Empty)}{_file?.Name ?? "Untitled"} - Orchestration Editor";
    }

    private async Task<bool> ConfirmDiscardAsync()
    {
        if (!_dirty) return true;

        var dialog = new ContentDialog
        {
            XamlRoot = Root.XamlRoot,
            Title = "Unsaved changes",
            Content = "Do you want to save your changes first?",
            PrimaryButtonText = "Save",
            SecondaryButtonText = "Don't Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
            return await SaveAsync(false);
        return result == ContentDialogResult.Secondary;
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = Root.XamlRoot,
            Title = title,
            Content = message,
            CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose || !_dirty) return;

        args.Cancel = true;
        if (await ConfirmDiscardAsync())
        {
            _allowClose = true;
            Close();
        }
    }

    #endregion
}
