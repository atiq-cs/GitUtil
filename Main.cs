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
      var scmAppCLA = new SCMAppCLA();
      var rootCommand = new RootCommand("");

      var pushCommand = new Command("push", "Commit and push");
      var pushModCmd = new Command("mod", "type of push");
      pushModCmd.AddAlias("modified");

      // `push mod` and `push mod --amend`
      var amendOption = new Option<bool>(new[] {"--amend", "-f"}, "whether to amend last commit and"
        + " add current changes along");
      pushModCmd.AddOption(amendOption);

      pushCommand.AddCommand(pushModCmd);
      pushModCmd.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string, bool>(async (repoPath, amend) =>
      {
        if (amend)
          System.Console.WriteLine("this will amend last comment and force push to remote");

        await scmAppCLA.Run(GitUtility.SCMAction.PushModified, repoPath, amend);
      });

      rootCommand.AddCommand(pushCommand);

      var pullCommand = new Command("pull", "Pull changes from repository");
      pullCommand.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string>(async (repoPath) =>
      {
        await scmAppCLA.Run(GitUtility.SCMAction.Pull, repoPath);
      });
      rootCommand.AddCommand(pullCommand);

      var infoCommand = new Command("info", "Show information about repository");
      infoCommand.AddAlias("information");
      infoCommand.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string>(async (repoPath) =>
      {
        await scmAppCLA.Run(GitUtility.SCMAction.ShowInfo, repoPath);
      });
      rootCommand.AddCommand(infoCommand);

      var statusCommand = new Command("status", "Show status on changes/message");
      statusCommand.AddAlias("stat");
      statusCommand.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string>(async (repoPath) =>
      {
        await scmAppCLA.Run(GitUtility.SCMAction.ShowStatus, repoPath);
      });
      rootCommand.AddCommand(statusCommand);

      // POSIX style arguments
      var rpOption = new Option<string>("--repo-path", "Path of the repo");
      rootCommand.AddOption(rpOption);
      var jcOption = new Option<string>("--config-file-path", "Path of the json configuration file");
      rootCommand.AddOption(jcOption);

      await rootCommand.InvokeAsync(args);
    }

  }

  class SCMAppCLA {
    public SCMAppCLA() {
    }

    /// <summary>
    /// Automaton of the app
    /// </summary>
    public async Task Run(GitUtility.SCMAction action, string repoPath, bool shouldAmend=false) {
      if (repoPath.EndsWith('\\'))
        repoPath = repoPath.Substring(0, repoPath.Length - 1);

      var app = new GitUtility(action, repoPath, string.Empty);
      switch (action) {
      case GitUtility.SCMAction.ShowInfo:
        app.ShowRepoAndUserInfo();
        break;

      case GitUtility.SCMAction.ShowStatus:
        await app.ShowStatus();
        break;

      case GitUtility.SCMAction.Pull:
        app.PullChanges();
        break;

      case GitUtility.SCMAction.PushModified:
        await app.PushChanges(shouldAmend);
        break;

      default:
        throw new System.ArgumentException("Unknown Action specified!");
      }
    }
  }
}
