Run from the WebApp directory:

```powershell
dotnet run --project Verification/StudentApplications/StudentApplications.csproj --no-launch-profile -p:UseAppHost=false -p:OutputPath="$env:TEMP/bothfind-student-applications-ui/"
```

Uses production MVC controllers, Razor and the vacancy API client with local fixtures. Covers student authorization, session identity, candidate compatibility, legacy redirects, saved application statuses, closed/recategorized history, search and filters, status changes on reload, and empty/error/not-found states. It never reads hosted data.

Add `-- --preview` to keep the local browser preview at `http://127.0.0.1:5272/Student/Applications`. `/preview/filled`, `/preview/empty` and `/preview/error` select fixture states. Preview routes are not part of the application.

Publish Backend and WebApp for this feature. No database migration or SQL query is required.
