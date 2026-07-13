using ArgoBooks.Core.Services.Sync;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Mobile.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // Trivial use of ArgoBooks.Shared (net10.0) from the Android head, to prove the
    // cross-platform reuse premise: the same sync crypto code that runs on desktop
    // compiles and runs unchanged on Android.
    [ObservableProperty]
    private string _greeting = $"Welcome to Argo Books! (demo sync key: {SyncCrypto.GenerateSyncKey()[..8]}...)";
}
