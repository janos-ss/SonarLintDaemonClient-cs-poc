using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Sonarlint;
using System.IO;

namespace SonarLintDaemonClient
{
    class Program
    {
        static readonly string DAEMON_HOST = "localhost";
        static readonly int DAEMON_PORT = 8050;

        static void Main(string[] args)
        {
            var tmpdir = Path.Combine(Path.GetTempPath(), "SonarLintDaemonClient");
            var resourcesBasePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../../resources"));

            var config = new StandaloneConfiguration();
            config.HomePath = Path.Combine(tmpdir, "dot.sonarlint");
            config.PluginUrl.Add(ToJavaUrlString(Path.Combine(resourcesBasePath, "sonar-java-plugin-4.2.1.6971.jar")));
            config.PluginUrl.Add(ToJavaUrlString(Path.Combine(resourcesBasePath, "sonar-javascript-plugin-2.18.0.3454.jar")));

            var channel = new Channel(string.Join(":", DAEMON_HOST, DAEMON_PORT), ChannelCredentials.Insecure);
            var client = new StandaloneSonarLint.StandaloneSonarLintClient(channel);
            client.Start(config);

            // sanity check ...
            var details = client.GetRuleDetails(new RuleKey { Key = "squid:S1602" });
            Console.WriteLine("rule details = " + details);

            var inputFile1 = new InputFile();
            inputFile1.Path = Path.Combine(resourcesBasePath, "Hello.java");
            inputFile1.Charset = "UTF-8";
            //inputFile1.UserObject = "joe";

            var inputFile2 = new InputFile();
            inputFile2.Path = Path.Combine(resourcesBasePath, "Hello.js");
            inputFile2.Charset = "UTF-8";
            inputFile2.UserObject = "jack";

            var request = new AnalysisReq();
            request.BaseDir = Path.Combine(tmpdir, "dummy-basedir");
            Directory.CreateDirectory(request.BaseDir);
            request.WorkDir = Path.Combine(tmpdir, "dummy-workdir");
            request.File.Add(inputFile1);
            request.File.Add(inputFile2);

            using (var call = client.Analyze(request))
            {
                ProcessIssues(call).Wait();
            }

            channel.ShutdownAsync().Wait();
        }

        private static async Task ProcessIssues(AsyncServerStreamingCall<Issue> call)
        {
            while (await call.ResponseStream.MoveNext())
            {
                Issue issue = call.ResponseStream.Current;
                Console.WriteLine(issue);
            }
        }

        private static string ToJavaUrlString(string path)
        {
            return "file:/" + path;
        }
    }
}
