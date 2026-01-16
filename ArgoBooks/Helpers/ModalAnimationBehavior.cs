using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace ArgoBooks.Helpers;

/// <summary>
/// Attached behavior that automatically handles modal fade-in/scale animations.
///
/// Usage in XAML:
/// 1. Add the namespace: xmlns:helpers="using:ArgoBooks.Helpers"
/// 2. On the modal Border, add: helpers:ModalAnimationBehavior.IsEnabled="True"
/// 3. Optionally specify the property name: helpers:ModalAnimationBehavior.OpenPropertyName="IsAddModalOpen"
///    (defaults to "IsOpen")
///
/// The behavior will:
/// - Set initial Opacity=0 and Scale=0.95
/// - Subscribe to the ViewModel's specified property
/// - Animate in when property becomes true
/// - Animate out when property becomes false
/// </summary>
public static class ModalAnimationBehavior
{
    #region Attached Properties

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Border, bool>("IsEnabled", typeof(ModalAnimationBehavior));

    public static readonly AttachedProperty<string> OpenPropertyNameProperty =
        AvaloniaProperty.RegisterAttached<Border, string>("OpenPropertyName", typeof(ModalAnimationBehavior), "IsOpen");

    public static bool GetIsEnabled(Border element) => element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Border element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static string GetOpenPropertyName(Border element) => element.GetValue(OpenPropertyNameProperty);
    public static void SetOpenPropertyName(Border element, string value) => element.SetValue(OpenPropertyNameProperty, value);

    #endregion

    static ModalAnimationBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<Border>(OnIsEnabledChanged);
    }

    private static void OnIsEnabledChanged(Border border, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            // Set initial state
            border.Opacity = 0;
            border.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            border.RenderTransform = new ScaleTransform(0.95, 0.95);

            // Subscribe to DataContext changes to get ViewModel
            border.DataContextChanged += OnDataContextChanged;

            // If DataContext is already set, subscribe now
            if (border.DataContext is INotifyPropertyChanged vm)
            {
                SubscribeToViewModel(border, vm);
            }
        }
        else
        {
            border.DataContextChanged -= OnDataContextChanged;
        }
    }

    private static void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (sender is Border border && border.DataContext is INotifyPropertyChanged vm)
        {
            SubscribeToViewModel(border, vm);
        }
    }

    private static readonly Dictionary<Border, PropertyChangedEventHandler> _handlers = new();

    private static void SubscribeToViewModel(Border border, INotifyPropertyChanged vm)
    {
        // Remove old handler if exists
        if (_handlers.TryGetValue(border, out var oldHandler))
        {
            vm.PropertyChanged -= oldHandler;
            _handlers.Remove(border);
        }

        var propertyName = GetOpenPropertyName(border);

        PropertyChangedEventHandler handler = (sender, args) =>
        {
            if (args.PropertyName == propertyName)
            {
                var prop = sender?.GetType().GetProperty(propertyName);
                if (prop?.GetValue(sender) is bool isOpen)
                {
                    if (isOpen)
                        AnimateIn(border);
                    else
                        AnimateOut(border);
                }
            }
        };

        vm.PropertyChanged += handler;
        _handlers[border] = handler;

        // Check initial state
        var initialProp = vm.GetType().GetProperty(propertyName);
        if (initialProp?.GetValue(vm) is true)
        {
            AnimateIn(border);
        }
    }

    private static void AnimateIn(Border border)
    {
        Dispatcher.UIThread.Post(() =>
        {
            border.Opacity = 1;
            border.RenderTransform = new ScaleTransform(1, 1);
            border.Focus();
        }, DispatcherPriority.Render);
    }

    private static void AnimateOut(Border border)
    {
        Dispatcher.UIThread.Post(() =>
        {
            border.Opacity = 0;
            border.RenderTransform = new ScaleTransform(0.95, 0.95);
        }, DispatcherPriority.Background);
    }
}
