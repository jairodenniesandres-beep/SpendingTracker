using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpendingTracker.Models;

namespace SpendingTracker.Data;

public static class SeedData
{
    public static readonly string[] DefaultCategories =
    {
        "Food & Campus", "Transport", "Tuition & Fees", "Housing/Rent",
        "Entertainment", "Shopping", "Health", "Subscriptions", "Other"
    };

    private static readonly string[] SampleExpenseTitles =
    {
        "Shared flat rent",
        "Lulu / Carrefour groceries",
        "Metro + Karwa",
        "Ooredoo student plan",
        "Campus coffee + snack"
    };

    // Previously seeded logins — removed on startup so Register is the way in.
    private static readonly string[] LegacySeedEmails =
    {
        "user@aptech.com",
        "dennies@gmail.com",
        "admin@aptech.com",
        "admin@local.test",
        "user@local.test"
    };

    // Prevent concurrent MigrateAsync (e.g. two `dotnet run` / watch restarts) fighting over __EFMigrationsLock.
    private static readonly Mutex InitMutex = new(false, @"Local\SpendingTracker.DbInit");

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var acquired = false;
        try
        {
            acquired = InitMutex.WaitOne(TimeSpan.FromMinutes(2));
            if (!acquired)
                throw new TimeoutException(
                    "Timed out waiting for SpendingTracker database initialization. " +
                    "Stop other running instances, then delete SpendingTracker.db, SpendingTracker.db-shm, and SpendingTracker.db-wal if the lock persists.");

            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Migrate once, with a generous timeout, before any seed work opens more connections.
            context.Database.SetCommandTimeout(120);
            await context.Database.MigrateAsync();

            await ClearLegacySeedUsersAsync(scope.ServiceProvider);
            await EnsureStudentCatalogAsync(context);
        }
        finally
        {
            if (acquired)
                InitMutex.ReleaseMutex();
        }
    }

    private static async Task EnsureStudentCatalogAsync(AppDbContext context)
    {
        var names = await context.Categories.Select(c => c.Name).ToListAsync();
        var isStudentSet = names.Count > 0
            && names.Contains("Tuition & Fees")
            && names.Contains("Food & Campus")
            && !names.Contains("Utilities");

        if (!isStudentSet)
        {
            await context.Expenses.ExecuteDeleteAsync();
            await context.Categories.ExecuteDeleteAsync();

            var budgets = new Dictionary<string, decimal?>
            {
                ["Food & Campus"] = 900m,
                ["Transport"] = 250m,
                ["Tuition & Fees"] = 2500m,
                ["Housing/Rent"] = 1800m,
                ["Entertainment"] = 300m,
                ["Shopping"] = 400m,
                ["Health"] = 150m,
                ["Subscriptions"] = 120m,
                ["Other"] = 200m
            };
            context.Categories.AddRange(DefaultCategories.Select(n => new Category
            {
                Name = n,
                MonthlyBudget = budgets[n]
            }));
            await context.SaveChangesAsync();
        }

        if (await context.Expenses.AnyAsync(e => SampleExpenseTitles.Contains(e.Title)))
            await context.Expenses.ExecuteDeleteAsync();
    }

    private static async Task ClearLegacySeedUsersAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var email in LegacySeedEmails)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
                await userManager.DeleteAsync(user);
        }

        if (await roleManager.RoleExistsAsync("Admin"))
        {
            var adminRole = await roleManager.FindByNameAsync("Admin");
            if (adminRole != null)
                await roleManager.DeleteAsync(adminRole);
        }
    }
}
