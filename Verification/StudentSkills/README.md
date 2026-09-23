# Student skills verification

Run from the WebApp repository in PowerShell:

```powershell
dotnet run --project Verification/StudentSkills/StudentSkills.csproj --no-launch-profile -p:UseAppHost=false -p:OutputPath="$env:TEMP/bothfind-student-skills-ui-tests/"
node --test Verification/StudentSkills/student-skills.test.cjs
```

The HTTP checks host the production controllers, Razor views and API client with a stateful local API fixture. They cover student access restrictions, unchanged candidate skills access, anti-forgery, authenticated user identity, skill addition without employment, automatic quiz launch, persisted knowledge and SSI, removal, and service outages. They do not contact the hosted backend, a database or an AI provider.

The JavaScript checks cover complete-answer validation, request cancellation, stale language responses, score-save failures and retry, and refreshing the profile after a successful quiz.

Add `-- --preview` to the `dotnet run` command to keep the local preview open at `http://127.0.0.1:5271/Student/Skills`. `/preview/filled` loads three fixture skills; `/preview/empty` resets them. These preview routes exist only in the verification application.
