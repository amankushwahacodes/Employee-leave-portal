using EmployeeLeavePortal.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmployeeLeavePortal.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        string[] roles = ["Admin", "Manager", "Employee"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Departments
        if (!await context.Departments.AnyAsync())
        {
            context.Departments.AddRange(
                new Department { Name = "Human Resources" },
                new Department { Name = "Engineering" },
                new Department { Name = "Finance" }
            );
            await context.SaveChangesAsync();
        }

        var hr = await context.Departments.FirstAsync(d => d.Name == "Human Resources");
        var eng = await context.Departments.FirstAsync(d => d.Name == "Engineering");

        // Users
        async Task<ApplicationUser> EnsureUser(string email, string fullName, string role, Department dept)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    DepartmentId = dept.DepartmentId
                };
                var result = await userManager.CreateAsync(user, "Pass@123");
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create user {email}: {errors}");
                }
            }
            if (!await userManager.IsInRoleAsync(user, role))
            {
                await userManager.AddToRoleAsync(user, role);
            }
            return user;
        }

        var admin = await EnsureUser("admin@elp.local", "Admin User", "Admin", hr);
        var manager = await EnsureUser("manager@elp.local", "Manager User", "Manager", eng);
        var emp1 = await EnsureUser("employee1@elp.local", "Employee One", "Employee", eng);
        var emp2 = await EnsureUser("employee2@elp.local", "Employee Two", "Employee", eng);

        // Leave balances (basic)
        async Task EnsureBalance(ApplicationUser u, LeaveType type, int total)
        {
            if (!await context.LeaveBalances.AnyAsync(x => x.EmployeeId == u.Id && x.LeaveType == type))
            {
                context.LeaveBalances.Add(new LeaveBalance
                {
                    EmployeeId = u.Id,
                    LeaveType = type,
                    TotalAllowed = total,
                    Used = 0
                });
            }
        }

        await EnsureBalance(admin, LeaveType.Casual, 12);
        await EnsureBalance(manager, LeaveType.Casual, 12);
        await EnsureBalance(emp1, LeaveType.Casual, 12);
        await EnsureBalance(emp2, LeaveType.Casual, 12);

        await context.SaveChangesAsync();
    }
}
