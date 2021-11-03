// Copyright (c) iQubit Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace SCMApp {
  using System;
  using LibGit2Sharp;
  using LibGit2Sharp.Handlers;
  using System.Threading.Tasks;

  /// <summary> Source Control Manager Application
  /// This name coz we might wanna support other version control systems in future
  /// </summary>
  internal static class NamespaceDoc {
  }

  internal class GitUtility {
    public enum SCMAction {
      ShowInfo,
      PushModified,
      ShowStatus,
      Pull
    };

    /// <summary>
    /// Repository Instance
    /// </summary>
    private Repository Repo { get; set; }
    /// <summary>
    /// What Git Action to perform
    /// </summary>
    private SCMAction Action { get; set; }
    /// <summary>
    /// Single file path or pattern files for adding
    /// </summary>
    private string FilePath { get; set; }
    /// <summary>
    /// Configuration Instance
    /// </summary>
    private JsonConfig Config { get; set; }

    public GitUtility(SCMAction action, string repoPath, string filePath) {
      if (string.IsNullOrEmpty(repoPath))
        repoPath = System.IO.Directory.GetCurrentDirectory();
      else if (! System.IO.Directory.Exists(repoPath))
        throw new ArgumentException("Provide repository path does not exist!");

      try {
        Repo = new Repository(repoPath);
      }
      catch (RepositoryNotFoundException e) {
        Console.WriteLine("Repo dir: " + repoPath);
        Console.WriteLine(e.Message);
      }
 
      Action = action;
      
      FilePath = filePath;
      if (string.IsNullOrEmpty(FilePath) == false)
      {
        if (FilePath.StartsWith(repoPath))
          FilePath = FilePath.Substring(repoPath.Length + 1);

        var prePath = @"input\posts";
        if (repoPath.EndsWith(@"statiq\note") && FilePath.EndsWith(".md"))
          FilePath = prePath + '\\' + FilePath;
      }

      Config = null;
    }

    /// <summary>
    /// Get short commit sha id: 9 alpha digits
    /// </summary>
    private string GetShaShort() => Repo.Head.Tip.Id.ToString().Substring(0, 9);

    private void ShowUserInfo() {
      Console.WriteLine("Local Repo: " + Repo.Info);
      var username = Repo.Config.Get<string>("user.name").Value;
      Console.WriteLine("Author: " + username);
      var email = Repo.Config.Get<string>("user.email").Value;
      Console.WriteLine("Email: " + email);
      Console.WriteLine("Branch: " + Repo.Head);
      Console.WriteLine("SHA: " + GetShaShort());
    }

    private async Task InstantiateJsonConfig() {
      if (Config == null) {
        Config = new JsonConfig();
        await Config.Load(Repo.Config.Get<string>("user.name").Value, Repo.Config.
          Get<string>("user.email").Value, Repo.Info.Path);
      }
    }

    private async Task ShowStatus() {
      ShowUserInfo();

      var statusOps = new StatusOptions();
      statusOps.IncludeIgnored = false;

      foreach (var item in Repo.RetrieveStatus(statusOps))
        Console.WriteLine(" " + item.FilePath + ": " + item.State);

      Console.WriteLine(Environment.NewLine + "Commit message:");

      await InstantiateJsonConfig();
      var msg = GetCommitMessage();

      Console.WriteLine(msg + Environment.NewLine);
    }

    /// <summary>
    /// Get commit message from commit log file
    /// </summary>
    private string GetCommitMessage() {
      var commitFilePath = Config.GetCommitFilePath();
      if (!System.IO.File.Exists(commitFilePath)) {
        throw new InvalidOperationException($"Log file: {commitFilePath} not found!");
      }

      return System.IO.File.ReadAllText(commitFilePath);
    }

    private async Task PullChanges() {
      // Credential information to fetch
      PullOptions options = new PullOptions();
      options.FetchOptions = new FetchOptions();
      options.FetchOptions.CredentialsProvider = new CredentialsHandler(
          (url, usernameFromUrl, types) =>
              new DefaultCredentials());

      await InstantiateJsonConfig();
      var signature = Config.GetSignature();
      try {
        Commands.Pull(Repo, signature, options); 
      }
      catch (CheckoutConflictException e) {
          Console.WriteLine(e.Message);
      }
      Console.WriteLine($"{Repo.Head} -> {GetShaShort()}");
    }

    private async Task PushChanges(bool shouldForce = false) {
      // Add only modified files
      var statusOps = new StatusOptions();
      statusOps.IncludeIgnored = false;

      if (string.IsNullOrEmpty(FilePath) == false) {
        Console.WriteLine("* add " + FilePath);
        // Stage the file
        Repo.Index.Add(FilePath);
        Repo.Index.Write();
      }
      else
        foreach (var item in Repo.RetrieveStatus(statusOps))
        {
            if (item.State == FileStatus.ModifiedInWorkdir)
            {
              Console.WriteLine("adding " + item.FilePath);
              // Stage the file
              Repo.Index.Add(item.FilePath);
              Repo.Index.Write();
            }
        }

      Console.WriteLine("changes staged");

      await InstantiateJsonConfig();
      // Create the committer's signature and commit
      Signature author = Config.GetSignature();
      Signature committer = author;
      var targetBranch = "dev";

      bool hasCommitFailed = false;
      // Commit to the repository
      try {
        Console.WriteLine("author name: " + author.Name);
        Commit commit = Repo.Commit(GetCommitMessage(), author, committer);
      }
      catch (EmptyCommitException) {
        // Not implemented: no change found, but previous commit was not pushed to remote yet
        Console.WriteLine("Not creating new commit.");

        var originBranchStr = "origin/" + targetBranch;
        if (Repo.Head.Tip == Repo.Branches[originBranchStr].Tip)
          return ;

        hasCommitFailed = true;
      }

      if (!hasCommitFailed)
        Console.WriteLine("committed");

      PushOptions options = new PushOptions();
      // Config is instantiated already before commit.
      options.CredentialsProvider = Config.GetCredentials();

      try {
        Repo.Network.Push(Repo.Branches[targetBranch], options);
      }
      catch (System.NullReferenceException) {
        Console.WriteLine("Probably attempting push to wrong branch!");
        return ;
      }
      catch (NonFastForwardException) {
        Console.WriteLine("Not attempting fast forward, aborting!");
        return ;
      }
      catch(LibGit2SharpException e) {
        Console.WriteLine("Probable authentication error: " + e.Message);

        // stuffs to help debugging
        Console.WriteLine("Canonical name: " + Repo.Branches[targetBranch].CanonicalName);

        var remote = Repo.Network.Remotes["origin"];
        Console.WriteLine("URL: " + remote.Url);
        Console.WriteLine("Push URL: " + remote.PushUrl);
        return ;
      }

      Console.WriteLine("pushed");
    }

    /// <summary>
    /// Automaton of the app
    /// 
    /// ref for enumeration,
    ///  https://stackoverflow.com/q/105372
    /// </summary>
    public async Task Run() {
      switch (Action) {
      case SCMAction.ShowInfo:
        ShowUserInfo();
        break;

      case SCMAction.ShowStatus:
        await ShowStatus();
        break;

      case SCMAction.Pull:
        await PullChanges();
        break;

      case SCMAction.PushModified:
        await PushChanges();
        break;

      default:
        Console.WriteLine("Unknown Action specified!");
        break;
      }
    }
  }
}