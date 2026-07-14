# Local development configuration

ADEEB does not commit local database passwords, seed administrator passwords, or JWT signing keys.

Use user-secrets or environment variables for local values:

```bash
dotnet user-secrets set "ConnectionStrings:Identity" "Host=localhost;Port=5432;Database=adeeb_v2;Username=<user>;Password=<password>" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
dotnet user-secrets set "ConnectionStrings:AcademicCatalog" "Host=localhost;Port=5432;Database=adeeb_v2;Username=<user>;Password=<password>" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
dotnet user-secrets set "ConnectionStrings:QuestionBank" "Host=localhost;Port=5432;Database=adeeb_v2;Username=<user>;Password=<password>" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
dotnet user-secrets set "ConnectionStrings:Students" "Host=localhost;Port=5432;Database=adeeb_v2;Username=<user>;Password=<password>" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
dotnet user-secrets set "ConnectionStrings:Commerce" "Host=localhost;Port=5432;Database=adeeb_v2;Username=<user>;Password=<password>" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-characters-non-default-secret>" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
```

The local Docker compose file uses `Username=postgres;Password=postgres` for development only.

Optional local seed administrator values:

```bash
dotnet user-secrets set "SeedSuperAdmin:Enabled" "true" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
dotnet user-secrets set "SeedSuperAdmin:Email" "<admin-email>" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
dotnet user-secrets set "SeedSuperAdmin:Password" "<admin-password>" --project src/Hosts/Adeeb.Api/Adeeb.Api.csproj
```

Access tokens are immutable JWTs. If a user changes preferred language, existing access tokens keep the old `lang` claim until a new access token is issued by login or refresh.
