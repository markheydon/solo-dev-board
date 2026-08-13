using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SoloDevBoard.Infrastructure.Identity;

namespace SoloDevBoard.Infrastructure.Tests;

public sealed class HostedAdmissionControlMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DeniedHtmlRequest_RedirectsToAuthErrorPage()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var context = CreateHttpContext(isAuthenticated: true, acceptHeader: "text/html");
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var evaluator = CreateEvaluator(new HostedAdmissionDecision(false, "Hosted identity is not present in user or organisation allow-lists."));

        // Act
        await middleware.InvokeAsync(context, evaluator);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Equal(
            HostedAuthErrorRoutes.BuildErrorUrl(HostedAuthErrorRoutes.AccessDenied),
            context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task InvokeAsync_DeniedJsonRequest_ReturnsProblemDetails()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var context = CreateHttpContext(isAuthenticated: true, acceptHeader: "application/json");
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var evaluator = CreateEvaluator(new HostedAdmissionDecision(false, "Hosted identity is not present in user or organisation allow-lists."));

        // Act
        await middleware.InvokeAsync(context, evaluator);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Contains("application/json", context.Response.ContentType, StringComparison.OrdinalIgnoreCase);

        context.Response.Body.Position = 0;
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken);

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status403Forbidden, problemDetails.Status);
        Assert.Equal("Hosted access denied", problemDetails.Title);
        Assert.Equal("Your hosted identity is not authorised for this deployment.", problemDetails.Detail);
    }

    [Fact]
    public async Task InvokeAsync_AllowedUser_InvokesNextMiddleware()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var context = CreateHttpContext(isAuthenticated: true, acceptHeader: "text/html");
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var evaluator = CreateEvaluator(new HostedAdmissionDecision(true, "Hosted admission allowed by user allow-list."));

        // Act
        await middleware.InvokeAsync(context, evaluator);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedUser_ChallengesWelcomePage()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var context = CreateHttpContext(isAuthenticated: false, acceptHeader: "text/html");
        ConfigureAuthentication(context, loginPath: "/welcome");
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var evaluator = CreateEvaluator(new HostedAdmissionDecision(false, "Hosted request is not authenticated."));

        // Act
        await middleware.InvokeAsync(context, evaluator);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Contains("/welcome", context.Response.Headers.Location.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedUser_ChallengesSignIn()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var context = CreateHttpContext(isAuthenticated: false, acceptHeader: "text/html");
        ConfigureAuthentication(context, loginPath: "/auth/sign-in");
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var evaluator = CreateEvaluator(new HostedAdmissionDecision(false, "Hosted request is not authenticated."));

        // Act
        await middleware.InvokeAsync(context, evaluator);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        Assert.Contains("/auth/sign-in", context.Response.Headers.Location.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_DisabledAdmissionControl_InvokesNextMiddleware()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        // Arrange
        var context = CreateHttpContext(isAuthenticated: true, acceptHeader: "text/html");
        var nextCalled = false;
        var middleware = CreateMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new HostedAdmissionControlOptions { Enabled = false });
        var evaluator = CreateEvaluator(new HostedAdmissionDecision(false, "Hosted identity is not present in user or organisation allow-lists."));

        // Act
        await middleware.InvokeAsync(context, evaluator);

        // Assert
        Assert.True(nextCalled);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    [InlineData("/health/github")]
    [InlineData("/welcome")]
    [InlineData("/_blazor/negotiate")]
    public async Task InvokeAsync_UnauthenticatedBypassedPath_InvokesNextMiddleware(string requestPath)
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        var context = CreateHttpContext(isAuthenticated: false, acceptHeader: "text/html");
        context.Request.Path = requestPath;
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var evaluator = CreateEvaluator(new HostedAdmissionDecision(false, "Hosted request is not authenticated."));

        await middleware.InvokeAsync(context, evaluator);

        Assert.True(nextCalled);
    }

    private static HostedAdmissionControlMiddleware CreateMiddleware(
        RequestDelegate next,
        HostedAdmissionControlOptions? admissionOptions = null) =>
        new(
            next,
            Options.Create(admissionOptions ?? new HostedAdmissionControlOptions { Enabled = true }),
            Substitute.For<ILogger<HostedAdmissionControlMiddleware>>());

    private static IHostedAdmissionEvaluator CreateEvaluator(HostedAdmissionDecision decision)
    {
        var evaluator = Substitute.For<IHostedAdmissionEvaluator>();
        evaluator.Evaluate(Arg.Any<ClaimsPrincipal>()).Returns(decision);

        return evaluator;
    }

    private static DefaultHttpContext CreateHttpContext(bool isAuthenticated, string? acceptHeader)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
            Response =
            {
                Body = new MemoryStream(),
            },
        };

        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/";

        if (!string.IsNullOrWhiteSpace(acceptHeader))
        {
            context.Request.Headers.Accept = acceptHeader;
        }

        if (isAuthenticated)
        {
            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, "markheydon"));
            context.User = new ClaimsPrincipal(identity);
        }

        return context;
    }

    private static void ConfigureAuthentication(HttpContext context, string loginPath = "/welcome")
    {
        var services = new ServiceCollection();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = loginPath;
            });
        services.AddLogging();

        context.RequestServices = services.BuildServiceProvider();
    }
}
