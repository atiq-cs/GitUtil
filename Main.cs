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
      await scmAppCLA.ConfigureCLA(args);
    }
  }

  class SCMAppCLA {
    public SCMAppCLA() { }

    /// <summary>
    /// Automaton of the app
    /// </summary>
    public async Task ConfigureCLA(string[] args) {
      var rootCmd = new RootCommand();

      var rpOption = new Option<string>(new[] {"--repodir", "-d"}, "Repository directory location / path");
      rootCmd.AddGlobalOption(rpOption);

      var jcOption = new Option<string>(new[] {"--configfilepath", "-c"}, "Path of the json configuration file");
      rootCmd.AddGlobalOption(jcOption);

      rootCmd.AddCommand(GetPushCmd());
      rootCmd.AddCommand(GetPullCmd());

      // TODO: separate these Command Implementations with methods like Push and Pull
      var infoCmd = new Command("info", "Show information about repository.");
      infoCmd.AddAlias("information");
      infoCmd.Handler = CommandHandler
        .Create<string, string>((repodir, configfilepath) =>
      {
        var app = new GitUtility(GitUtility.SCMAction.ShowInfo, ValidateRepoDir(repodir), (configfilepath is null? string.Empty : configfilepath));
        app.ShowRepoAndUserInfo();
      });
      rootCmd.AddCommand(infoCmd);

      var statusCmd = new Command("status", "Show status on changes (including commit message).");
      statusCmd.AddAlias("stat");
      statusCmd.Handler = CommandHandler
        .Create<string, string>((repodir, configfilepath) =>
      {
        var app = new GitUtility(GitUtility.SCMAction.ShowStatus, ValidateRepoDir(repodir), (configfilepath is null? string.Empty : configfilepath));
        app.ShowStatus();
      });
      rootCmd.AddCommand(statusCmd);

      // UpdateRemote: remote URL
      var setUrlCmd = new Command("set-url", "Update remote origin URL.");
      setUrlCmd.AddArgument(new Argument<string>("remoteUrl"));
      // 'set-url' with or without '--upstream'
      setUrlCmd.AddOption(GetUpstreamOption());

      setUrlCmd.Handler = CommandHandler
        .Create<string, bool, string, string>((remoteUrl, upstream, repodir, configfilepath) =>
      {
        var app = new GitUtility(GitUtility.SCMAction.UpdateRemote, ValidateRepoDir(repodir), (configfilepath is null? string.Empty : configfilepath));
        app.UpdateRemoteURL(upstream ? "upstream": "origin", remoteUrl);
      });
      rootCmd.AddCommand(setUrlCmd);

      rootCmd.AddCommand(GetBranchCmd());

      await rootCmd.InvokeAsync(args);
    }

    /// <summary>
    /// Implements the "push" command
    /// - Use bare push to push modified files
    /// - Use '--singlefile' option to push a file
    /// - Use '--all' option to push all
    /// </summary>
    /// <returns>The "push" Command</returns>
    private Command GetPushCmd() {
      var pushCmd = new Command("push", "Commit local changes and push commits to remote.");
      // default push type: Add only modified files to commit and Push when no additional option is
      // specified in command line

      // 'push mod' with or without '--amend'
      var amendOption = new Option<bool>(new[] {"--amend", "-f"}, "Amend last commit and force push"
        + "!");
      pushCmd.AddOption(amendOption);

      // change push type to Single when singlefile is specified on options
      // Used to have an argument with 'single' sub command: file path
      var sfOption = new Option<string>(new[] {"--singlefile", "-s"}, "Add a file to commit and "+
          "push; argument: path of the file to add to commmit!");
      pushCmd.AddOption(sfOption);
      // change push type to All
      var allOption = new Option<bool>(new[] {"--all", "-a"}, "Add all files to commit and push"
        + "!");
      pushCmd.AddOption(allOption);

      pushCmd.Handler = CommandHandler
        .Create<string, bool, bool, string, string>((singlefile, all, amend, repodir, configfilepath) =>
      {
        // Validations
        if ((singlefile is not null) && all)
          throw new System.ArgumentException("Pushing single file and pushing all files are mutually exclusive arguments!");

        var pushOpType = GitUtility.StageType.Update;
        if (singlefile is not null)
          pushOpType = GitUtility.StageType.Single;
        else if (all)
          pushOpType = GitUtility.StageType.All;

        var app = new GitUtility(GitUtility.SCMAction.Push, ValidateRepoDir(repodir), (configfilepath is null? string.Empty : configfilepath));

        if (amend)
          System.Console.WriteLine("Amend/Force flag is set. This will amend last commit and force push "
            + "to remote!");

        switch(pushOpType) {
        case GitUtility.StageType.Update:
        case GitUtility.StageType.All:
          app.SCPChanges(pushOpType, amend);
          break;
        case GitUtility.StageType.Single:
          if (singlefile == string.Empty)
            throw new System.ArgumentException("Argument for --singlefile cannot be empty!");

          // singlefile?? string.Empty: just to ignore nullable warning
          app.SCPSingleChange(singlefile?? string.Empty, amend);
          break;
        default:
          break;
        }
      });

      return pushCmd;
    }

    private Command GetPullCmd() {
      var pullCmd = new Command("pull", "Pull changes from repository.");
      pullCmd.AddOption(GetUpstreamOption());

      pullCmd.Handler = CommandHandler
        .Create<bool, string, string>((upstream, repodir, configfilepath) =>
      {
        var app = new GitUtility(GitUtility.SCMAction.Pull, ValidateRepoDir(repodir), (configfilepath is null? string.Empty : configfilepath));
        app.PullChanges(upstream);
      });
      return pullCmd;
    }

    /// <summary>
    /// Implements the "branch" command
    ///  with options to,
    /// - deletes a branch
    /// - renames a branch
    /// </summary>
    /// <returns>The "branch" Command</returns>
    private Command GetBranchCmd() {
      // DeleteBranch: branch name
      var branchCmd = new Command("branch", "Delete branch from local and remote.");
      // delBrCmd.AddArgument(new Argument<string>("branchName"));
      var deleteOption = new Option<string>(new[] {"--delete", "-d"}, "Delete branch from local "+
        "and remote.");
      branchCmd.AddOption(deleteOption);
      var renameOption = new Option<string>(new[] {"--rename", "-r"}, "Rename branch from local "+
        "and remote.");
      branchCmd.AddOption(renameOption);


      branchCmd.Handler = CommandHandler
        .Create<string, string, string, string>((delete, rename, repodir, configfilepath) =>
      {
        if (delete is not null && rename is not null)
          throw new System.ArgumentException("Delete and rename are mutually exclusive arguments!");

        if (delete is not null) {
          if (delete == string.Empty)
            throw new System.ArgumentException("Branch name (argument for --delete) is missing!");

          var branchName = delete;
          var app = new GitUtility(GitUtility.SCMAction.Branch, ValidateRepoDir(repodir), (configfilepath is null? string.Empty : configfilepath));
          app.DeleteBranch(branchName);
        }

        if (rename is not null) {
          if (rename == string.Empty)
            throw new System.ArgumentException("Branch name (argument for --rename) to rename to is missing!");

          var branchName = rename;
          var app = new GitUtility(GitUtility.SCMAction.Branch, ValidateRepoDir(repodir), (configfilepath is null? string.Empty : configfilepath));
          app.RenameBranch(branchName);
        }
      });

      return branchCmd;
    }

    private string ValidateRepoDir(string repoDir) {
        var repoPath = ".";

        if (repoDir is not null) {
          repoPath = repoDir;

          if (repoDir.EndsWith('\\'))
            repoPath = repoDir.Substring(0, repoPath.Length - 1);
        }

        return repoPath;
    }

    /// <remarks>
    /// Shared by set-url and pull
    /// </remarks>
    private Option<bool> GetUpstreamOption() {
      var upstreamOption = new Option<bool>(new[] {"--upstream", "-u"}, "Whther to use remote upstream !");
      return upstreamOption;
    }
  }
}
