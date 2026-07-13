using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ArgoBooks.Mobile.ViewModels;
using ArgoBooks.Mobile.Views;

namespace ArgoBooks.Mobile;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Android only uses the single-view application lifetime.
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // PairingView is the pairing/connect-to-desktop screen (Plan 4 Task 5). A later
            // task will branch on PairedCompanyStore.GetActiveAsync() to skip straight to the
            // dashboard when a company is already paired; for now this is the single entry view.
            singleViewPlatform.MainView = new PairingView
            {
                DataContext = new PairingViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
