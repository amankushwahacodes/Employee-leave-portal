namespace EmployeeLeavePortal.Models;

public class AttendanceRecord
{
    public int Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public ApplicationUser? Employee { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public double HoursWorked { get; set; }
}
