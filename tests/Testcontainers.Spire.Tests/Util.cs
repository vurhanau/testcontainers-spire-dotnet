using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers;

namespace Testcontainers.Spire.Tests
{
    public static class Util
    {
        public static async Task AssertLogAsync(this IContainer c, string log, int timeoutSeconds)
        {
            _ = c ?? throw new ArgumentNullException(nameof(c));
            _ = log ?? throw new ArgumentNullException(nameof(log));

            var t0 = DateTime.Now;
            var ready = false;
            while ((DateTime.Now - t0).TotalSeconds < timeoutSeconds)
            {
                var (stdout, stderr) = await c.GetLogsAsync();
                if (!string.IsNullOrEmpty(stderr))
                {
                    Assert.Fail($"Failed to await log '{log}'. Stderr: {stderr}");
                }

                if (Regex.IsMatch(stdout, log))
                {
                    ready = true;
                    break;
                }

                await Task.Delay(50);
            }

            if (!ready)
            {
                var duration = (DateTime.Now - t0).TotalSeconds;
                Assert.Fail($"Failed to await log '{log}' in {duration} seconds");
            }
        }
    }
}