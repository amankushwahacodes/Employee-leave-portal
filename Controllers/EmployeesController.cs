using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EmployeeLeavePortal.Data;
using EmployeeLeavePortal.Models;
using EmployeeLeavePortal.Models.ViewModels;

namespace EmployeeLeavePortal.Controllers;

[Authorize(Roles = "Admin")]
public class EmployeesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public EmployeesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.Include(u => u.Department).ToListAsync();
        var model = new List<(ApplicationUser User, string Role)>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            model.Add((u, roles.FirstOrDefault() ?? ""));
        }
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DepartmentId", "Name");
        ViewBag.Roles = new SelectList(new[] { "Manager", "Employee" });
        return View(new EmployeeCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeCreateViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DepartmentId", "Name", vm.DepartmentId);
            ViewBag.Roles = new SelectList(new[] { "Manager", "Employee" }, vm.Role);
            return View(vm);
        }

        var user = new ApplicationUser
        {
            UserName = vm.Email,
            Email = vm.Email,
            EmailConfirmed = true,
            FullName = vm.FullName,
            DepartmentId = vm.DepartmentId
        };
        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DepartmentId", "Name", vm.DepartmentId);
            ViewBag.Roles = new SelectList(new[] { "Manager", "Employee" }, vm.Role);
            return View(vm);
        }
        if (!await _roleManager.RoleExistsAsync(vm.Role)) await _roleManager.CreateAsync(new IdentityRole(vm.Role));
        await _userManager.AddToRoleAsync(user, vm.Role);

        if (!await _context.LeaveBalances.AnyAsync(x => x.EmployeeId == user.Id && x.LeaveType == LeaveType.Casual))
        {
            _context.LeaveBalances.Add(new LeaveBalance { EmployeeId = user.Id, LeaveType = LeaveType.Casual, TotalAllowed = 12, Used = 0 });
            await _context.SaveChangesAsync();
        }
        TempData["Toast"] = $"Employee '{user.FullName}' created.";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var user = await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Departments = new SelectList(await _context.Departments.OrderBy(d => d.Name).ToListAsync(), "DepartmentId", "Name", user.DepartmentId);
        ViewBag.Roles = new SelectList(new[] { "Manager", "Employee" }, roles.FirstOrDefault() ?? "Employee");
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string fullName, int departmentId, string role)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        user.FullName = fullName;
        user.DepartmentId = departmentId;
        _context.Update(user);
        await _context.SaveChangesAsync();

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(role))
        {
            if (currentRoles.Count > 0) await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!await _roleManager.RoleExistsAsync(role)) await _roleManager.CreateAsync(new IdentityRole(role));
            await _userManager.AddToRoleAsync(user, role);
        }
        TempData["Toast"] = $"Employee '{user.FullName}' updated.";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var user = await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }
        TempData["Toast"] = "Employee deleted.";
        TempData["ToastType"] = "danger";
        return RedirectToAction(nameof(Index));
    }
}
