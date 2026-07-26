using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Xunit;

namespace UnoEdit.DevFlowTests;

public sealed class HighlightingTests : IAsyncLifetime
{
    private Process? _app;
    private HttpClient? _client;

    public async ValueTask InitializeAsync()
    {
        var sampleProjectPath = GetSampleProjectPath();
        // Note: Sample app must be built manually before running tests:
        //   dotnet build src/UnoEdit.Sample/UnoEdit.Sample.csproj -f net10.0-desktop -c Debug
        // Build is not done here to avoid a pre-existing TextCore API mismatch
        // (AttachToCurrentWindow vs AttachToWindowHandle).
        _app = StartApp(sampleProjectPath);
        _client = new HttpClient { BaseAddress = new Uri("http://localhost:9223") };
        if (!await TryPingAsync(TimeSpan.FromSeconds(5)))
        {
            // If the app isn't running, build and start it
            BuildSampleProject(sampleProjectPath);
            _app = StartApp(sampleProjectPath);
        }
        await WaitForAgentAsync(TimeSpan.FromSeconds(30));
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        if (_app is { HasExited: false })
        {
            _app.Kill(true);
            await _app.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
        }
        _app?.Dispose();
    }

    [Fact]
    public async Task CsHighlighting_ProducesColoredSections()
    {
        var result = await CallHighlightTest(CreateTempFile(".cs", """
        using System;
        namespace Demo;
        public class Test {
            public int Add(int a, int b) => a + b;
        }
        """));
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.True(result.GetProperty("coloredLines").GetInt32() > 0);
        Assert.True(result.GetProperty("totalColoredSections").GetInt32() > 0);
    }

    [Fact]
    public async Task VbHighlighting_ProducesColoredSections()
    {
        var result = await CallHighlightTest(CreateTempFile(".vb", """
        Public Class Test
            Dim x As Integer = 42
            Public Function Add(ByVal a As Integer, ByVal b As Integer) As Integer
                Return a + b
            End Function
        End Class
        ' comment
        """));
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.True(result.GetProperty("coloredLines").GetInt32() > 0);
        Assert.True(result.GetProperty("totalColoredSections").GetInt32() > 0);
    }

    [Fact]
    public async Task XmlHighlighting_ProducesColoredSections()
    {
        var result = await CallHighlightTest(CreateTempFile(".xaml", """
        <?xml version="1.0" encoding="utf-8"?>
        <Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
            <StackPanel>
                <TextBlock Text="Hello" />
                <Button Content="Click" />
            </StackPanel>
        </Page>
        """));
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.True(result.GetProperty("coloredLines").GetInt32() > 0);
        Assert.True(result.GetProperty("totalColoredSections").GetInt32() > 0);
    }

    async Task<JsonElement> CallHighlightTest(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        var body = JsonSerializer.Serialize(new { args = new[] { filePath, ext } });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _client!.PostAsync("/api/v1/invoke/actions/unoedit.highlight.test", content);
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (envelope.TryGetProperty("returnValue", out var rv))
        {
            if (rv.ValueKind == JsonValueKind.String)
                return JsonDocument.Parse(rv.GetString()!).RootElement.Clone();
            return rv.Clone();
        }
        return envelope;
    }

    static string CreateTempFile(string extension, string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"unodev_{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, content);
        return path;
    }

    static string GetSampleProjectPath()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "src", "UnoEdit.Sample", "UnoEdit.Sample.csproj");
            if (File.Exists(candidate))
                return candidate;
            // Try walking up: from bin/Debug/net10.0 -> src/UnoEdit.Sample
            candidate = Path.Combine(dir, "..", "..", "..", "..", "..", "src", "UnoEdit.Sample", "UnoEdit.Sample.csproj");
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
            dir = Path.GetDirectoryName(dir);
        }
        throw new InvalidOperationException($"Cannot locate UnoEdit.Sample.csproj from {AppContext.BaseDirectory}");
    }

    static void BuildSampleProject(string sampleProjectPath)
    {
        var psi = new ProcessStartInfo("dotnet", $"build \"{sampleProjectPath}\" -f net10.0-desktop -c Debug")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet build");
        p.WaitForExit(120_000);
        if (p.ExitCode != 0)
        {
            var err = p.StandardError.ReadToEnd();
            throw new InvalidOperationException($"Build failed: {err}");
        }
    }

    static Process StartApp(string sampleProjectPath)
    {
        var psi = new ProcessStartInfo("dotnet", $"run --project \"{sampleProjectPath}\" -f net10.0-desktop --no-build -c Debug")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var p = new Process { StartInfo = psi };
        p.Start();
        return p;
    }

    async Task<bool> TryPingAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri("http://localhost:9223") };
                using var resp = await client.GetAsync("/api/v1/agent/status");
                if (resp.IsSuccessStatusCode)
                    return true;
            }
            catch { }
            await Task.Delay(300);
        }
        return false;
    }

    async Task WaitForAgentAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var resp = await _client!.GetAsync("/api/v1/agent/status");
                if (resp.IsSuccessStatusCode)
                    return;
            }
            catch { }
            await Task.Delay(300);
        }
        throw new TimeoutException("DevFlow agent did not start within timeout");
    }
}
