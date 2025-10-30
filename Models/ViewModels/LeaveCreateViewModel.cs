using System.ComponentModel.DataAnnotations;
using EmployeeLeavePortal.Models;

namespace EmployeeLeavePortal.Models.ViewModels;

public class LeaveCreateViewModel
{
    [Required]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }

    [Required]
    public LeaveType Type { get; set; } = LeaveType.Casual;

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
