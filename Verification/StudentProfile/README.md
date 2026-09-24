# Student profile verification

From the web repository:

```powershell
dotnet run --project Verification/StudentProfile/StudentProfile.csproj
```

The harness uses the actual MVC controllers, Razor views, API client and antiforgery middleware with local in-memory API fixtures. It never calls the hosted API or database. It checks student access, identity ownership, CSRF, validation, save/reload, name cookie refresh, unchanged quiz scores, draft retention during API errors, legacy candidate/employer pages and employer-visible student education.

Add `-- --preview` to leave the fixture running at `http://127.0.0.1:5273/Student/Profile` for browser checks. Browser verification covered desktop and 390px mobile layouts, draft preview, availability toggle, save feedback and invalid-photo rejection. Fixture names and skills are not production seed data.

Before publishing the updated applications, apply backend migration `20260924120000_AddStudentProfileDetails` (or the matching SQL script). See `Scripts/StudentProfileDeployment.md` in the backend repository.
