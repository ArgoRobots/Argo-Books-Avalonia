using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

public partial class PaginationFooter : UserControl
{
    public static readonly StyledProperty<string> PaginationTextProperty =
        AvaloniaProperty.Register<PaginationFooter, string>(nameof(PaginationText), "0 items");

    public static readonly StyledProperty<ObservableCollection<int>> PageSizeOptionsProperty =
        AvaloniaProperty.Register<PaginationFooter, ObservableCollection<int>>(nameof(PageSizeOptions));

    public static readonly StyledProperty<int> PageSizeProperty =
        AvaloniaProperty.Register<PaginationFooter, int>(nameof(PageSize), 10);

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<PaginationFooter, int>(nameof(CurrentPage), 1);

    public static readonly StyledProperty<int> TotalPagesProperty =
        AvaloniaProperty.Register<PaginationFooter, int>(nameof(TotalPages), 1);

    public static readonly StyledProperty<ObservableCollection<int>> PageNumbersProperty =
        AvaloniaProperty.Register<PaginationFooter, ObservableCollection<int>>(nameof(PageNumbers));

    public static readonly StyledProperty<bool> CanGoToPreviousPageProperty =
        AvaloniaProperty.Register<PaginationFooter, bool>(nameof(CanGoToPreviousPage), false);

    public static readonly StyledProperty<bool> CanGoToNextPageProperty =
        AvaloniaProperty.Register<PaginationFooter, bool>(nameof(CanGoToNextPage), false);

    public static readonly StyledProperty<ICommand?> GoToPreviousPageCommandProperty =
        AvaloniaProperty.Register<PaginationFooter, ICommand?>(nameof(GoToPreviousPageCommand));

    public static readonly StyledProperty<ICommand?> GoToNextPageCommandProperty =
        AvaloniaProperty.Register<PaginationFooter, ICommand?>(nameof(GoToNextPageCommand));

    public static readonly StyledProperty<ICommand?> GoToPageCommandProperty =
        AvaloniaProperty.Register<PaginationFooter, ICommand?>(nameof(GoToPageCommand));

    public string PaginationText
    {
        get => GetValue(PaginationTextProperty);
        set => SetValue(PaginationTextProperty, value);
    }

    public ObservableCollection<int> PageSizeOptions
    {
        get => GetValue(PageSizeOptionsProperty);
        set => SetValue(PageSizeOptionsProperty, value);
    }

    public int PageSize
    {
        get => GetValue(PageSizeProperty);
        set => SetValue(PageSizeProperty, value);
    }

    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int TotalPages
    {
        get => GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public ObservableCollection<int> PageNumbers
    {
        get => GetValue(PageNumbersProperty);
        set => SetValue(PageNumbersProperty, value);
    }

    public bool CanGoToPreviousPage
    {
        get => GetValue(CanGoToPreviousPageProperty);
        set => SetValue(CanGoToPreviousPageProperty, value);
    }

    public bool CanGoToNextPage
    {
        get => GetValue(CanGoToNextPageProperty);
        set => SetValue(CanGoToNextPageProperty, value);
    }

    public ICommand? GoToPreviousPageCommand
    {
        get => GetValue(GoToPreviousPageCommandProperty);
        set => SetValue(GoToPreviousPageCommandProperty, value);
    }

    public ICommand? GoToNextPageCommand
    {
        get => GetValue(GoToNextPageCommandProperty);
        set => SetValue(GoToNextPageCommandProperty, value);
    }

    public ICommand? GoToPageCommand
    {
        get => GetValue(GoToPageCommandProperty);
        set => SetValue(GoToPageCommandProperty, value);
    }

    public PaginationFooter()
    {
        InitializeComponent();
        PageSizeOptions = new ObservableCollection<int> { 10, 25, 50, 100 };
        PageNumbers = new ObservableCollection<int>();
    }
}
