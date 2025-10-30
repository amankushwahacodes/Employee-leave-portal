using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeLeavePortal.Data;
using EmployeeLeavePortal.Models;

namespace EmployeeLeavePortal.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin")) return RedirectToAction(nameof(Admin));
        if (User.IsInRole("Manager")) return RedirectToAction(nameof(Manager));
        return RedirectToAction(nameof(Employee));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Admin()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var totalEmployees = await _context.Users.CountAsync();
        var totalDepartments = await _context.Departments.CountAsync();
        var pendingLeaves = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending);
        var todaysAttendance = await _context.AttendanceRecords.CountAsync(a => a.Date == today);
        ViewBag.TotalEmployees = totalEmployees;
        ViewBag.TotalDepartments = totalDepartments;
        ViewBag.PendingLeaves = pendingLeaves;
        ViewBag.TodaysAttendance = todaysAttendance;
        return View();
    }

    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Manager()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me == null) return Challenge();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var teamSize = await _context.Users.CountAsync(u => u.DepartmentId == me.DepartmentId);
        var teamPending = await _context.LeaveRequests.Include(l => l.Employee)
            .CountAsync(l => l.Status == LeaveStatus.Pending && l.Employee!.DepartmentId == me.DepartmentId);
        var teamAttendanceToday = await _context.AttendanceRecords.Include(a => a.Employee)
            .CountAsync(a => a.Date == today && a.Employee!.DepartmentId == me.DepartmentId);
        ViewBag.TeamSize = teamSize;
        ViewBag.TeamPending = teamPending;
        ViewBag.TeamAttendanceToday = teamAttendanceToday;
        return View();
    }

    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<IActionResult> Employee()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me == null) return Challenge();
        var balances = await _context.LeaveBalances.Where(b => b.EmployeeId == me.Id).ToListAsync();
        var recentAttendance = await _context.AttendanceRecords.Where(a => a.EmployeeId == me.Id)
            .OrderByDescending(a => a.Date).Take(7).ToListAsync();
        var myPending = await _context.LeaveRequests.CountAsync(l => l.EmployeeId == me.Id && l.Status == LeaveStatus.Pending);
        ViewBag.Balances = balances;
        ViewBag.RecentAttendance = recentAttendance;
        ViewBag.MyPending = myPending;
        return View();
    }
}
