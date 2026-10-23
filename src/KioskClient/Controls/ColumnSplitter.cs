using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace KioskClient.Controls;

/// <summary>
/// A draggable splitter that resizes the Grid column it sits in (or the next column when ResizeNext is true).
/// </summary>
public sealed partial class ColumnSplitter : Grid
{
    private double _startX;
    private double _startWidth;
    private bool _dragging;

    public double MinColumnWidth { get; set; } = 150;
    public bool ResizeNext { get; set; }

    public ColumnSplitter()
    {
        Width = 8;
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        Children.Add(new Border
        {
            Width = 1,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"]
        });
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerCanceled += OnPointerReleased;
    }

    private ColumnDefinition? Target()
    {
        if (Parent is not Grid grid) return null;
        int index = Grid.GetColumn(this) + (ResizeNext ? 1 : -1);
        if (index < 0 || index >= grid.ColumnDefinitions.Count) return null;
        return grid.ColumnDefinitions[index];
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var target = Target();
        if (target is null) return;
        _dragging = true;
        _startX = e.GetCurrentPoint((UIElement)Parent).Position.X;
        _startWidth = target.ActualWidth;
        CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging) return;
        var target = Target();
        if (target is null) return;
        double delta = e.GetCurrentPoint((UIElement)Parent).Position.X - _startX;
        double width = ResizeNext ? _startWidth - delta : _startWidth + delta;
        target.Width = new GridLength(System.Math.Max(MinColumnWidth, width));
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }
}
