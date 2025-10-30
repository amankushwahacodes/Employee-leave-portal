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

[Authorize(Roles = "Employee,Manager,Admin")]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var record = await _context.AttendanceRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.EmployeeId == user.Id && r.Date == today);
        return View(record);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var existing = await _context.AttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == user.Id && r.Date == today);
        if (existing == null)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            _context.AttendanceRecords.Add(new AttendanceRecord
            {
                EmployeeId = user.Id,
                Date = today,
                CheckIn = now,
                HoursWorked = 0
            });
            await _context.SaveChangesAsync();
            TempData["Toast"] = $"Checked in at {TimeOnly.FromDateTime(DateTime.Now).ToString("HH\\:mm")}.";
            TempData["ToastType"] = "success";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var record = await _context.AttendanceRecords.FirstOrDefaultAsync(r => r.EmployeeId == user.Id && r.Date == today);
        if (record != null && record.CheckIn.HasValue && !record.CheckOut.HasValue)
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            record.CheckOut = now;
            try
            {
                var duration = now.ToTimeSpan() - record.CheckIn.Value.ToTimeSpan();
                if (duration.TotalHours > 0)
                    record.HoursWorked = Math.Round(duration.TotalHours, 2);
            }
            catch
            {
                // ignore invalid times
            }
            await _context.SaveChangesAsync();
            TempData["Toast"] = $"Checked out at {TimeOnly.FromDateTime(DateTime.Now).ToString("HH\\:mm")}.";
            TempData["ToastType"] = "success";
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> History()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var items = await _context.AttendanceRecords
            .Where(r => r.EmployeeId == user.Id)
            .OrderByDescending(r => r.Date)
            .ToListAsync();
        return View(items);
    }
}
