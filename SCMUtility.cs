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
  /// <summary> ref for enumeration,
  ///  https://stackoverflow.com/q/105372
  /// </summary>
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

    /// <summary>
    /// Get rid of the suffix from git repo path
    /// Otherwise output string comes as following,
    ///  "D:\Code\CoolApp\.git\"
    /// After trimming suffix it becomes,
    ///  "D:\Code\CoolApp"
    /// </summary>
    private string GetRepoPath() {
      var repoPath = Repo.Info.WorkingDirectory;
      if (repoPath == null)
        throw new NotImplementedException($"Most likely a bare repository: {Repo.Info.Path}. It is" + 
        " not tested with this app yet.");

      return repoPath.Substring(0, repoPath.Length-1);
    }


    public void ShowRepoAndUserInfo() {
      Console.WriteLine("Local Repo: " + GetRepoPath());
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
          Get<string>("user.email").Value, GetRepoPath());
      }
    }

    public async Task ShowStatus() {
      ShowRepoAndUserInfo();

      var statusOps = new StatusOptions();
      statusOps.IncludeIgnored = false;

      Console.WriteLine(Environment.NewLine + "Local changes:");
      foreach (var item in Repo.RetrieveStatus(statusOps))
        Console.WriteLine((item.State == FileStatus.ModifiedInWorkdir? "*": " ") + " " + item.FilePath);

      Console.WriteLine(Environment.NewLine + "Commit message (local log file):");

      await InstantiateJsonConfig();
      var msg = GetCommitMessage(singleLine: false);

      Console.WriteLine(msg + Environment.NewLine);
    }

    /// <summary>
    /// Get commit message from commit log file
    /// ref for single line read: read-first-line-of-a-text-file-c-sharp
    ///  https://stackoverflow.com/q/52598516
    /// </summary>
    private string GetCommitMessage(bool singleLine = true) {
      var commitFilePath = Config.GetCommitFilePath();
      if (!System.IO.File.Exists(commitFilePath)) {
        throw new InvalidOperationException($"Log file: {commitFilePath} not found!");
      }

      if (singleLine)
        // Open the file to read from
        using (System.IO.StreamReader sr = System.IO.File.OpenText(commitFilePath)) {
            return sr.ReadLine();;
        }
      else
        return System.IO.File.ReadAllText(commitFilePath);
    }

    public void PullChanges() {
      // Credential information to fetch
      PullOptions options = new PullOptions();
      options.FetchOptions = new FetchOptions();
      options.FetchOptions.CredentialsProvider = new CredentialsHandler(
          (url, usernameFromUrl, types) =>
              new DefaultCredentials());

      var signature = Repo.Config.BuildSignature(DateTimeOffset.Now);
      try {
        Commands.Pull(Repo, signature, options); 
      }
      catch (CheckoutConflictException e) {
          Console.WriteLine(e.Message);
      }
      Console.WriteLine($"{Repo.Head} -> {GetShaShort()}");
    }

    private bool Stage() {
      // Add only modified files
      var statusOps = new StatusOptions();
      statusOps.IncludeIgnored = false;

      bool isModified = false;

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

              if (!isModified)
                isModified = true;
            }
        }

      if (isModified)
        Console.WriteLine("changes staged");

      return isModified;
    }

    /// <summary>
    /// Commit staged changes
    /// - create the committer's signature
    /// - use that signature to commit
    /// * Get Signature from Repo Config ref
    /// * And, Amend ref,
    ///    LibGit2Sharp.Tests/CommitFixture.cs
    /// </summary>
    private async Task Commit(bool shouldAmend = false) {
      Signature signature = Repo.Config.BuildSignature(DateTimeOffset.Now);

      // when shouldAmend is true,
      // don't commit if nothing is staged and commit message is not different

      // Commit to the repository
      Console.WriteLine("author name: " + signature.Name);

      await InstantiateJsonConfig();
      Commit commit = Repo.Commit(GetCommitMessage(), signature, signature,
        new CommitOptions { AmendPreviousCommit = shouldAmend });
      Console.WriteLine("committed");
    }

    /// <summary>
    /// Push commits to remote
    /// </summary>
    private async Task PushToRemote(bool shouldForce = false) {
      var targetBranch = Repo.Head.FriendlyName;
      var originBranchStr = "origin/" + targetBranch;

      if (Repo.Head.Tip == Repo.Branches[originBranchStr].Tip) {
        Console.WriteLine("nothing to push");
        return ;
      }

      PushOptions options = new PushOptions();
      // Config is not instantiated if commit was not called
      await InstantiateJsonConfig();
      options.CredentialsProvider = Config.GetCredentials();

      try {
        if (shouldForce) {
          // TODO: get remote from upstream tracking URL so that remote is not hardcoded
          var remote = Repo.Network.Remotes["origin"];
          if (remote == null)
            throw new LibGit2SharpException("Remote origin not found!");
          Console.WriteLine("name: " + remote.Name);

          string pushRefSpec = string.Format("+{0}:{0}", Repo.Head.CanonicalName);
          Repo.Network.Push(remote, pushRefSpec, options);
        }
        else
          Repo.Network.Push(Repo.Branches[targetBranch], options);
      }
      catch (System.NullReferenceException) {
        Console.WriteLine("Unexpected, since branch is not hardcoded!");
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

      Console.Write("pushed" + (shouldForce? " (forced)" : " "));
    }

    public async Task PushChanges(bool shouldAmend = false) {
      if (Stage() /* || if shouldAmend && commit log changed*/)
        await Commit(shouldAmend);
      await PushToRemote(shouldForce: shouldAmend);
    }
  }
}
