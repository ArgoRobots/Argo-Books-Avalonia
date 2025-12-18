using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.Controls;
using ArgoBooks.Services;
using ArgoBooks.ViewModels;
using System.ComponentModel;

namespace ArgoBooks.Views;

/// <summary>
/// The main application window with custom chrome and state persistence.
/// </summary>
public partial class MainWindow : Window
{
    private readonly ModalService? _modalService;
    private readonly MessageBoxService? _messageBoxService;

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
            _modalService = new ModalService();
            _modalService.SetOverlay(modalOverlay);

            _messageBoxService = new MessageBoxService();
            _messageBoxService.SetOverlay(modalOverlay);
        }

        // Subscribe to window events for state persistence
        this.Opened += OnWindowOpened;
        this.Closing += OnWindowClosing;
        this.PositionChanged += OnPositionChanged;
    }

    /// <summary>
    /// Gets the modal service for this window.
    /// </summary>
    public ModalService? ModalService => _modalService;

    /// <summary>
    /// Gets the message box service for this window.
    /// </summary>
    public MessageBoxService? MessageBoxService => _messageBoxService;

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
            if (viewModel.WindowLeft >= 0 && viewModel.WindowTop >= 0)
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

    private bool _isClosingConfirmed = false;

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // If we've already confirmed closing, just save window state and close
        if (_isClosingConfirmed)
        {
            SaveWindowState();
            return;
        }

        // Check for unsaved changes - use UndoRedoManager's saved state tracking
        // which correctly handles undo back to original state
        var hasUnsavedChanges = App.UndoRedoManager?.IsAtSavedState == false;
        if (hasUnsavedChanges)
        {
            // Cancel the close event to show dialog
            e.Cancel = true;

            // Show unsaved changes dialog
            if (DataContext is MainWindowViewModel viewModel && viewModel.UnsavedChangesDialogViewModel != null)
            {
                var result = await viewModel.UnsavedChangesDialogViewModel.ShowSimpleAsync(
                    "Unsaved Changes",
                    "You have unsaved changes. Would you like to save them before closing?");

                switch (result)
                {
                    case UnsavedChangesResult.Save:
                        // Save and close
                        if (App.CompanyManager != null)
                        {
                            await App.CompanyManager.SaveCompanyAsync();
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
            // No unsaved changes, just save window state
            SaveWindowState();
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
