// Copyright (c) iQubit Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

/// <summary>
/// Source Control Manager Application
/// This name coz we might wanna support other version control systems in future
/// </summary>
namespace SCMApp {
  using System;
  using LibGit2Sharp;
  using LibGit2Sharp.Handlers;
  using System.Threading.Tasks;

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

      ShowCommitMessage(@"D:\git_ws\commit_log.txt");
    }

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
      Console.WriteLine("staging..");

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

      Console.WriteLine("commiting..");
      // show single line instead
      var message = ShowCommitMessage(@"D:\git_ws\commit_log.txt");

      var credManager = new CredManager();
      await credManager.LoadConfig(repo.Config.Get<string>("user.name").Value, repo.Config.
        Get<string>("user.email").Value, RepoPath);

      // Create the committer's signature and commit
      Signature author = credManager.GetSignature();
      Signature committer = author;
      var targetBranch = "dev";

      // Commit to the repository
      try {
        Console.WriteLine("author name: " + author.Name);
        Commit commit = repo.Commit(message, author, committer);
      }
      catch (EmptyCommitException) {
        // Not implemented: no change found, but previous commit was not pushed to remote yet
        Console.WriteLine("Not creating new commit.");

        var originBranchStr = "origin/" + targetBranch;
        if (repo.Head.Tip == repo.Branches[originBranchStr].Tip)
          return ;

        foreach(Branch b in repo.Branches)
          if (b.IsRemote)
            Console.WriteLine(string.Format("{0}{1}", b.IsCurrentRepositoryHead ? "*" : " ", b.FriendlyName + ": " + b.Tip));
      }

      Console.WriteLine("pushing to remote..");
      PushOptions options = new PushOptions();
      options.CredentialsProvider = credManager.GetCredentials();

      try {
        var localBranch = repo.Branches[targetBranch];
        repo.Network.Push(localBranch, options);
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
        return ;
      }

      Console.WriteLine("done");
    }

    /// <summary>
    /// Show commit message from file
    /// </summary>
    string ShowCommitMessage(string path) {
      Console.WriteLine(Environment.NewLine + "Commit message:");
      var msg = System.IO.File.ReadAllText(path);
      Console.WriteLine(msg + Environment.NewLine);
      return msg;
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