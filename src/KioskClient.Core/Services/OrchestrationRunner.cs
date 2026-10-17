using KioskClient.Core.Models;

namespace KioskClient.Core.Services;

/// <summary>
/// Event args for when it's time to display the next action.
/// </summary>
public class NextActionEventArgs : EventArgs
{
    public required ActionBase Action { get; init; }
    public int ActionIndex { get; init; }
    public int TotalActions { get; init; }
}

/// <summary>
/// Event args for orchestration status updates.
/// </summary>
public class OrchestrationStatusEventArgs : EventArgs
{
    public required string Message { get; init; }
    public bool IsError { get; init; }
}

/// <summary>
/// Manages the execution lifecycle of an orchestration, including action sequencing,
/// timing, looping, and background reload for URL-based orchestrations.
/// </summary>
public class OrchestrationRunner : IDisposable
{
    private const int PollLeadTimeSeconds = 15;

    private readonly IOrchestrationLoader _loader;
    private readonly IImageHashService _imageHasher;
    private string? _displayedImageKey;
    private string? _displayedImageHash;
    private CancellationTokenSource? _cts;
    private Orchestration? _currentOrchestration;
    private Orchestration? _nextOrchestration;
    private bool _isRunning;
    private volatile int _currentActionDuration;
    private TaskCompletionSource? _actionCompletion;

    public event EventHandler<NextActionEventArgs>? NextAction;
    /// <summary>Raised whenever an action's duration timer begins (even if the display was not reloaded). Argument is the duration in seconds.</summary>
    public event EventHandler<int>? ActionTimerStarted;
    public event EventHandler? OrchestrationStarted;
    public event EventHandler? OrchestrationCompleted;
    public event EventHandler? OrchestrationCancelled;
    public event EventHandler<OrchestrationStatusEventArgs>? StatusUpdate;
    public event EventHandler<bool>? NetworkStatusChanged;

    public bool IsRunning => _isRunning;
    public Orchestration? CurrentOrchestration => _currentOrchestration;

    public OrchestrationRunner(IOrchestrationLoader loader, IImageHashService? imageHasher = null)
    {
        _loader = loader;
        _imageHasher = imageHasher ?? new ImageHashService();
    }

