using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeLeavePortal.Data;
using EmployeeLeavePortal.Models;
using EmployeeLeavePortal.Models.ViewModels;

namespace EmployeeLeavePortal.Controllers;

[Authorize]
public class LeavesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeavesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // Employee: My requests
    [Authorize(Roles = "Employee,Manager,Admin")]
    public async Task<IActionResult> My()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        var list = await _context.LeaveRequests
            .Where(l => l.EmployeeId == user.Id)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();
        return View(list);
    }

    // Employee: create leave request
    [Authorize(Roles = "Employee,Manager,Admin")]
    public IActionResult Create()
    {
        return View(new LeaveCreateViewModel());
    }

    [Authorize(Roles = "Employee,Manager,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveCreateViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (vm.EndDate < vm.StartDate)
        {
            ModelState.AddModelError(string.Empty, "End date cannot be before start date.");
        }

        // Prevent overlaps with existing non-rejected requests
        var overlaps = await _context.LeaveRequests
            .Where(l => l.EmployeeId == user.Id && l.Status != LeaveStatus.Rejected)
            .Where(l => !(vm.EndDate < l.StartDate || vm.StartDate > l.EndDate))
            .AnyAsync();
        if (overlaps)
        {
            ModelState.AddModelError(string.Empty, "Overlapping leave request exists in the selected range.");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var req = new LeaveRequest
        {
            EmployeeId = user.Id,
            StartDate = vm.StartDate,
            EndDate = vm.EndDate,
            Type = vm.Type,
            Reason = vm.Reason,
            Status = LeaveStatus.Pending
        };
        _context.LeaveRequests.Add(req);
        await _context.SaveChangesAsync();
        TempData["Toast"] = "Leave request submitted.";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(My));
    }

    // Manager: pending approvals for team (same department)
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Pending()
    {
        var me = await _userManager.GetUserAsync(User);
        if (me == null) return Challenge();
        IQueryable<LeaveRequest> query = _context.LeaveRequests.Include(l => l.Employee);
        if (!User.IsInRole("Admin"))
        {
            query = query.Where(l => l.Employee!.DepartmentId == me.DepartmentId);
        }
        var list = await query.Where(l => l.Status == LeaveStatus.Pending)
            .OrderBy(l => l.StartDate)
            .ToListAsync();
        return View(list);
    }

    // Admin: view all
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> All()
    {
        var list = await _context.LeaveRequests.Include(l => l.Employee)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();
        return View(list);
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? comments)
    {
        var req = await _context.LeaveRequests.Include(l => l.Employee).FirstOrDefaultAsync(l => l.Id == id);
        if (req == null || req.Status != LeaveStatus.Pending) return NotFound();

        // Authorization: if Manager, must be same department
        var me = await _userManager.GetUserAsync(User);
        if (me == null) return Challenge();
        if (User.IsInRole("Manager") && req.Employee?.DepartmentId != me.DepartmentId)
            return Forbid();

        req.Status = LeaveStatus.Approved;
        req.ManagerComments = comments;

        // Update leave balance simple logic
        var days = (req.EndDate.ToDateTime(TimeOnly.MinValue) - req.StartDate.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;
        var balance = await _context.LeaveBalances.FirstOrDefaultAsync(b => b.EmployeeId == req.EmployeeId && b.LeaveType == req.Type);
        if (balance == null)
        {
            balance = new LeaveBalance { EmployeeId = req.EmployeeId, LeaveType = req.Type, TotalAllowed = 12, Used = 0 };
            _context.LeaveBalances.Add(balance);
        }
        balance.Used += (int)Math.Max(0, days);

        await _context.SaveChangesAsync();
        TempData["Toast"] = "Leave approved.";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Pending));
    }

    [Authorize(Roles = "Manager,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? comments)
    {
        var req = await _context.LeaveRequests.Include(l => l.Employee).FirstOrDefaultAsync(l => l.Id == id);
        if (req == null || req.Status != LeaveStatus.Pending) return NotFound();

        var me = await _userManager.GetUserAsync(User);
        if (me == null) return Challenge();
        if (User.IsInRole("Manager") && req.Employee?.DepartmentId != me.DepartmentId)
            return Forbid();

        req.Status = LeaveStatus.Rejected;
        req.ManagerComments = comments;
        await _context.SaveChangesAsync();
        TempData["Toast"] = "Leave rejected.";
        TempData["ToastType"] = "danger";
        return RedirectToAction(nameof(Pending));
    }
}
