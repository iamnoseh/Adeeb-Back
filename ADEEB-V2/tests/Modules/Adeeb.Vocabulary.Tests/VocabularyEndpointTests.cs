using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Modules.Vocabulary.Endpoints;
using Adeeb.Modules.Vocabulary.Application;
using Adeeb.Application.Abstractions.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.Vocabulary.Tests;

public sealed class VocabularyEndpointTests
{
    [Fact]
    public void Admin_routes_use_view_and_manage_permissions_and_student_routes_require_authentication()
    {
        var builder = WebApplication.CreateBuilder(); builder.Services.AddAuthorization(); builder.Services.AddScoped<VocabularyAdminService>(); builder.Services.AddScoped<VocabularyStudentService>(); builder.Services.AddSingleton<IMessageLocalizer, Localizer>();
        var app = builder.Build(); app.MapVocabularyEndpoints();
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>().ToList();
        AssertPolicy(endpoints, "/api/v2/admin/vocabulary/languages", "GET", Permissions.Vocabulary.View);
        AssertPolicy(endpoints, "/api/v2/admin/vocabulary/languages", "POST", Permissions.Vocabulary.Manage);
        var student = Find(endpoints, "/api/v2/students/me/vocabulary/dashboard", "GET");
        Assert.NotEmpty(student.Metadata.GetOrderedMetadata<IAuthorizeData>());
    }

    private static void AssertPolicy(IReadOnlyList<RouteEndpoint> endpoints, string route, string method, string policy)
    {
        var endpoint = Find(endpoints, route, method);
        Assert.Contains(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>(), x => x.Policy == policy);
    }
    private static RouteEndpoint Find(IReadOnlyList<RouteEndpoint> endpoints, string route, string method) => endpoints.Single(x => "/" + x.RoutePattern.RawText?.TrimStart('/') == route && x.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains(method) == true);
    private sealed class Localizer : IMessageLocalizer { public string this[string key] => key; }
}
