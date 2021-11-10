// Copyright (c) iQubit Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace SCMApp {
  using System;
  using System.Threading.Tasks;
  using System.Collections.Generic;

  /// <summary>
  /// Credentials Related
  /// Provides
  /// - signature for commits
  /// - identity for pushes
  /// - provide commit log file path
  /// - performs some validation checks
  /// </summary>
  class JsonConfig {
    private UserCredential UserCred {get; set; }
    /// <summary>
    /// Config file path
    /// </summary>
    private string JsonConfigFilePath { get; set; }


    /// <remarks>
    /// <see href="https://docs.microsoft.com/en-us/dotnet/api/system.environment.specialfolder">
    /// Accessing Local App Data via Environment
    /// </see>
    /// </remarks>
    public JsonConfig() {
      JsonConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        + @"\GitUtilConfig.json";

      if (!System.IO.File.Exists(JsonConfigFilePath)) {
        throw new InvalidOperationException($"Required config: {JsonConfigFilePath} not found!" + 
          "Please create the config file and run this application again.");
      }
    }

    /// <summary>
    /// Structure to read records from config file (json format)
    /// </summary>
    class UserCredential {
      /// <summary>
      /// Github user name for example, 'coolgeek'
      /// </summary>
      public string UserName { get; set; }
      /// <summary>
      /// Actual Name
      /// <example> Esther Arkin </example>
      /// </summary>
      public string FullName { get; set; }
      public string Email { get; set; }
      public string GithubToken { get; set; }
      public string CommitLogFilePath { get; set; }
      public HashSet<string> Dirs { get; set; }
    }


    /// <summary>
    /// Read and Parse Config file
    /// </summary>
    /// <param name="fullNameFromRepo">Full Name in Repo Config</param>
    /// <param name="emailFromRepo">Email Address in Repo Config</param>
    /// <param name="repoPath">Repository Dir/Path</param>
    public async Task Load(string fullNameFromRepo, string emailFromRepo, string repoPath) {
      using System.IO.FileStream openStream = System.IO.File.OpenRead(JsonConfigFilePath);

      var root = await System.Text.Json.JsonSerializer.DeserializeAsync<Dictionary
        <string, UserCredential>>(openStream);

      if (! root.ContainsKey("default"))
        throw new InvalidOperationException($"Invalid json config file '{JsonConfigFilePath}': field " +
          "'default' missing!");

      if (! root.ContainsKey("specified"))
        throw new InvalidOperationException($"Invalid json config file '{JsonConfigFilePath}': field " +
          "'specified' missing!");

      UserCred = root["default"];
      var spCred = root["specified"];
      var dirList = spCred.Dirs;

      if (dirList == null)
        throw new InvalidOperationException($"Invalid json config file '{JsonConfigFilePath}': "
          + "field 'Dirs' missing!");

      if (dirList.Contains(repoPath)) {
        Console.WriteLine("Setting specified config for this repository.");

        UserCred = spCred;
      }

      if (fullNameFromRepo != UserCred.FullName || emailFromRepo != UserCred.Email)
        throw new InvalidOperationException("Inavlid user name or email in git config!");
    }

    /// <summary>
    /// Construct CredentialsHandler for <code>Network.Push</code>
    /// </summary>
    /// <remarks>
    /// <see href="https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to">
    /// Read from json file MS Docs ref
    /// </see>
    /// </remarks>
    /// <returns>LibGit2Sharp.CredentialsHandler</returns>
    public LibGit2Sharp.Handlers.CredentialsHandler GetCredentials() {
      return new LibGit2Sharp.Handlers.CredentialsHandler(
          (url, usernameFromUrl, types) => new LibGit2Sharp.UsernamePasswordCredentials()
          {
            Username = UserCred.UserName,
            Password = UserCred.GithubToken
          });
    }

    public string GetCommitFilePath() => UserCred.CommitLogFilePath;
  }
}
