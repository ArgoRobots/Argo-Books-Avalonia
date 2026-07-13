using System;
using System.Threading.Tasks;
using Avalonia;
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
    private ISingleViewApplicationLifetime? _singleView;

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

            // If a company is already paired (returning user), skip straight to the shell.
            _ = TryResumeSessionAsync();
        }

        base.OnFrameworkInitializationCompleted();
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
            await ShowShellAsync();
        }
    }

    private async Task ShowShellAsync()
    {
        var client = new MobileSyncClient(null, MobileApiConfig.BaseUrl);
        var pairedCompanyStore = new PairedCompanyStore(new MauiSecureStore());
        var cache = new FileSnapshotCache(FileSystem.Current.AppDataDirectory);
        var snapshotStore = new SnapshotStore(client, pairedCompanyStore, cache);

        var shellViewModel = new ShellViewModel(snapshotStore, pairedCompanyStore);
        await shellViewModel.InitializeAsync();

        Dispatcher.UIThread.Post(() =>
        {
            _singleView!.MainView = new ShellView
            {
                DataContext = shellViewModel
            };
        });
    }
}
