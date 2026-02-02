using System.Collections.ObjectModel;
using ArgoBooks.Data;
using ArgoBooks.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Represents a tab in the emoji picker.
/// </summary>
public partial class EmojiTabItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string Icon { get; init; } = string.Empty;
    public bool IsSpecial { get; init; } = false;

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>
/// Represents an emoji item for display in the picker.
/// </summary>
public partial class EmojiDisplayItem : ObservableObject
{
    public string Emoji { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;

    [ObservableProperty]
    private bool _isFavorite;
}

/// <summary>
/// ViewModel for the emoji picker modal.
/// </summary>
public partial class EmojiPickerViewModel : ObservableObject
{
    private EmojiPickerSettings? _settings;
    private Action<string>? _onEmojiSelected;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private EmojiTabItem? _selectedTab;

    [ObservableProperty]
    private string? _selectedEmoji;

    public ObservableCollection<EmojiTabItem> Tabs { get; } = [];
    public ObservableCollection<EmojiDisplayItem> DisplayedEmojis { get; } = [];

    public bool HasRecentEmojis => _settings?.RecentEmojis.Count > 0;
    public bool HasFavoriteEmojis => _settings?.FavoriteEmojis.Count > 0;

    public EmojiPickerViewModel()
    {
        InitializeTabs();
    }

    private void InitializeTabs()
    {
        Tabs.Clear();

        // Special tabs first
        Tabs.Add(new EmojiTabItem { Name = "Recent", Icon = "🕐", IsSpecial = true });
        Tabs.Add(new EmojiTabItem { Name = "Favorites", Icon = "⭐", IsSpecial = true });

        // Category tabs
        foreach (var category in EmojiData.Categories)
        {
            Tabs.Add(new EmojiTabItem { Name = category.Name, Icon = category.Icon });
        }
    }

    /// <summary>
    /// Opens the emoji picker modal.
    /// </summary>
    /// <param name="currentEmoji">Currently selected emoji (if any).</param>
    /// <param name="onSelected">Callback when an emoji is selected.</param>
    public void Open(string? currentEmoji, Action<string> onSelected)
    {
        _onEmojiSelected = onSelected;
        _settings = App.SettingsService?.GlobalSettings?.Ui?.EmojiPicker ?? new EmojiPickerSettings();
        SelectedEmoji = currentEmoji;
        SearchText = string.Empty;

        // Refresh favorite states
        OnPropertyChanged(nameof(HasRecentEmojis));
        OnPropertyChanged(nameof(HasFavoriteEmojis));

        // Select first tab with content
        if (_settings.RecentEmojis.Count > 0)
        {
            SelectTab(Tabs.First(t => t.Name == "Recent"));
        }
        else if (_settings.FavoriteEmojis.Count > 0)
        {
            SelectTab(Tabs.First(t => t.Name == "Favorites"));
        }
        else
        {
            // Select first category tab (skip Recent and Favorites)
            SelectTab(Tabs.FirstOrDefault(t => !t.IsSpecial) ?? Tabs.First());
        }

        IsOpen = true;
    }

    [RelayCommand]
    public void Close()
    {
        IsOpen = false;
        _onEmojiSelected = null;
    }

    [RelayCommand]
    public void SelectTab(EmojiTabItem? tab)
    {
        if (tab == null) return;

        foreach (var t in Tabs)
        {
            t.IsSelected = t == tab;
        }

        SelectedTab = tab;
        UpdateDisplayedEmojis();
    }

    [RelayCommand]
    public void SelectEmoji(EmojiDisplayItem? item)
    {
        if (item == null) return;

        SelectedEmoji = item.Emoji;
        AddToRecent(item.Emoji);
        _onEmojiSelected?.Invoke(item.Emoji);
        Close();
    }

    [RelayCommand]
    public void ToggleFavorite(EmojiDisplayItem? item)
    {
        if (item == null || _settings == null) return;

        if (item.IsFavorite)
        {
            _settings.FavoriteEmojis.Remove(item.Emoji);
            item.IsFavorite = false;
        }
        else
        {
            if (!_settings.FavoriteEmojis.Contains(item.Emoji))
            {
                _settings.FavoriteEmojis.Insert(0, item.Emoji);
            }
            item.IsFavorite = true;
        }

        SaveSettings();
        OnPropertyChanged(nameof(HasFavoriteEmojis));

        // Refresh if we're on the Favorites tab
        if (SelectedTab?.Name == "Favorites")
        {
            UpdateDisplayedEmojis();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        UpdateDisplayedEmojis();
    }

    private void UpdateDisplayedEmojis()
    {
        DisplayedEmojis.Clear();

        if (_settings == null) return;

        IEnumerable<EmojiData.EmojiItem> emojis;

        // If searching, search all emojis
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            emojis = EmojiData.AllEmojis
                .Where(e => e.Name.Contains(searchLower, StringComparison.OrdinalIgnoreCase) ||
                            e.Emoji.Contains(searchLower));
        }
        else if (SelectedTab == null)
        {
            return;
        }
        else if (SelectedTab.Name == "Recent")
        {
            // Get recent emojis with their names
            var recentEmojis = _settings.RecentEmojis
                .Select(emoji => EmojiData.AllEmojis.FirstOrDefault(e => e.Emoji == emoji) ??
                                 new EmojiData.EmojiItem(emoji, ""))
                .ToList();
            emojis = recentEmojis;
        }
        else if (SelectedTab.Name == "Favorites")
        {
            // Get favorite emojis with their names
            var favoriteEmojis = _settings.FavoriteEmojis
                .Select(emoji => EmojiData.AllEmojis.FirstOrDefault(e => e.Emoji == emoji) ??
                                 new EmojiData.EmojiItem(emoji, ""))
                .ToList();
            emojis = favoriteEmojis;
        }
        else
        {
            // Find category
            var category = EmojiData.Categories.FirstOrDefault(c => c.Name == SelectedTab.Name);
            emojis = category?.Emojis ?? [];
        }

        foreach (var emoji in emojis)
        {
            DisplayedEmojis.Add(new EmojiDisplayItem
            {
                Emoji = emoji.Emoji,
                Name = emoji.Name,
                IsFavorite = _settings.FavoriteEmojis.Contains(emoji.Emoji)
            });
        }
    }

    private void AddToRecent(string emoji)
    {
        if (_settings == null) return;

        // Remove if already exists
        _settings.RecentEmojis.Remove(emoji);

        // Add to beginning
        _settings.RecentEmojis.Insert(0, emoji);

        // Trim to max size
        while (_settings.RecentEmojis.Count > _settings.MaxRecentEmojis)
        {
            _settings.RecentEmojis.RemoveAt(_settings.RecentEmojis.Count - 1);
        }

        SaveSettings();
        OnPropertyChanged(nameof(HasRecentEmojis));
    }

    private void SaveSettings()
    {
        if (App.SettingsService != null)
        {
            _ = App.SettingsService.SaveGlobalSettingsAsync();
        }
    }
}
