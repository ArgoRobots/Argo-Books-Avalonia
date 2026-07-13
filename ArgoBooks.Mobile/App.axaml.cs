using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ArgoBooks.Mobile.Services;
using ArgoBooks.Mobile.ViewModels;
using ArgoBooks.Mobile.Views;
using ArgoBooks.Shared.Mobile;
using ArgoBooks.Shared.Sync;
using Microsoft.Maui.Storage;

namespace ArgoBooks.Mobile;

public partial class App : Application
{
    private static App? _current;

    private ISingleViewApplicationLifetime? _singleView;

    // The lock screen is an overlay: swapping MainView back to whatever was showing before the
    // lock (rather than always rebuilding the shell) keeps the shell's navigation state intact
    // across a resume-triggered lock/unlock.
    private Control? _contentBeforeLock;
    private bool _isLockShowing;

    // Guards the resume-triggered lock check against the very first OnResume, which Android
    // fires immediately after OnCreate/OnFrameworkInitializationCompleted on cold start - that
    // path is handled by TryResumeSessionAsync itself, so the resume handler no-ops until the
    // initial cold-start decision has actually been made.
    private bool _initialFlowComplete;

    public App()
    {
        _current = this;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Android only uses the single-view application lifetime.
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            _singleView = singleViewPlatform;
            ShowPairing();

            // If a company is already paired (returning user), skip straight to the shell -
            // behind the lock screen first if the biometric app lock is on.
            _ = TryResumeSessionAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>Called from MainActivity.OnPause: records when the app went to the background.</summary>
    public static void NotifyBackgrounded()
    {
        if (_current?._initialFlowComplete == true)
        {
            AppLockSettings.RecordBackgrounded();
        }
    }

    /// <summary>
    /// Called from MainActivity.OnResume: shows the lock screen if the grace period has elapsed
    /// (or hasn't started yet) since the app was last backgrounded. No-ops during the initial
    /// cold-start flow (already handled by TryResumeSessionAsync) and while the lock screen is
    /// already showing.
    /// </summary>
    public static void NotifyForegrounded()
    {
        _ = _current?.EvaluateResumeLockAsync();
    }

    private async Task EvaluateResumeLockAsync()
    {
        if (!_initialFlowComplete || _isLockShowing)
        {
            return;
        }

        var isEnabled = await AppLockSettings.IsEnabledAsync();
        var pairedCompanyStore = new PairedCompanyStore(new MauiSecureStore());
        var active = await pairedCompanyStore.GetActiveAsync();

        var shouldLock = AppLockPolicy.ShouldLock(
            isEnabled,
            active != null,
            AppLockSettings.LastBackgroundedUtc,
            DateTime.UtcNow,
            AppLockSettings.GracePeriod);

        if (shouldLock)
        {
            Dispatcher.UIThread.Post(ShowLockOverlay);
        }
    }

    private void ShowPairing()
    {
        var pairingViewModel = new PairingViewModel();
        pairingViewModel.Paired += companyLabel => Dispatcher.UIThread.Post(() => { _ = ShowShellAsync(); });

        _singleView!.MainView = new PairingView
        {
            DataContext = pairingViewModel
        };
    }

    private async Task TryResumeSessionAsync()
    {
        var pairedCompanyStore = new PairedCompanyStore(new MauiSecureStore());
        var active = await pairedCompanyStore.GetActiveAsync();
        if (active != null)
        {
            if (await AppLockSettings.IsEnabledAsync())
            {
                ShowLockThenShell();
            }
            else
            {
                await ShowShellAsync();
            }
        }

        _initialFlowComplete = true;
    }

    /// <summary>Shows the lock screen first (cold start); proceeds to the shell once unlocked.</summary>
    private void ShowLockThenShell()
    {
        _isLockShowing = true;
        _contentBeforeLock = null;

        var lockViewModel = new LockViewModel(new AppLockService());
        lockViewModel.Unlocked += () => Dispatcher.UIThread.Post(() =>
        {
            AppLockSettings.RecordUnlocked();
            _isLockShowing = false;
            _ = ShowShellAsync();
        });

        Dispatcher.UIThread.Post(() =>
        {
            _singleView!.MainView = new LockView { DataContext = lockViewModel };
        });
    }

    /// <summary>Overlays the lock screen on resume; restores the prior content once unlocked.</summary>
    private void ShowLockOverlay()
    {
        _isLockShowing = true;
        _contentBeforeLock = _singleView!.MainView;

        var lockViewModel = new LockViewModel(new AppLockService());
        lockViewModel.Unlocked += () => Dispatcher.UIThread.Post(() =>
        {
            AppLockSettings.RecordUnlocked();
            _isLockShowing = false;
            _singleView!.MainView = _contentBeforeLock ?? _singleView.MainView;
            _contentBeforeLock = null;
        });

        _singleView!.MainView = new LockView { DataContext = lockViewModel };
    }

    private async Task ShowShellAsync()
    {
        var client = new MobileSyncClient(null, MobileApiConfig.BaseUrl);
        var secureStore = new MauiSecureStore();
        var pairedCompanyStore = new PairedCompanyStore(secureStore);
        var cache = new FileSnapshotCache(FileSystem.Current.AppDataDirectory);
        var snapshotStore = new SnapshotStore(client, pairedCompanyStore, cache);
        var deviceApiAuth = await DeviceApiAuth.CreateAsync(secureStore);

        var shellViewModel = new ShellViewModel(snapshotStore, pairedCompanyStore, secureStore, deviceApiAuth, client);
        await shellViewModel.InitializeAsync();

        // Unpairing the last remaining company (Settings > Unpair this phone) drops back to the
        // full pairing screen, same as a fresh install.
        shellViewModel.RequestPairing += () => Dispatcher.UIThread.Post(ShowPairing);

        Dispatcher.UIThread.Post(() =>
        {
            _singleView!.MainView = new ShellView
            {
                DataContext = shellViewModel
            };
        });
    }
}
