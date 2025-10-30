namespace EmployeeLeavePortal.Models;

public enum LeaveType
{
    Casual = 0,
    Sick = 1,
    Earned = 2,
    Unpaid = 3
}

public enum LeaveStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class LeaveRequest
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public ApplicationUser? Employee { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public LeaveType Type { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public string? ManagerComments { get; set; }
}

public class LeaveBalance
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public ApplicationUser? Employee { get; set; }

    public LeaveType LeaveType { get; set; }
    public int TotalAllowed { get; set; }
    public int Used { get; set; }
}
