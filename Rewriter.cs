// Copyright (c) iQubit Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace SCMApp {
  using System;
  using System.Collections.Generic;
  using LibGit2Sharp;

  internal class Rewriter {
  private bool succeeding;
    private Exception? error;

    /*/// <summary>
    /// Configuration Instance
    /// </summary>
    private JsonConfig Config { get; set; } */

    public Rewriter(/* SCMAction action, string repoPath, string jsonConfigurationFilePath */) {
      /* if (string.IsNullOrEmpty(repoPath))
        repoPath = System.IO.Directory.GetCurrentDirectory();
      else if (! System.IO.Directory.Exists(repoPath))
        throw new ArgumentException("Provided repository directory path does not exist!");

      try {
        Repo = new Repository(repoPath);
      }
      catch (RepositoryNotFoundException e) {
        Console.WriteLine("Repo dir: " + repoPath + " " + e.Message);

        Console.WriteLine("Initialize a repository in this location: " + repoPath + "?");
        var response = Console.ReadLine();
        if (string.IsNullOrEmpty(response) || response[0] != 'Y' && response[0] != 'y')
          throw ;
        
        // ref, LibGit2Sharp.Tests/PushFixture.cs
        var res = Repository.Init(repoPath, isBare: false);
        // Console.WriteLine("result: " + res);

        Repo = new Repository(repoPath);
        RepoInitStage = true;
        // Cannot do branches Add here since we don't have the commit or commit SHA here..
        // branch = Repo.Branches.Add("dev", commit);        
      }
      // catch any other exception thrown
      catch (LibGit2SharpException e) {
          Console.WriteLine(e.Message);
          throw e;
      }
 
      Action = action;
      Config = new JsonConfig(jsonConfigurationFilePath, Repo.Config.Get<string>("user.name").Value, Repo.Config.
        Get<string>("user.email").Value, GetRepoPath()); */
    }

    /// <summary>
    /// RewriteHistory - Version Control History
    ///  for now, amends author on matched commits
    /// TODO: get rid of Linq queries
    /// ref, `LibGit2Sharp.Tests/FilterBranchFixture.cs`
    /// </summary>
    /// <param>
    /// Repository Instance
    /// </param>
    public void AmendAuthor(string name, string email, Repository repo) {
      Console.WriteLine("Iterating ICommits from Query");

      // Following is overriden by custom implementation
      // Commit[] commits = repo.Commits.QueryBy(new CommitFilter { IncludeReachableFrom = repo.Refs }).ToArray();

      var iCommits = repo.Commits.QueryBy(new CommitFilter());
      var commits = new List<Commit>();
      var iter = iCommits.GetEnumerator();

      // Filter Commits
      while(iter.MoveNext()) {
        var commit = iter.Current;

        // check mis-match on name
        if ((name == string.Empty || (commit.Author.Name == name && commit.Committer.Name == name)) && 
          // check mis-match on email
          (email == string.Empty || (commit.Author.Email == email && commit.Committer.Email == email)))
          continue;
        if (commit.Message.Contains("Initial commit"))
          continue;

        commits.Add(commit);
      }

      int i = 1;
      foreach(var commit in commits) {
        Console.WriteLine($"{i++} #{commit.Sha}: ");
        Console.WriteLine(" Author");
        Console.WriteLine($"  name: {commit.Author.Name}, email: {commit.Author.Email}");
        Console.WriteLine(" Committer");
        Console.WriteLine($"  name: {commit.Committer.Email}, email: {commit.Committer.Email}");
        Console.WriteLine(" Msg: " + commit.MessageShort);
      }

      repo.Refs.RewriteHistory(new RewriteHistoryOptions
      {
        OnError = OnError,
        OnSucceeding = OnSucceeding,
        CommitHeaderRewriter =
          c =>
          CommitRewriteInfo.From(c, author: new Signature(name, email, c.Author.When), committer: new Signature(name, email, c.Committer.When)),
      }, commits);
    }

    // Below 2 implementations are place Holder for now, ref, `LibGit2Sharp.Tests/FilterBranchFixture.cs`
    private Action OnSucceeding {
      get
      {
        succeeding = false;
        return () => succeeding = true;
      }
    }

    private Action<Exception> OnError {
      get
      {
        error = null;
        return ex => error = ex;
      }
    }
  }
}
