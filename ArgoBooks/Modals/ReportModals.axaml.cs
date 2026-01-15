using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for report template management.
/// </summary>
public partial class ReportModals : UserControl
{
    private bool _eventsSubscribed;

    public ReportModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ReportModalsViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(ReportModalsViewModel.IsPageSettingsOpen):
                        var pageSettingsBorder = PageSettingsModal?.FindControl<Border>("RootBorder");
                        if (vm.IsPageSettingsOpen)
                            ModalAnimationHelper.AnimateIn(pageSettingsBorder);
                        else
                            ModalAnimationHelper.AnimateOut(pageSettingsBorder);
                        break;
                    case nameof(ReportModalsViewModel.IsSaveTemplateOpen):
                        var saveTemplateBorder = SaveTemplateModal?.FindControl<Border>("RootBorder");
                        if (vm.IsSaveTemplateOpen)
                            ModalAnimationHelper.AnimateIn(saveTemplateBorder);
                        else
                            ModalAnimationHelper.AnimateOut(saveTemplateBorder);
                        break;
                    case nameof(ReportModalsViewModel.IsDeleteTemplateOpen):
                        if (vm.IsDeleteTemplateOpen)
                            ModalAnimationHelper.AnimateIn(DeleteTemplateModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(DeleteTemplateModalBorder);
                        break;
                    case nameof(ReportModalsViewModel.IsRenameTemplateOpen):
                        if (vm.IsRenameTemplateOpen)
                            ModalAnimationHelper.AnimateIn(RenameTemplateModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(RenameTemplateModalBorder);
                        break;
                }
            };
        }
    }

    /// <summary>
    /// Handles Enter key press in the rename template TextBox.
    /// </summary>
    private void OnRenameTemplateKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ReportModalsViewModel vm)
        {
            vm.ReportsPageViewModel?.ConfirmRenameTemplateCommand.Execute(null);
            e.Handled = true;
        }
    }
}
