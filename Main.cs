// Copyright (c) iQubit Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace SCMApp {
  using System.Threading.Tasks;
  using System.CommandLine;
  using System.CommandLine.NamingConventionBinder;

  class SCMAppMain {
    /// <summary>
    /// Entry Point
    /// In this source file, we handle command line arguments
    /// - define handlers
    /// - follow CLA Design wiki
    /// </summary>
    /// <param name="args">CLA</param>
    /// <returns></returns>
    static async Task Main(string[] args)
    {
      var scmAppCLA = new SCMAppCLA();
      var rootCmd = new RootCommand();

      var pushCmd = new Command("push", "Commit and push");
      // push mod
      var modSubCmd = new Command("mod", "Add only modified files to commit and Push.");
      modSubCmd.AddAlias("modified");

      // 'push mod' with or without '--amend'
      var amendOption = new Option<bool>(new[] {"--amend", "-f"}, "Amend last commit and force push"
        + "!");
      // delete if -f works with above code
      // var amendOption = new Option<bool>("--amend", "Amend last commit and force push"
      //   + "!");
      // amendOption.AddAlias("-f");
      modSubCmd.AddOption(amendOption);

      modSubCmd.Handler = CommandHandler
        .Create<string, bool>((repoPath, amend) =>
      {
        if (amend)
          System.Console.WriteLine("Amend/Force flag is set. This will amend last comment and force push "
            + "to remote!");

        scmAppCLA.Run(GitUtility.SCMAction.PushModified, repoPath, string.Empty, amend);
      });
      pushCmd.AddCommand(modSubCmd);

      // push single
      var singleSubCmd = new Command("single", "Add a file to commit and push.");
      singleSubCmd.AddAlias("single-file");
      // Argument does not have an AddAlias method
      singleSubCmd.AddArgument(new Argument<string>("filePath", "Path of the file to add to commmit."));

      singleSubCmd.Handler = CommandHandler
        .Create<string, string, bool>((repoPath, filePath, amend) =>
      {
        scmAppCLA.Run(GitUtility.SCMAction.PushModified, repoPath, filePath, amend);
      });
      singleSubCmd.AddOption(amendOption);    // Support --amend switch
      pushCmd.AddCommand(singleSubCmd);

      // push all
      var allSubCmd = new Command("all", "Add all files to commit and push.");

      allSubCmd.Handler = CommandHandler
        .Create<string, bool>((repoPath, amend) =>
      {
        // hacky string, consumed by StageGeneric()
        scmAppCLA.Run(GitUtility.SCMAction.PushModified, repoPath, "__cmdOption:--all", amend);
      });
      allSubCmd.AddOption(amendOption);     // Support --amend switch
      pushCmd.AddCommand(allSubCmd);

      rootCmd.AddCommand(pushCmd);

      var pullCmd = new Command("pull", "Pull changes from repository.");
      var upstreamOption = new Option<bool>(new[] {"--upstream", "-u"}, "pull from upstream");
      pullCmd.AddOption(upstreamOption);
      pullCmd.Handler = CommandHandler
        .Create<string, bool>((repoPath, upstream) =>
      {
        scmAppCLA.Run(GitUtility.SCMAction.Pull, repoPath, string.Empty, upstream);
      });
      rootCmd.AddCommand(pullCmd);

      var infoCmd = new Command("info", "Show information about repository.");
      infoCmd.AddAlias("information");
      infoCmd.Handler = CommandHandler
        .Create<string>((repoPath) =>
      {
        scmAppCLA.Run(GitUtility.SCMAction.ShowInfo, repoPath, string.Empty);
      });
      rootCmd.AddCommand(infoCmd);

      var statusCmd = new Command("status", "Show status on changes (including commit message).");
      statusCmd.AddAlias("stat");
      statusCmd.Handler = CommandHandler
        .Create<string>((repoPath) =>
      {
        scmAppCLA.Run(GitUtility.SCMAction.ShowStatus, repoPath, string.Empty);
      });
      rootCmd.AddCommand(statusCmd);

      var setUrlCmd = new Command("set-url", "Update remote origin URL.");
      setUrlCmd.AddArgument(new Argument<string>("remoteUrl"));
      // 'set-url' with or without '--upstream'
      upstreamOption = new Option<bool>(new[] {"--upstream", "-u"}, "Set remote upstream URL!");
      setUrlCmd.AddOption(upstreamOption);

      setUrlCmd.Handler = CommandHandler
        .Create<string, string, bool>((repoPath, remoteUrl, upstream) =>
      {
        scmAppCLA.Run(GitUtility.SCMAction.UpdateRemote, repoPath, remoteUrl, upstream);
      });
      rootCmd.AddCommand(setUrlCmd);

      var delBrCmd = new Command("delete-branch", "Delete branch from local and remote.");
      delBrCmd.AddArgument(new Argument<string>("branchName"));

      delBrCmd.Handler = CommandHandler
        .Create<string, string>((repoPath, branchName) =>
      {
        scmAppCLA.Run(GitUtility.SCMAction.DeleteBranch, repoPath, branchName, false);
      });
      rootCmd.AddCommand(delBrCmd);

      // POSIX style arguments
      var rpOption = new Option<string>("--repo-path", "Path of the repo");
      rootCmd.AddOption(rpOption);
      var jcOption = new Option<string>("--config-file-path", "Path of the json configuration file");
      rootCmd.AddOption(jcOption);

      await rootCmd.InvokeAsync(args);
    }
  }

  class SCMAppCLA {
    public SCMAppCLA() { }

    /// <summary>
    /// Automaton of the app
    /// </summary>
    /// <remarks>
    /// Usage of Ambiguous parameters -- horrible design
    ///  TODO, probably get rid of this Run Method and call these app::methods directly
    /// </remarks>
    /// <param name="firstParam">
    /// Polymorphic parameter, refers to different things based on different action:
    ///  - DeleteBranch: branch name
    ///  - UpdateRemote: remote URL
    ///  - PushModified:
    ///     file path      when 'push mod single'
    ///     special string when 'push mod all'
    /// </param>
    public void Run(GitUtility.SCMAction action, string repoPath, string strParam,
      bool bParam=false)
    {
      if (repoPath == null)
        repoPath = ".";

      if (repoPath.EndsWith('\\'))
        repoPath = repoPath.Substring(0, repoPath.Length - 1);

      var app = new GitUtility(action, repoPath, string.Empty);

      switch (action) {
      case GitUtility.SCMAction.PushModified:
        app.SCPChanges(strParam, bParam);
        break;

      case GitUtility.SCMAction.DeleteBranch:
        app.DeleteBranch(strParam);
        break;

      case GitUtility.SCMAction.UpdateRemote:
        app.UpdateRemoteURL(bParam ? "upstream": "origin", strParam);
        break;

      case GitUtility.SCMAction.Pull:
        app.PullChanges(bParam);
        break;

      case GitUtility.SCMAction.ShowInfo:
        app.ShowRepoAndUserInfo();
        break;

      case GitUtility.SCMAction.ShowStatus:
        app.ShowStatus();
        break;

      default:
        throw new System.ArgumentException("Unknown Action specified!");
      }
    }
  }
}
