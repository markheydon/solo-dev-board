using System.Diagnostics;

namespace SoloDevBoard.E2E.Tests.Fixtures;

/// <summary>
/// Starts and stops the SoloDevBoard application for end-to-end test runs.
/// </summary>
public sealed class SoloDevBoardWebApplicationFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim StartLock = new(1, 1);
    private static SoloDevBoardWebApplicationFixture? sharedInstance;
    private static int referenceCount;

    private Process? process;
    private bool ownsProcess;

    /// <summary>
    /// Base URL for the application under test.
    /// </summary>
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("PLAYWRIGHT_BASE_URL") ?? "http://localhost:5080";

    /// <summary>
    /// Health endpoint used to wait for application readiness.
    /// </summary>
    public static Uri HealthUri => new($"{BaseUrl.TrimEnd('/')}/health");

    /// <summary>
    /// Ensures the shared application instance is running.
    /// </summary>
    public static async Task EnsureStartedAsync()
    {
        await StartLock.WaitAsync();
        try
        {
            sharedInstance ??= new SoloDevBoardWebApplicationFixture();
            referenceCount++;
            await sharedInstance.EnsureProcessStartedAsync();
        }
        finally
        {
            StartLock.Release();
        }
    }

    /// <summary>
    /// Releases a reference to the shared application instance.
    /// </summary>
    public static async Task ReleaseAsync()
    {
        await StartLock.WaitAsync();
        try
        {
            if (sharedInstance is null || referenceCount == 0)
            {
                return;
            }

            referenceCount--;
            if (referenceCount > 0)
            {
                return;
            }

            await sharedInstance.DisposeAsync();
            sharedInstance = null;
        }
        finally
        {
            StartLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        await EnsureStartedAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await ReleaseAsync();
    }

    private async Task EnsureProcessStartedAsync()
    {
        if (process is { HasExited: false })
        {
            return;
        }

        if (ShouldReuseExistingServer() && await IsHealthyAsync())
        {
            ownsProcess = false;
            return;
        }

        var projectPath = ResolveAppProjectPath();

        var configuration = ResolveBuildConfiguration();

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{projectPath}\" -c {configuration} --no-launch-profile --no-build",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var (key, value) in WebServerEnvironment.Create())
        {
            startInfo.Environment[key] = value;
        }

        process = Process.Start(startInfo);
        ArgumentNullException.ThrowIfNull(process);

        ownsProcess = true;
        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                Console.WriteLine($"[app] {args.Data}");
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrWhiteSpace(args.Data))
            {
                Console.WriteLine($"[app:err] {args.Data}");
            }
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await WaitForHealthyAsync(TimeSpan.FromSeconds(120));
    }

    private static bool ShouldReuseExistingServer()
    {
        var reuse = Environment.GetEnvironmentVariable("PLAYWRIGHT_REUSE_SERVER");
        return string.Equals(reuse, "1", StringComparison.Ordinal)
            || string.Equals(reuse, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var body = await client.GetStringAsync(HealthUri);
            return body.Contains("Healthy", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static async Task WaitForHealthyAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await IsHealthyAsync())
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Timed out waiting for {HealthUri}.");
    }

    private static string ResolveBuildConfiguration()
    {
        var baseDirectory = AppContext.BaseDirectory;
        if (baseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return "Release";
        }

        if (baseDirectory.Contains($"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return "Debug";
        }

        return "Release";
    }

    private static string ResolveAppProjectPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "App", "SoloDevBoard.App", "SoloDevBoard.App.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate SoloDevBoard.App.csproj.");
    }

    private async ValueTask DisposeProcessAsync()
    {
        if (!ownsProcess || process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }
        finally
        {
            process.Dispose();
            process = null;
            ownsProcess = false;
        }
    }
}
