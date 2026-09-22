Run from the WebApp repository:

```powershell
dotnet run --project Verification/Internships/Internships.csproj
```

Runs the real Razor pages on localhost with a local fixture API. Checks Student-only access, empty/error states, filtering/search, Employee separation, screening links and employer category form rendering/binding. No hosted API is contacted.

For visual testing, append `-- --preview`. The preview starts empty at `http://127.0.0.1:5267/Student/Internships`. `/preview/filled` enables local test cards and `/preview/empty` clears them. These endpoints and fixtures exist only in this verification project, which is excluded from the published WebApp.

The Backend's `20260922120000_AddVacancyType` migration must be applied before publishing this feature.
