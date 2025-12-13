using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;

namespace ArgoBooks.Core.Models.Entities;

/// <summary>
/// Represents an employee.
/// </summary>
public class Employee
{
    /// <summary>
    /// Unique identifier (e.g., EMP-001).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// First name.
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name.
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Phone number.
    /// </summary>
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Date of birth.
    /// </summary>
    [JsonPropertyName("dateOfBirth")]
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Department ID.
    /// </summary>
    [JsonPropertyName("departmentId")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// Job position/title.
    /// </summary>
    [JsonPropertyName("position")]
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Date of hire.
    /// </summary>
    [JsonPropertyName("hireDate")]
    public DateTime HireDate { get; set; }

    /// <summary>
    /// Employment type (e.g., Full-time, Part-time, Contract).
    /// </summary>
    [JsonPropertyName("employmentType")]
    public string EmploymentType { get; set; } = "Full-time";

    /// <summary>
    /// Salary type (e.g., Annual, Hourly).
    /// </summary>
    [JsonPropertyName("salaryType")]
    public string SalaryType { get; set; } = "Annual";

    /// <summary>
    /// Salary amount.
    /// </summary>
    [JsonPropertyName("salaryAmount")]
    public decimal SalaryAmount { get; set; }

    /// <summary>
    /// Pay frequency (e.g., Weekly, Bi-weekly, Monthly).
    /// </summary>
    [JsonPropertyName("payFrequency")]
    public string PayFrequency { get; set; } = "Bi-weekly";

    /// <summary>
    /// Emergency contact information.
    /// </summary>
    [JsonPropertyName("emergencyContact")]
    public EmergencyContact? EmergencyContact { get; set; }

    /// <summary>
    /// Employment status.
    /// </summary>
    [JsonPropertyName("status")]
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>
    /// When the record was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the record was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Full name of the employee.
    /// </summary>
    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
