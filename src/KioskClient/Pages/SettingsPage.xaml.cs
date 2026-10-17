using KioskClient.Core.Models;
using KioskClient.Core.Serialization;
using KioskClient.Dialogs;
using KioskClient.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace KioskClient.Pages;

/// <summary>
/// Settings page with NavigationView and Pivot tabs for orchestration management,
/// validation results, logging, and application settings.
/// </summary>
public sealed partial class SettingsPage : Page
{
    private DispatcherTimer? _startupRetryTimer;
    private int _startupRetryCountdown;

    public SettingsViewModel ViewModel => App.SettingsVM;

    public SettingsPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateValidationDisplay();
        _ = UpdatePreviewAsync();

        if (ViewModel.ShouldAutoRetryStart)
        {
            ViewModel.ShouldAutoRetryStart = false;
            StartStartupRetry();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        StopStartupRetry();
        ViewModel.SaveState();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.OrchestrationValidationResult))
        {
            if (ViewModel.OrchestrationValidationResult is null)
                ClearValidationDisplay();
            else
                UpdateValidationDisplay();
        }
        else if (e.PropertyName == nameof(ViewModel.Orchestration))
        {
            _previewIndex = 0;
            _ = UpdatePreviewAsync();
        }
        else if (e.PropertyName == nameof(ViewModel.LoadError))
        {
            LoadErrorBar.Message = ViewModel.LoadError ?? string.Empty;
            LoadErrorBar.IsOpen = ViewModel.LoadError is not null;
        }
    }

    private async void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is not NavigationViewItem item) return;

        switch (item.Tag?.ToString())
        {
            case "Run":
                RunOrchestration();
                break;
            case "Reset":
                ResetSettings();
                break;
            case "Examples":
                await ShowExamplesDialogAsync();
                break;
            case "Tutorial":
                await ShowTutorialDialogAsync();
                break;
            case "Help":
                await Windows.System.Launcher.LaunchUriAsync(
                    new Uri("https://github.com/CityOfStanton/Kiosk-Client/wiki"));
                break;
            case "About":
                await ShowAboutDialogAsync();
                break;
        }
    }

    private void Page_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.F5)
        {
            RunOrchestration();
        }
        else if (e.Key == Windows.System.VirtualKey.R &&
                 Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                     .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
        {
            RunOrchestration();
        }
    }

    private void RunOrchestration()
    {
        if (ViewModel.Orchestration is null || !ViewModel.CanStart) return;

        ViewModel.AddLog("Starting orchestration...");
        Frame.Navigate(typeof(OrchestrationPage), ViewModel.Orchestration);
    }

    private void ResetSettings()
    {
        ViewModel.ResetCommand.Execute(null);
    }

    private async Task ShowExamplesDialogAsync()
    {
        var dialog = new ExamplesDialog { XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }

    private async Task ShowTutorialDialogAsync()
    {
        var dialog = new TutorialDialog { XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }

    private async Task ShowAboutDialogAsync()
    {
        var dialog = new AboutDialog { XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }

    private async void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".xml");

        // Initialize the picker with the window handle
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            ClearValidationDisplay();
            await ViewModel.LoadFromFileCommand.ExecuteAsync(file.Path);
            UpdateValidationDisplay();
            if (!ViewModel.CanStart)
                NavigateToValidationTab();
        }
    }

    private async void UrlAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.UriPath = args.QueryText;
        if (ViewModel.LoadFromUriCommand.CanExecute(null))
        {
            ClearValidationDisplay();
            await ViewModel.LoadFromUriCommand.ExecuteAsync(null);
            UpdateValidationDisplay();
            if (!ViewModel.CanStart)
                NavigateToValidationTab();
        }
    }

    private void UrlAutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        ViewModel.UriPath = args.SelectedItem as string ?? string.Empty;
    }

    private void UrlAutoSuggestBox_Loaded(object sender, RoutedEventArgs e)
    {
        // Hide the built-in clear ("x") button; history is managed via the Settings tab.
        if (sender is not DependencyObject root) return;
        var clearButton = FindDescendant<Button>(root, "DeleteButton");
        if (clearButton is null) return;
        clearButton.Opacity = 0;
        clearButton.IsHitTestVisible = false;
        clearButton.IsTabStop = false;
    }

    private static T? FindDescendant<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T match && match.Name == name) return match;
            var result = FindDescendant<T>(child, name);
            if (result is not null) return result;
        }
        return null;
    }

    private int _previewVersion;
    private int _previewIndex;

    private void PreviewPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_previewIndex > 0)
        {
            _previewIndex--;
            _ = UpdatePreviewAsync();
        }
    }

    private void PreviewNext_Click(object sender, RoutedEventArgs e)
    {
        var count = ViewModel.Orchestration?.Actions.Count ?? 0;
        if (_previewIndex < count - 1)
        {
            _previewIndex++;
            _ = UpdatePreviewAsync();
        }
    }

    private async Task UpdatePreviewAsync()
    {
        var version = ++_previewVersion;
        var orchestration = ViewModel.Orchestration;

        PreviewImage.Visibility = Visibility.Collapsed;
        PreviewImage.Source = null;
        PreviewWeb.Visibility = Visibility.Collapsed;
        if (PreviewVideo.MediaPlayer is { } oldPlayer)
        {
            PreviewVideo.SetMediaPlayer(null);
            oldPlayer.Dispose();
        }
        PreviewVideo.Visibility = Visibility.Collapsed;
        PreviewMessage.Visibility = Visibility.Collapsed;
        PreviewProgress.IsActive = false;
        try { PreviewWeb.Source = new Uri("about:blank"); } catch { }

        var total = orchestration?.Actions.Count ?? 0;
        _previewIndex = Math.Clamp(_previewIndex, 0, Math.Max(total - 1, 0));

        var action = orchestration?.Actions.ElementAtOrDefault(_previewIndex);
        if (orchestration is null || action is null)
        {
            PreviewCaption.Text = string.Empty;
            PreviewNavPanel.Visibility = Visibility.Collapsed;
            return;
        }

        PreviewNavPanel.Visibility = total > 1 ? Visibility.Visible : Visibility.Collapsed;
        PreviewPositionText.Text = $"{_previewIndex + 1} / {total}";
        PreviewPrevButton.IsEnabled = _previewIndex > 0;
        PreviewNextButton.IsEnabled = _previewIndex < total - 1;

        PreviewCaption.Text = $"Preview: {action.Name}";

        try
        {
            PreviewProgress.IsActive = true;

            switch (action)
            {
                case ImageAction image:
                    var bitmap = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                    if (Uri.TryCreate(image.Path, UriKind.Absolute, out var imageUri))
                    {
                        bitmap.UriSource = imageUri;
                    }
                    else
                    {
                        var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(image.Path);
                        using var stream = await file.OpenReadAsync();
                        await bitmap.SetSourceAsync(stream);
                    }
                    if (version != _previewVersion) return;
                    PreviewImage.Source = bitmap;
                    PreviewImage.Visibility = Visibility.Visible;
                    break;

                case WebsiteAction website:
                    if (version != _previewVersion) return;
                    await PreviewWeb.EnsureCoreWebView2Async();
                    PreviewWeb.CoreWebView2.IsMuted = true;
                    if (version != _previewVersion) return;
                    PreviewWeb.Source = new Uri(website.Path);
                    PreviewWeb.Visibility = Visibility.Visible;
                    break;

                case VideoAction video:
                    if (!Uri.TryCreate(video.Path, UriKind.Absolute, out var videoUri))
                        throw new UriFormatException("The video path is not a valid URL or file path.");
                    if (version != _previewVersion) return;
                    var player = new Windows.Media.Playback.MediaPlayer
                    {
                        Source = Windows.Media.Core.MediaSource.CreateFromUri(videoUri),
                        IsMuted = true,
                        IsLoopingEnabled = true,
                        AutoPlay = true
                    };
                    PreviewVideo.SetMediaPlayer(player);
                    PreviewVideo.Visibility = Visibility.Visible;
                    break;

                default:
                    PreviewMessage.Text = "Preview not available for this action type.";
                    PreviewMessage.Visibility = Visibility.Visible;
                    break;
            }
        }
        catch (Exception ex)
        {
            if (version != _previewVersion) return;
            PreviewMessage.Text = $"Unable to load preview: {ex.Message}";
            PreviewMessage.Visibility = Visibility.Visible;
        }
        finally
        {
            if (version == _previewVersion)
                PreviewProgress.IsActive = false;
        }
    }

    private void UrlHistoryDropDown_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.UrlHistory.Count == 0) return;
        UrlAutoSuggestBox.Focus(FocusState.Programmatic);
        UrlAutoSuggestBox.IsSuggestionListOpen = true;
    }

    private void DeleteHistoryEntry_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string url })
            ViewModel.RemoveFromUrlHistory(url);
    }

    private void NavigateToValidationTab()
    {
        ValidationExpander.IsExpanded = true;
    }

    private void ClearValidationDisplay()
    {
        PassedCount.Text = string.Empty;
        FailedCount.Text = string.Empty;
        PassedCount.ClearValue(TextBlock.ForegroundProperty);
        FailedCount.ClearValue(TextBlock.ForegroundProperty);
        PassedIcon.ClearValue(IconElement.ForegroundProperty);
        FailedIcon.ClearValue(IconElement.ForegroundProperty);
        ValidationTree.RootNodes.Clear();
        ValidationExpander.IsExpanded = false;
    }

    private void UpdateValidationDisplay()
    {
        var result = ViewModel.OrchestrationValidationResult;
        if (result is not null)
        {
            ValidationExpander.IsExpanded = true;

            var passed = result.PassedCount;
            var failed = result.FailedCount;

            PassedCount.Text = $"Passed: {passed}";
            FailedCount.Text = $"Failed: {failed}";

            var greenBrush = (Brush)Application.Current.Resources["KioskGreenBrush"];
            var redBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);

            if (passed > 0)
            {
                PassedCount.Foreground = greenBrush;
                PassedIcon.Foreground = greenBrush;
            }
            else
            {
                PassedCount.ClearValue(TextBlock.ForegroundProperty);
                PassedIcon.ClearValue(IconElement.ForegroundProperty);
            }

            if (failed > 0)
            {
                FailedCount.Foreground = redBrush;
                FailedIcon.Foreground = redBrush;
            }
            else
            {
                FailedCount.ClearValue(TextBlock.ForegroundProperty);
                FailedIcon.ClearValue(IconElement.ForegroundProperty);
            }

            BuildValidationTree(result);
        }
    }

    private void BuildValidationTree(ValidationResult root)
    {
        ValidationTree.RootNodes.Clear();
        var rootNode = CreateTreeNode(root);
        ValidationTree.RootNodes.Add(rootNode);
    }

    private sealed class ValidationNodeItem
    {
        public string Glyph { get; init; } = string.Empty;
        public Brush IconForeground { get; init; } = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        public string Label { get; init; } = string.Empty;
    }

    private static TreeViewNode CreateTreeNode(ValidationResult result)
    {
        var isValid = result.IsValid == true;

        var greenBrush = (Brush)Application.Current.Resources["KioskGreenBrush"];
        var redBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);

        var node = new TreeViewNode
        {
            Content = new ValidationNodeItem
            {
                Glyph = isValid ? "\uE73E" : "\uE711",
                IconForeground = isValid ? greenBrush : redBrush,
                Label = $"{result.Identifier}: {result.Message}",
            },
            IsExpanded = true,
        };

        foreach (var child in result.Children)
            node.Children.Add(CreateTreeNode(child));

        return node;
    }

    private void StartStartupRetry()
    {
        _startupRetryCountdown = ViewModel.AutoRetrySeconds;
        UpdateRetryBanner();

        StartupRetryBanner.IsOpen = true;
        ViewModel.IsAutoRetryActive = true;

        _startupRetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _startupRetryTimer.Tick += StartupRetryTimer_Tick;
        _startupRetryTimer.Start();
    }

    private async void StartupRetryTimer_Tick(object? sender, object e)
    {
        _startupRetryCountdown--;
        UpdateRetryBanner();

        if (_startupRetryCountdown > 0) return;

        StopStartupRetry();
        ViewModel.AddLog("Auto-retry: attempting to reload orchestration...");

        var result = await ViewModel.TryAutoStartAsync();
        UpdateValidationDisplay();

        if (result == StartupLoadResult.LoadedAndValid)
        {
            ViewModel.AddLog("Auto-retry succeeded. Starting orchestration.");
            Frame.Navigate(typeof(OrchestrationPage), ViewModel.Orchestration);
        }
        else
        {
            ViewModel.AddLog("Auto-retry failed. Restarting countdown.");
            StartStartupRetry();
        }
    }

    private void StopStartupRetry()
    {
        _startupRetryTimer?.Stop();
        _startupRetryTimer = null;
        StartupRetryBanner.IsOpen = false;
        ViewModel.IsAutoRetryActive = false;
    }

    private void UpdateRetryBanner()
    {
        ViewModel.CurrentAutoRetryCountdown = _startupRetryCountdown;
        StartupRetryBanner.Message =
            $"Failed to load the previous orchestration. Retrying in {_startupRetryCountdown}s...";
    }

    private void StopStartupRetry_Click(object sender, RoutedEventArgs e)
    {
        StopStartupRetry();
        ViewModel.AddLog("Startup auto-retry stopped.");
    }

    private void CopyLog_Click(object sender, RoutedEventArgs e)
    {
        var text = string.Join(Environment.NewLine, ViewModel.LogEntries);
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.LogEntries.Clear();
    }
}
