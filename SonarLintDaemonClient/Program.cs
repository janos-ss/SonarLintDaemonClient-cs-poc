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

            var channel = new Channel(string.Join(":", DAEMON_HOST, DAEMON_PORT), ChannelCredentials.Insecure);
            var client = new StandaloneSonarLint.StandaloneSonarLintClient(channel);

            // sanity check ...
            var details = client.GetRuleDetails(new RuleKey { Key = "javascript:S2757" });
            Console.WriteLine("rule details = " + details);

            var inputFile = new InputFile();
            inputFile.Path = Path.Combine(resourcesBasePath, "Hello.js");
            inputFile.Path = @"c:/Users/Janos Gyerik/Documents/Visual Studio 2015/Projects/SonarLintDaemonClient/resources/Hello.js";
            inputFile.Charset = "UTF-8";
            //inputFile2.UserObject = "jack";

            var request = new AnalysisReq();
            request.BaseDir = Path.Combine(tmpdir, "dummy-basedir");
            Directory.CreateDirectory(request.BaseDir);
            request.WorkDir = Path.Combine(tmpdir, "dummy-workdir");
            request.File.Add(inputFile);

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
    }
}
