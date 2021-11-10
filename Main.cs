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
      var rootCmd = new RootCommand("");

      var pushCmd = new Command("push", "Commit and push");
      // push mod
      var modSubCmd = new Command("mod", "Add only modified files to commit and Push.");
      modSubCmd.AddAlias("modified");

      // 'push mod' with or without '--amend'
      var amendOption = new Option<bool>(new[] {"--amend", "-f"}, "Amend last commit and force push"
        + "!");
      modSubCmd.AddOption(amendOption);

      modSubCmd.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string, bool>(async (repoPath, amend) =>
      {
        if (amend)
          System.Console.WriteLine("Amend/Force flag is set. This will amend last comment and force push "
            + "to remote!");

        await scmAppCLA.Run(GitUtility.SCMAction.PushModified, repoPath, string.Empty, amend);
      });
      pushCmd.AddCommand(modSubCmd);

      // push single
      var singleSubCmd = new Command("single", "Add a file to commit and push.");
      singleSubCmd.AddAlias("single-file");
      singleSubCmd.AddArgument(new Argument("filePath"));

      singleSubCmd.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string, string, bool>(async (repoPath, filePath, amend) =>
      {
        await scmAppCLA.Run(GitUtility.SCMAction.PushModified, repoPath, filePath, amend);
      });
      singleSubCmd.AddOption(amendOption);    // Support --amend switch
      pushCmd.AddCommand(singleSubCmd);

      // push all
      var allSubCmd = new Command("all", "Add all files to commit and push.");

      allSubCmd.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string, bool>(async (repoPath, amend) =>
      {
        // hacky string, consumed by StageGeneric()
        await scmAppCLA.Run(GitUtility.SCMAction.PushModified, repoPath, "__cmdOption:--all", amend);
      });
      allSubCmd.AddOption(amendOption);     // Support --amend switch
      pushCmd.AddCommand(allSubCmd);

      rootCmd.AddCommand(pushCmd);

      var pullCmd = new Command("pull", "Pull changes from repository.");
      pullCmd.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string>(async (repoPath) =>
      {
        await scmAppCLA.Run(GitUtility.SCMAction.Pull, repoPath, string.Empty);
      });
      rootCmd.AddCommand(pullCmd);

      var infoCommand = new Command("info", "Show information about repository.");
      infoCommand.AddAlias("information");
      infoCommand.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string>(async (repoPath) =>
      {
        await scmAppCLA.Run(GitUtility.SCMAction.ShowInfo, repoPath, string.Empty);
      });
      rootCmd.AddCommand(infoCommand);

      var statusCommand = new Command("status", "Show status on changes (including commit message).");
      statusCommand.AddAlias("stat");
      statusCommand.Handler = System.CommandLine.Invocation.CommandHandler
        .Create<string>(async (repoPath) =>
      {
        await scmAppCLA.Run(GitUtility.SCMAction.ShowStatus, repoPath, string.Empty);
      });
      rootCmd.AddCommand(statusCommand);

      // POSIX style arguments
      var rpOption = new Option<string>("--repo-path", "Path of the repo");
      rootCmd.AddOption(rpOption);
      var jcOption = new Option<string>("--config-file-path", "Path of the json configuration file");
      rootCmd.AddOption(jcOption);

      await rootCmd.InvokeAsync(args);
    }

  }

  class SCMAppCLA {
    public SCMAppCLA() {
    }

    /// <summary>
    /// Automaton of the app
    /// </summary>
    public async Task Run(GitUtility.SCMAction action, string repoPath, string filePath, bool shouldAmend=false) {
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
        await app.SCPChanges(filePath, shouldAmend);
        break;

      default:
        throw new System.ArgumentException("Unknown Action specified!");
      }
    }
  }
}
