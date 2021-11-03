// Copyright (c) iQubit Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace SCMApp {
  using System.Threading.Tasks;
  using System.CommandLine;

  class SCMAppMain {
    /// <summary>
    /// Entry Point
    /// In this source file, we handle command line arguments
    /// - define handlers
    /// - follow CLA Design wiki
    /// </summary>
    /// <param name="args">CLA</param>
    /// <returns></returns>
    static async Task Main(string[] args) {
      var rootCommand = new RootCommand("");

      var pushCommand = new Command("push", "Commit and push");
      var pushModCmd = new Command("mod", "type of push");
      pushModCmd.AddAlias("modified");
      var amendOption = new Option<string>("--amend", "whether to amend last commit and add current changes");
      pushModCmd.AddOption(amendOption);

      pushCommand.AddCommand(pushModCmd);
      pushModCmd.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string, bool>(async (repoPath, shouldAmend) =>
      {
        await RunSCM(GitUtility.SCMAction.PushModified, repoPath);
      });

      var rpOption = new Option<string>("--repo-path", "Path of the repo");
      pushModCmd.AddOption(rpOption);
      rootCommand.AddCommand(pushCommand);

      var pullCommand = new Command("pull", "Pull changes from repository");
      pullCommand.AddOption(rpOption);
      pullCommand.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string>(async (repoPath) =>
      {
        await RunSCM(GitUtility.SCMAction.Pull, repoPath);
      });
      rootCommand.AddCommand(pullCommand);

      var infoCommand = new Command("info", "Show information about repository");
      infoCommand.AddAlias("information");
      infoCommand.AddOption(rpOption);
      infoCommand.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string>(async (repoPath) =>
      {
        await RunSCM(GitUtility.SCMAction.ShowInfo, repoPath);
      });
      rootCommand.AddCommand(infoCommand);

      var statusCommand = new Command("status", "Show status on changes/message");
      statusCommand.AddAlias("stat");
      statusCommand.AddOption(rpOption);
      statusCommand.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string>(async (repoPath) =>
      {
        await RunSCM(GitUtility.SCMAction.ShowStatus, repoPath);
      });
      rootCommand.AddCommand(statusCommand);

      await rootCommand.InvokeAsync(args);
    }

    public static async Task RunSCM(GitUtility.SCMAction action, string repoPath) {
      if (repoPath.EndsWith('\\'))
        repoPath = repoPath.Substring(0, repoPath.Length - 1);

      var app = new GitUtility(action, repoPath, string.Empty);
      await app.Run();
    }
  }
}
