using Microsoft.AspNetCore.Identity;

namespace EmployeeLeavePortal.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
}
