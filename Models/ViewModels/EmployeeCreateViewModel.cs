using System.ComponentModel.DataAnnotations;

namespace EmployeeLeavePortal.Models.ViewModels;

public class EmployeeCreateViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    [RegularExpression("^(Manager|Employee)$", ErrorMessage = "Role must be Manager or Employee")]
    public string Role { get; set; } = "Employee";

    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = "Pass@123";
}