    /// <summary>
    /// Starts running the specified orchestration.
    /// </summary>
    public async Task StartAsync(Orchestration orchestration)
    {
        Stop();

        _currentOrchestration = orchestration;
        _cts = new CancellationTokenSource();
        _isRunning = true;

        OrchestrationStarted?.Invoke(this, EventArgs.Empty);
        RaiseStatus("Orchestration started.");

        try
        {
            await RunLoopAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            OrchestrationCancelled?.Invoke(this, EventArgs.Empty);
            RaiseStatus("Orchestration cancelled.");
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// Stops the current orchestration.
    /// </summary>
    public void Stop()
    {
        if (_cts is not null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
        _isRunning = false;
    }

    /// <summary>
    /// Signals that the current action finished on its own (for example, a video that played to the end).
    /// Only affects actions that end on completion rather than on a duration.
    /// </summary>
    public void NotifyActionCompleted() => _actionCompletion?.TrySetResult();

    private async Task RunLoopAsync(CancellationToken ct)
    {
        if (_currentOrchestration is null) return;

        // Start a single persistent background polling loop for URL-based orchestrations
        var pollingTask = TryReloadInBackgroundAsync(ct);

        _displayedImageKey = null;
        _displayedImageHash = null;

        do
        {
            var actions = GetOrderedActions(_currentOrchestration);

            for (int i = 0; i < actions.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var action = actions[i];
                var endsWhenComplete = action is VideoAction { PlayMode: VideoPlayMode.PlayOnce };
                var duration = endsWhenComplete ? 0 : action.Duration ?? 10;
                _currentActionDuration = duration;

                _actionCompletion = endsWhenComplete
                    ? new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
                    : null;

                if (!await IsImageUnchangedAsync(action, ct))
                {
                    NextAction?.Invoke(this, new NextActionEventArgs
                    {
                        Action = action,
                        ActionIndex = i,
                        TotalActions = actions.Count
                    });
                }

                ActionTimerStarted?.Invoke(this, duration);

                var completion = _actionCompletion;
                if (completion is not null)
                {
                    using var registration = ct.Register(() => completion.TrySetCanceled(ct));
                    await completion.Task;
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(duration), ct);
                }
            }

            // Apply any update that arrived between cycles
            if (_nextOrchestration is not null)
            {
                _currentOrchestration = _nextOrchestration;
                _nextOrchestration = null;
                RaiseStatus("Orchestration reloaded with updated content.");
            }

        } while (_currentOrchestration.Lifecycle == LifecycleBehavior.ContinuousLoop);

        await pollingTask;

        OrchestrationCompleted?.Invoke(this, EventArgs.Empty);
        RaiseStatus("Orchestration completed.");
    }

    /// <summary>
    /// Returns true when the action is an image that is already on screen with identical content.
    /// Also records the image currently being displayed.
    /// </summary>
    private async Task<bool> IsImageUnchangedAsync(ActionBase action, CancellationToken ct)
    {
        if (action is not ImageAction image)
        {
            _displayedImageKey = null;
            _displayedImageHash = null;
            return false;
        }

        var key = $"{image.Path}|{image.Stretch}";
        var hash = await _imageHasher.ComputeHashAsync(image.Path, ct);

        if (hash is not null && key == _displayedImageKey && hash == _displayedImageHash)
        {
            RaiseStatus($"Image unchanged, not reloading: {image.Path}");
            return true;
        }

        _displayedImageKey = key;
        _displayedImageHash = hash;
        return false;
    }

    private List<ActionBase> GetOrderedActions(Orchestration orchestration)
    {
        var actions = orchestration.Actions.ToList();

        if (orchestration.Order == Ordering.Random)
        {
            var rng = Random.Shared;
            for (int i = actions.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (actions[i], actions[j]) = (actions[j], actions[i]);
            }
        }

        return actions;
    }

    private async Task TryReloadInBackgroundAsync(CancellationToken ct)
    {
        if (_currentOrchestration?.Source != OrchestrationSource.URL ||
            string.IsNullOrWhiteSpace(_currentOrchestration.SourcePath))
            return;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Derive the poll delay from the current action's duration:
                //   duration > 15s  → poll 15s before the action ends
                //   duration ≤ 15s  → poll at the halfway point
                var duration = _currentActionDuration > 0 ? _currentActionDuration : 10;
                var delaySeconds = duration > PollLeadTimeSeconds
                    ? duration - PollLeadTimeSeconds
                    : duration / 2.0;

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);
                ct.ThrowIfCancellationRequested();

                RaiseStatus("Polling: checking for orchestration updates...");
                var reloaded = await _loader.LoadFromUrlAsync(_currentOrchestration.SourcePath, ct);

                if (reloaded.ContentHash == _currentOrchestration.ContentHash)
                {
                    RaiseStatus("Polling: orchestration unchanged.");
                    NetworkStatusChanged?.Invoke(this, true);
                }
                else
                {
                    _nextOrchestration = reloaded;
                    NetworkStatusChanged?.Invoke(this, true);
                    RaiseStatus("Polling: orchestration changed, will apply at next cycle boundary.");
                }

                // Wait for the remainder of the action before polling again
                var remaining = Math.Max(duration - delaySeconds, 0);
                if (remaining > 0)
                    await Task.Delay(TimeSpan.FromSeconds(remaining), ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                NetworkStatusChanged?.Invoke(this, false);
                RaiseStatus($"Polling: failed to check for updates: {ex.Message}");

                // Back off for the current action duration before retrying
                var backoff = Math.Max(_currentActionDuration, 10);
                await Task.Delay(TimeSpan.FromSeconds(backoff), ct).ConfigureAwait(false);
            }
        }
    }

    private void RaiseStatus(string message, bool isError = false)
    {
        StatusUpdate?.Invoke(this, new OrchestrationStatusEventArgs
        {
            Message = message,
            IsError = isError
        });
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
