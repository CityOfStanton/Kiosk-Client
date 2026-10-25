using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace KioskClient.Services;

/// <summary>Which tutorial path the user wants to follow.</summary>
public enum TutorialChoice
{
    Create,
    Run
}

/// <summary>A single step of a guided tour. A null target shows the tip unanchored.</summary>
public sealed record TutorialStep(FrameworkElement? Target, string Title, string Text);

/// <summary>
/// Walks the user through a list of steps using a single TeachingTip.
/// </summary>
public static class TutorialGuide
{
    public static void Run(TeachingTip tip, IReadOnlyList<TutorialStep> steps)
    {
        var index = -1;

        tip.IsLightDismissEnabled = false;
        tip.CloseButtonContent = "End tutorial";

        void Cleanup()
        {
            tip.ActionButtonClick -= OnNext;
            tip.CloseButtonClick -= OnClose;
            tip.IsOpen = false;
        }

        void OnClose(TeachingTip s, object a) => Cleanup();

        void OnNext(TeachingTip s, object a) => Advance();

        void Advance()
        {
            index++;
            while (index < steps.Count && steps[index].Target is { } t && (t.ActualWidth == 0 || t.Visibility != Visibility.Visible))
                index++;

            if (index >= steps.Count)
            {
                Cleanup();
                return;
            }

            if (tip.IsOpen)
            {
                void OnClosed(TeachingTip s, TeachingTipClosedEventArgs a)
                {
                    tip.Closed -= OnClosed;
                    ShowCurrent();
                }
                tip.Closed += OnClosed;
                tip.IsOpen = false;
            }
            else
            {
                ShowCurrent();
            }
        }

        void ShowCurrent()
        {
            var step = steps[index];
            tip.Target = null;
            tip.Target = step.Target;
            tip.Title = $"{step.Title} ({index + 1}/{steps.Count})";
            tip.Subtitle = step.Text;
            tip.ActionButtonContent = index == steps.Count - 1 ? "Finish" : "Next";
            tip.IsOpen = true;
        }

        tip.ActionButtonClick += OnNext;
        tip.CloseButtonClick += OnClose;
        Advance();
    }
}
