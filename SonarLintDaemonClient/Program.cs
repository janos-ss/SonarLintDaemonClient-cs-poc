using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using Sonarlint;

namespace SonarLintDaemonClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new StandaloneConfiguration();
            config.HomePath = @"c:/work/tmp/daemon/dot.sonarlint";
            config.PluginUrl.Add("file:/c:/dev/git/sonar/sonarlint-intellij/build/idea-sandbox/plugins/SonarLint/classes/plugins/sonar-java-plugin-4.2.1.6971.jar");
            config.PluginUrl.Add("file:/c:/dev/git/sonar/sonarlint-intellij/build/idea-sandbox/plugins/SonarLint/classes/plugins/sonar-javascript-plugin-2.18.0.3454.jar");

            var channel = new Channel("127.0.0.1:8050", ChannelCredentials.Insecure);
            var client = new StandaloneSonarLint.StandaloneSonarLintClient(channel);
            client.Start(config);

            // sanity check ...
            var details = client.GetRuleDetails(new RuleKey { Key = "squid:S1602" });
            Console.WriteLine("rule details = " + details);

            var inputFile = new InputFile();
            inputFile.Path = "c:/dev/git/sonar/sonarlint-core/core/src/main/java/org/sonarsource/sonarlint/core/container/storage/Hello.java";
            inputFile.Charset = "UTF-8";
            inputFile.UserObject = "joe";

            var request = new AnalysisReq();
            request.BaseDir = "c:/work/tmp/daemon";
            request.WorkDir = "c:/work/tmp/daemon";
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
