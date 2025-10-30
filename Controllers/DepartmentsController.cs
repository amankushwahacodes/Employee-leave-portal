using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EmployeeLeavePortal.Data;
using EmployeeLeavePortal.Models;

namespace EmployeeLeavePortal.Controllers;

[Authorize(Roles = "Admin")]
public class DepartmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public DepartmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Departments
    public async Task<IActionResult> Index()
    {
        var list = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        return View(list);
    }

    // GET: Departments/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();
        var dept = await _context.Departments.FirstOrDefaultAsync(m => m.DepartmentId == id);
        if (dept == null) return NotFound();
        return View(dept);
    }

    // GET: Departments/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Departments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("DepartmentId,Name")] Department department)
    {
        if (!ModelState.IsValid) return View(department);
        _context.Add(department);
        await _context.SaveChangesAsync();
        TempData["Toast"] = $"Department '{department.Name}' created.";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // GET: Departments/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var dept = await _context.Departments.FindAsync(id);
        if (dept == null) return NotFound();
        return View(dept);
    }

    // POST: Departments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("DepartmentId,Name")] Department department)
    {
        if (id != department.DepartmentId) return NotFound();
        if (!ModelState.IsValid) return View(department);
        try
        {
            _context.Update(department);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Departments.AnyAsync(e => e.DepartmentId == department.DepartmentId))
                return NotFound();
            throw;
        }
        TempData["Toast"] = $"Department '{department.Name}' updated.";
        TempData["ToastType"] = "success";
        return RedirectToAction(nameof(Index));
    }

    // GET: Departments/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var dept = await _context.Departments.FirstOrDefaultAsync(m => m.DepartmentId == id);
        if (dept == null) return NotFound();
        return View(dept);
    }

    // POST: Departments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept != null)
        {
            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();
        }
        TempData["Toast"] = "Department deleted.";
        TempData["ToastType"] = "danger";
        return RedirectToAction(nameof(Index));
    }
}
