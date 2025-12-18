using System.Collections.ObjectModel;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for department modals, shared between DepartmentsPage and AppShell.
/// </summary>
public partial class DepartmentModalsViewModel : ObservableObject
{
    #region Modal State

    [ObservableProperty]
    private bool _isAddModalOpen;

    [ObservableProperty]
    private bool _isEditModalOpen;

    [ObservableProperty]
    private bool _isDeleteConfirmOpen;

    #endregion

    #region Modal Form Fields

    [ObservableProperty]
    private string _modalDepartmentName = string.Empty;

    [ObservableProperty]
    private string _modalDescription = string.Empty;

    [ObservableProperty]
    private string? _modalError;

    [ObservableProperty]
    private string? _modalDepartmentNameError;

    private Department? _editingDepartment;
    private DepartmentDisplayItem? _deletingDepartment;

    #endregion

    #region Delete Properties

    public string DeletingDepartmentName => _deletingDepartment?.Name ?? string.Empty;

    #endregion

    #region Events

    public event EventHandler? DepartmentSaved;
    public event EventHandler? DepartmentDeleted;

    #endregion

    #region Add Department

    [RelayCommand]
    public void OpenAddModal()
    {
        _editingDepartment = null;
        ClearModalFields();
        IsAddModalOpen = true;
    }

    [RelayCommand]
    public void CloseAddModal()
    {
        IsAddModalOpen = false;
        ClearModalFields();
    }

    [RelayCommand]
    public void SaveNewDepartment()
    {
        if (!ValidateModal()) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        companyData.IdCounters.Department++;
        var newId = $"DEPT-{companyData.IdCounters.Department:D3}";

        var newDepartment = new Department
        {
            Id = newId,
            Name = ModalDepartmentName.Trim(),
            Description = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        companyData.Departments.Add(newDepartment);
        companyData.MarkAsModified();

        var deptToUndo = newDepartment;
        App.UndoRedoManager?.RecordAction(new DepartmentAddAction(
            $"Add department '{newDepartment.Name}'",
            deptToUndo,
            () => { companyData.Departments.Remove(deptToUndo); companyData.MarkAsModified(); DepartmentSaved?.Invoke(this, EventArgs.Empty); },
            () => { companyData.Departments.Add(deptToUndo); companyData.MarkAsModified(); DepartmentSaved?.Invoke(this, EventArgs.Empty); }));

        DepartmentSaved?.Invoke(this, EventArgs.Empty);
        CloseAddModal();
    }

    #endregion

    #region Edit Department

    public void OpenEditModal(DepartmentDisplayItem? item)
    {
        if (item == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        var department = companyData?.Departments.FirstOrDefault(d => d.Id == item.Id);
        if (department == null) return;

        _editingDepartment = department;
        ModalDepartmentName = department.Name;
        ModalDescription = department.Description ?? string.Empty;
        ModalError = null;
        IsEditModalOpen = true;
    }

    [RelayCommand]
    public void CloseEditModal()
    {
        IsEditModalOpen = false;
        _editingDepartment = null;
        ClearModalFields();
    }

    [RelayCommand]
    public void SaveEditedDepartment()
    {
        if (!ValidateModal() || _editingDepartment == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var oldName = _editingDepartment.Name;
        var oldDescription = _editingDepartment.Description;

        var newName = ModalDepartmentName.Trim();
        var newDescription = string.IsNullOrWhiteSpace(ModalDescription) ? null : ModalDescription.Trim();

        var deptToEdit = _editingDepartment;
        deptToEdit.Name = newName;
        deptToEdit.Description = newDescription;
        companyData.MarkAsModified();

        App.UndoRedoManager?.RecordAction(new DepartmentEditAction(
            $"Edit department '{newName}'",
            deptToEdit,
            () => { deptToEdit.Name = oldName; deptToEdit.Description = oldDescription; companyData.MarkAsModified(); DepartmentSaved?.Invoke(this, EventArgs.Empty); },
            () => { deptToEdit.Name = newName; deptToEdit.Description = newDescription; companyData.MarkAsModified(); DepartmentSaved?.Invoke(this, EventArgs.Empty); }));

        DepartmentSaved?.Invoke(this, EventArgs.Empty);
        CloseEditModal();
    }

    #endregion

    #region Delete Department

    public void OpenDeleteConfirm(DepartmentDisplayItem? item)
    {
        if (item == null) return;
        _deletingDepartment = item;
        OnPropertyChanged(nameof(DeletingDepartmentName));
        IsDeleteConfirmOpen = true;
    }

    [RelayCommand]
    public void CloseDeleteConfirm()
    {
        IsDeleteConfirmOpen = false;
        _deletingDepartment = null;
    }

    [RelayCommand]
    public void ConfirmDelete()
    {
        if (_deletingDepartment == null) return;

        var companyData = App.CompanyManager?.CompanyData;
        if (companyData == null) return;

        var department = companyData.Departments.FirstOrDefault(d => d.Id == _deletingDepartment.Id);
        if (department == null) return;

        var deletedDept = department;
        companyData.Departments.Remove(department);
        companyData.MarkAsModified();

        App.UndoRedoManager?.RecordAction(new DepartmentDeleteAction(
            $"Delete department '{deletedDept.Name}'",
            deletedDept,
            () => { companyData.Departments.Add(deletedDept); companyData.MarkAsModified(); DepartmentDeleted?.Invoke(this, EventArgs.Empty); },
            () => { companyData.Departments.Remove(deletedDept); companyData.MarkAsModified(); DepartmentDeleted?.Invoke(this, EventArgs.Empty); }));

        DepartmentDeleted?.Invoke(this, EventArgs.Empty);
        CloseDeleteConfirm();
    }

    #endregion

    #region Helpers

    private void ClearModalFields()
    {
        ModalDepartmentName = string.Empty;
        ModalDescription = string.Empty;
        ModalError = null;
        ModalDepartmentNameError = null;
    }

    private bool ValidateModal()
    {
        ModalError = null;
        ModalDepartmentNameError = null;
        var isValid = true;

        if (string.IsNullOrWhiteSpace(ModalDepartmentName))
        {
            ModalDepartmentNameError = "Department name is required.";
            isValid = false;
        }
        else
        {
            var companyData = App.CompanyManager?.CompanyData;
            var existingWithSameName = companyData?.Departments.Any(d =>
                d.Name.Equals(ModalDepartmentName.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (_editingDepartment == null || d.Id != _editingDepartment.Id)) ?? false;

            if (existingWithSameName)
            {
                ModalDepartmentNameError = "A department with this name already exists.";
                isValid = false;
            }
        }

        return isValid;
    }

    #endregion
}
