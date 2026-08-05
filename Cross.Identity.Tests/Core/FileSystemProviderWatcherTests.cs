namespace Cross.Identity.Tests.Core;

[Category(TestCategory.UNIT)]
[TestFixture]
public sealed class FileSystemProviderWatcherTests
{
    [Test]
    public void Ctor_WhenTemplatesDirectoryMissing_ShouldThrow()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fs-no-templates-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var options = Microsoft.Extensions.Options.Options.Create(new FileSystemProcessDefinitionOptions
            {
                Directory = root,
                ReloadOnChange = false
            });

            var act = () => new FileSystemProcessDefinitionProvider(options);
            act.Should().Throw<DirectoryNotFoundException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void Watchers_ShouldReloadRenameAndDeleteFlowsAndTemplates()
    {
        var root = Path.Combine(Path.GetTempPath(), $"fs-watch-{Guid.NewGuid():N}");
        var templates = Path.Combine(root, "Templates");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(templates);

        try
        {
            var flowA = Path.Combine(root, "main.token.json");
            var tplA = Path.Combine(templates, "welcome.en.html");
            File.WriteAllText(flowA, """{"v":"1"}""");
            File.WriteAllText(tplA, "<h1>v1</h1>");

            var options = Microsoft.Extensions.Options.Options.Create(new FileSystemProcessDefinitionOptions
            {
                Directory = root,
                ReloadOnChange = true
            });

            using var sut = new FileSystemProcessDefinitionProvider(options);

            sut.GetJson("main", FlowOperationEnum.Token).Should().Contain("\"1\"");
            sut.GetTemplate("welcome", "en", "html").Should().Contain("v1");

            // changed
            File.WriteAllText(flowA, """{"v":"2"}""");
            File.WriteAllText(tplA, "<h1>v2</h1>");
            WaitUntil(() => sut.GetJson("main", FlowOperationEnum.Token).Contains("\"2\""));
            WaitUntil(() => sut.GetTemplate("welcome", "en", "html").Contains("v2"));

            // renamed
            var flowB = Path.Combine(root, "main.refreshtoken.json");
            File.Move(flowA, flowB);
            WaitUntil(() =>
            {
                try
                {
                    _ = sut.GetJson("main", FlowOperationEnum.Token);
                    return false;
                }
                catch (KeyNotFoundException)
                {
                    return true;
                }
            });
            WaitUntil(() => sut.GetJson("main", FlowOperationEnum.RefreshToken).Contains("\"2\""));

            var tplB = Path.Combine(templates, "verify.en.txt");
            File.Move(tplA, tplB);
            WaitUntil(() =>
            {
                try
                {
                    _ = sut.GetTemplate("welcome", "en", "html");
                    return false;
                }
                catch (KeyNotFoundException)
                {
                    return true;
                }
            });
            WaitUntil(() => sut.GetTemplate("verify", "en", "txt").Contains("v2"));

            // deleted
            File.Delete(flowB);
            File.Delete(tplB);
            WaitUntil(() =>
            {
                try
                {
                    _ = sut.GetJson("main", FlowOperationEnum.RefreshToken);
                    return false;
                }
                catch (KeyNotFoundException)
                {
                    return true;
                }
            });
            WaitUntil(() =>
            {
                try
                {
                    _ = sut.GetTemplate("verify", "en", "txt");
                    return false;
                }
                catch (KeyNotFoundException)
                {
                    return true;
                }
            });
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void WaitUntil(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return;
            }

            Thread.Sleep(50);
        }

        throw new TimeoutException("Condition was not met in time.");
    }
}
