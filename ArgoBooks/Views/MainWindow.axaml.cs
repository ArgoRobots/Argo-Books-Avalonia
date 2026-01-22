using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.Controls;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.ViewModels;
using System.ComponentModel;

namespace ArgoBooks.Views;

/// <summary>
/// The main application window with custom chrome and state persistence.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Subscribe to DataContext changes to ensure content is set
        DataContextChanged += OnDataContextChanged;

        // Set up window drag behavior for custom title bar
        var dragRegion = this.FindControl<Border>("DragRegion");
        var titleBar = this.FindControl<Border>("TitleBar");

        if (titleBar != null)
        {
            titleBar.PointerPressed += OnTitleBarPointerPressed;
        }

        if (dragRegion != null)
        {
            dragRegion.PointerPressed += OnTitleBarPointerPressed;
        }

        // Hook up modal overlay to services
        var modalOverlay = this.FindControl<ModalOverlay>("ModalOverlay");
        if (modalOverlay != null)
        {
            // Get or create modal service
            ModalService = new ModalService();
            ModalService.SetOverlay(modalOverlay);

            MessageBoxService = new MessageBoxService();
            MessageBoxService.SetOverlay(modalOverlay);
        }

        // Subscribe to window events for state persistence
        Opened += OnWindowOpened;
        Closing += OnWindowClosing;
        PositionChanged += OnPositionChanged;
    }

    /// <summary>
    /// Gets the modal service for this window.
    /// </summary>
    public ModalService? ModalService { get; }

    /// <summary>
    /// Gets the message box service for this window.
    /// </summary>
    public MessageBoxService? MessageBoxService { get; }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // Double-click to maximize/restore
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                // Single click to start drag
                BeginMoveDrag(e);
            }
        }
    }

    private void OnWindowOpened(object? sender, EventArgs e)
    {
        // Restore window position if saved
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.LoadWindowState();

            // Apply saved position if valid
            if (viewModel is { WindowLeft: >= 0, WindowTop: >= 0 })
            {
                var screens = Screens;
                var savedBounds = new PixelRect(
                    (int)viewModel.WindowLeft,
                    (int)viewModel.WindowTop,
                    (int)viewModel.WindowWidth,
                    (int)viewModel.WindowHeight);

                // Check if saved position is visible on any screen
                bool isVisible = false;
                foreach (var screen in screens.All)
                {
                    if (screen.WorkingArea.Intersects(savedBounds))
                    {
                        isVisible = true;
                        break;
                    }
                }

                if (isVisible)
                {
                    Position = new PixelPoint((int)viewModel.WindowLeft, (int)viewModel.WindowTop);
                }
            }
        }
    }

    private bool _isClosingConfirmed;

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // If we've already confirmed closing, just save window state and close
        if (_isClosingConfirmed)
        {
            SaveWindowState();
            // End telemetry session and wait for upload to complete
            if (App.TelemetryManager != null)
            {
                await App.TelemetryManager.EndSessionAsync();
            }
            return;
        }

        // Check for unsaved changes in the reports page first
        if (App.HasReportsPageUnsavedChanges)
        {
            e.Cancel = true;

            var shouldContinue = await App.ConfirmReportsUnsavedChangesAsync();
            if (!shouldContinue)
            {
                return; // User cancelled, don't close
            }

            // User confirmed, check for other unsaved changes before closing
            var hasAppUnsavedChanges = App.UndoRedoManager?.IsAtSavedState == false;
            if (!hasAppUnsavedChanges)
            {
                // No other unsaved changes, close the window
                _isClosingConfirmed = true;
                Close();
                return;
            }
            // Fall through to handle app-level unsaved changes
        }

        // Check for unsaved changes - use UndoRedoManager's saved state tracking
        // which correctly handles undo back to original state
        var hasUnsavedChanges = App.UndoRedoManager?.IsAtSavedState == false;
        if (hasUnsavedChanges)
        {
            // Cancel the close event to show dialog
            e.Cancel = true;

            // Show unsaved changes dialog
            if (DataContext is MainWindowViewModel { UnsavedChangesDialogViewModel: not null } viewModel)
            {
                var result = await viewModel.UnsavedChangesDialogViewModel.ShowSimpleAsync(
                    "Unsaved Changes".Translate(),
                    "You have unsaved changes. Would you like to save them before closing?".Translate());

                switch (result)
                {
                    case UnsavedChangesResult.Save:
                        // Save and close
                        if (App.CompanyManager != null)
                        {
                            // Sample company cannot be saved directly - redirect to Save As
                            if (App.CompanyManager.IsSampleCompany)
                            {
                                var saved = await App.SaveCompanyAsFromWindowAsync();
                                if (!saved) return; // User cancelled Save As, don't close
                            }
                            else
                            {
                                await App.CompanyManager.SaveCompanyAsync();
                            }
                        }
                        _isClosingConfirmed = true;
                        Close();
                        break;

                    case UnsavedChangesResult.DontSave:
                        // Close without saving
                        _isClosingConfirmed = true;
                        Close();
                        break;

                    case UnsavedChangesResult.Cancel:
                    case UnsavedChangesResult.None:
                        // Don't close
                        break;
                }
            }
        }
        else
        {
            // No unsaved changes, but we need to wait for telemetry upload to complete
            // Cancel close, do async work, then close
            e.Cancel = true;
            SaveWindowState();
            if (App.TelemetryManager != null)
            {
                await App.TelemetryManager.EndSessionAsync();
            }
            _isClosingConfirmed = true;
            Close();
        }
    }

    private void SaveWindowState()
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Only save position if not maximized
            if (WindowState == WindowState.Normal)
            {
                viewModel.WindowLeft = Position.X;
                viewModel.WindowTop = Position.Y;
            }

            viewModel.SaveWindowState();
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        // Update position in view model when window moves (if not maximized)
        if (DataContext is MainWindowViewModel viewModel && WindowState == WindowState.Normal)
        {
            viewModel.WindowLeft = e.Point.X;
            viewModel.WindowTop = e.Point.Y;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Manually set the content when DataContext changes to work around binding timing issues
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Subscribe to property changes to update content when CurrentView changes
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Set initial content
            UpdateMainContent(viewModel.CurrentView);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.CurrentView) && DataContext is MainWindowViewModel viewModel)
        {
            UpdateMainContent(viewModel.CurrentView);
        }
    }

    private void UpdateMainContent(object? content)
    {
        var contentControl = this.FindControl<ContentControl>("MainContent");
        if (contentControl != null && content != null)
        {
            contentControl.Content = content;
        }
    }
}
