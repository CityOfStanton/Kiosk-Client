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
    private readonly IOrchestrationLoader _loader;
    private CancellationTokenSource? _cts;
    private Orchestration? _currentOrchestration;
    private Orchestration? _nextOrchestration;
    private bool _isRunning;

    public event EventHandler<NextActionEventArgs>? NextAction;
    public event EventHandler? OrchestrationStarted;
    public event EventHandler? OrchestrationCompleted;
    public event EventHandler? OrchestrationCancelled;
    public event EventHandler<OrchestrationStatusEventArgs>? StatusUpdate;
    public event EventHandler<bool>? NetworkStatusChanged;

    public bool IsRunning => _isRunning;
    public Orchestration? CurrentOrchestration => _currentOrchestration;

    public OrchestrationRunner(IOrchestrationLoader loader)
    {
        _loader = loader;
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

    private async Task RunLoopAsync(CancellationToken ct)
    {
        if (_currentOrchestration is null) return;

        do
        {
            // Start background reload for URL-based orchestrations
            _ = TryReloadInBackgroundAsync(ct);

            var actions = GetOrderedActions(_currentOrchestration);

            for (int i = 0; i < actions.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var action = actions[i];
                NextAction?.Invoke(this, new NextActionEventArgs
                {
                    Action = action,
                    ActionIndex = i,
                    TotalActions = actions.Count
                });

                var duration = action.Duration ?? 10;
                await Task.Delay(TimeSpan.FromSeconds(duration), ct);
            }

            // Check if a new orchestration was loaded in the background
            if (_nextOrchestration is not null)
            {
                _currentOrchestration = _nextOrchestration;
                _nextOrchestration = null;
                RaiseStatus("Orchestration reloaded with updated content.");
            }

        } while (_currentOrchestration.Lifecycle == LifecycleBehavior.ContinuousLoop);

        OrchestrationCompleted?.Invoke(this, EventArgs.Empty);
        RaiseStatus("Orchestration completed.");
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

        try
        {
            var delaySeconds = Math.Max(_currentOrchestration.PollingInterval, 60);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), ct);

            ct.ThrowIfCancellationRequested();

            RaiseStatus("Reloading orchestration from URL...");
            var reloaded = await _loader.LoadFromUrlAsync(_currentOrchestration.SourcePath, ct);
            _nextOrchestration = reloaded;
            NetworkStatusChanged?.Invoke(this, true);
            RaiseStatus("Orchestration reload complete.");
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            NetworkStatusChanged?.Invoke(this, false);
            RaiseStatus($"Failed to reload orchestration: {ex.Message}");
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
