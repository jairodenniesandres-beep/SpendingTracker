# SpendingTracker

Personal spending dashboard - ASP.NET Core MVC with EF Core, SQLite, and Identity auth.

- .NET 10 (LTS) - `net10.0`, built with SDK 10.0.402
- Microsoft.EntityFrameworkCore.Sqlite 10.0.13
- Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.13

## Run
    dotnet restore
    dotnet run

Open the app and **Register** a new account at `/Identity/Account/Register`, then **Login** at `/Identity/Account/Login`. There are **no pre-seeded users** — create your own.

If an older database still has demo logins (`user@aptech.com`, `dennies@gmail.com`, `admin@aptech.com`, etc.), startup removes those leftover seed accounts.

**If startup hangs or times out on `__EFMigrationsLock`:** stop every running SpendingTracker instance, then delete these files next to the project and re-run:
- `SpendingTracker.db`
- `SpendingTracker.db-shm`
- `SpendingTracker.db-wal`

Login is required except Identity Login/Register, Privacy, and Error.

## Per-user expenses
**Each logged-in user only sees and manages their own expenses** (`OwnerUserId`). Dashboard / Index / Details / Create / Edit / Delete are scoped to the current user. Accessing another user's expense id returns **404**.

Categories are a **shared** catalog (same list for everyone). Expense counts on category pages show only your expenses.

## Notes
- Currency is **QAR** (Qatar Riyal) via `en-QA` culture — amounts display as `QAR 12.50`.
- Categories are seeded for a **student in Doha**. **No sample expenses** — leftover demo rows from older seeds are cleared on startup.
- Styling uses only the Bootstrap 5 classes that ship with the template.
- Never commit `SpendingTracker.db` (in `.gitignore`).
