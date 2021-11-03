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

  class GitUtility {
    public enum SCMAction {
      ShowInfo,
      PushModified,
      ShowStatus,
      Pull
    };

    /// <summary>
    /// Repository Dir Path
    /// </summary>
    private string RepoPath { get; set; }
    /// <summary>
    /// What Git Action to perform
    /// </summary>
    private SCMAction Action { get; set; }
    /// <summary>
    /// Single file path or pattern files for adding
    /// </summary>
    private string FilePath { get; set; }

    public GitUtility(SCMAction action, string repoPath, string filePath) {
      if (string.IsNullOrEmpty(repoPath))
        repoPath = System.IO.Directory.GetCurrentDirectory();
      else if (! System.IO.Directory.Exists(repoPath))
        throw new ArgumentException("Provide repository path does not exist!");

      Action = action;
      RepoPath = repoPath;
      FilePath = filePath;
    }

    /// <summary>
    /// probably attach to push single
    /// args preprocessing
    /// </summary>
    private bool ValidateCommandLine() {
      if (string.IsNullOrEmpty(FilePath) == false)
      {
        if (FilePath.StartsWith(RepoPath))
          FilePath = FilePath.Substring(RepoPath.Length + 1);

        var prePath = @"input\posts";
        if (RepoPath.EndsWith(@"statiq\note") && FilePath.EndsWith(".md"))
          FilePath = prePath + '\\' + FilePath;
      }
      return true;
    }

    string GetShaShort(Repository repo) => repo.Head.Tip.Id.ToString().Substring(0, 9);

    void ShowUserInfo(Repository repo) {
      Console.WriteLine("Local Repo: " + RepoPath);
      var username = repo.Config.Get<string>("user.name").Value;
      Console.WriteLine("Author: " + username);
      var email = repo.Config.Get<string>("user.email").Value;
      Console.WriteLine("Email: " + email);
      Console.WriteLine("Branch: " + repo.Head);
      Console.WriteLine("SHA: " + GetShaShort(repo));
    }

    void ShowStatus(Repository repo) {
      ShowUserInfo(repo);

      var statusOps = new StatusOptions();
      statusOps.IncludeIgnored = false;

      foreach (var item in repo.RetrieveStatus(statusOps))
        Console.WriteLine(" " + item.FilePath + ": " + item.State);

      Console.WriteLine(Environment.NewLine + "Commit message:");
      Console.WriteLine(GetCommitMessage(@"D:\git_ws\commit_log.txt") + Environment.NewLine);
    }

    /// <summary>
    /// Get commit message from commit log file
    /// </summary>
    private string GetCommitMessage(string path) => System.IO.File.ReadAllText(path);

    async Task PullChanges(Repository repo) {
      // Credential information to fetch
      PullOptions options = new PullOptions();
      options.FetchOptions = new FetchOptions();
      options.FetchOptions.CredentialsProvider = new CredentialsHandler(
          (url, usernameFromUrl, types) =>
              new DefaultCredentials());

      var credManager = new CredManager();
      await credManager.LoadConfig(repo.Config.Get<string>("user.name").Value, repo.Config.
        Get<string>("user.email").Value, RepoPath);
      var signature = credManager.GetSignature();
      try {
        Commands.Pull(repo, signature, options); 
      }
      catch (CheckoutConflictException e) {
          Console.WriteLine(e.Message);
      }
      Console.WriteLine($"{repo.Head} -> {GetShaShort(repo)}");
    }

    async Task PushChanges(Repository repo, bool shouldForce = false) {
      // Add only modified files
      var statusOps = new StatusOptions();
      statusOps.IncludeIgnored = false;

      if (string.IsNullOrEmpty(FilePath) == false) {
        Console.WriteLine("* add " + FilePath);
        // Stage the file
        repo.Index.Add(FilePath);
        repo.Index.Write();
      }
      else
        foreach (var item in repo.RetrieveStatus(statusOps))
        {
            if (item.State == FileStatus.ModifiedInWorkdir)
            {
              Console.WriteLine("adding " + item.FilePath);
              // Stage the file
              repo.Index.Add(item.FilePath);
              repo.Index.Write();
            }
        }

      Console.WriteLine("changes staged");

      var credManager = new CredManager();
      await credManager.LoadConfig(repo.Config.Get<string>("user.name").Value, repo.Config.
        Get<string>("user.email").Value, RepoPath);

      // Create the committer's signature and commit
      Signature author = credManager.GetSignature();
      Signature committer = author;
      var targetBranch = "dev";

      var commitMessage = GetCommitMessage(@"D:\git_ws\commit_log.txt");
      bool hasCommitFailed = false;
      // Commit to the repository
      try {
        Console.WriteLine("author name: " + author.Name);
        Commit commit = repo.Commit(commitMessage, author, committer);
      }
      catch (EmptyCommitException) {
        // Not implemented: no change found, but previous commit was not pushed to remote yet
        Console.WriteLine("Not creating new commit.");

        var originBranchStr = "origin/" + targetBranch;
        if (repo.Head.Tip == repo.Branches[originBranchStr].Tip)
          return ;

        hasCommitFailed = true;
      }

      if (!hasCommitFailed)
        Console.WriteLine("committed");

      PushOptions options = new PushOptions();
      options.CredentialsProvider = credManager.GetCredentials();

      try {
        repo.Network.Push(repo.Branches[targetBranch], options);
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
        Console.WriteLine("Canonical name: " + repo.Branches[targetBranch].CanonicalName);

        var remote = repo.Network.Remotes["origin"];
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
      if (ValidateCommandLine() == false)
        return ;

      try {
        using (var repo = new Repository(RepoPath)) {

          switch (Action) {
          case SCMAction.ShowInfo:
            ShowUserInfo(repo);
            break;

          case SCMAction.ShowStatus:
            ShowStatus(repo);
            break;

          case SCMAction.Pull:
            await PullChanges(repo);
            break;

          case SCMAction.PushModified:
            await PushChanges(repo);
            break;

          default:
            Console.WriteLine("Unknown Action specified!");
            break;
          }
        }
      }
      catch (RepositoryNotFoundException e) {
        Console.WriteLine("Repo dir: " + RepoPath);
        Console.WriteLine(e.Message);
      }
    }
  }
}