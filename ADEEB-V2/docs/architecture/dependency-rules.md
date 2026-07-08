# Dependency Rules

The API host may reference modules and building blocks. The Identity module may reference `Adeeb.SharedKernel` and `Adeeb.Application.Abstractions`.

Domain code must not depend on EF Core, ASP.NET Core, Npgsql, JWT, or module infrastructure. `Adeeb.SharedKernel` must remain framework-free.
