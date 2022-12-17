// Copyright (c) iQubit Inc. All rights reserved.
// Use of this source code is governed by a GPL-v3 license

namespace SCMApp {
  using System;
  using System.Threading.Tasks;
  using System.Collections.Generic;
  using System.Text.Json;

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

    /// <summary>
    /// Read and Parse Config file
    ///
    /// <remarks>
    /// <see href="https://docs.microsoft.com/en-us/dotnet/api/system.environment.specialfolder">
    /// Accessing Local App Data via Environment
    /// </see>
    /// </remarks>
    /// <param name="fullNameFromRepo">Full Name in Repo Config</param>
    /// <param name="emailFromRepo">Email Address in Repo Config</param>
    /// <param name="repoPath">Repository Dir/Path</param>
    public JsonConfig(string fullNameFromRepo, string emailFromRepo, string repoPath) {
      JsonConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        + @"\GitUtilConfig.json";

      if (!System.IO.File.Exists(JsonConfigFilePath)) {
        throw new InvalidOperationException($"Required config: {JsonConfigFilePath} not found!" + 
          "Please create the config file and run this application again.");
      }

      UserCred = new UserCredential();

      using System.IO.FileStream openStream = System.IO.File.OpenRead(JsonConfigFilePath);

      // Since we're in constructor we cannot use async. Otherwise `DeserializeAsync`
      var rootElement = JsonSerializer.Deserialize<JsonElement>(openStream);

      // find account to use; based on the dirList
      var servicesJson = rootElement.GetProperty("services");
      var services = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(servicesJson);

      foreach(var service in services) {
        var SCAccountsJson = service.Value;
        var SCAccounts = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(SCAccountsJson);

        foreach(var SCAccount in SCAccounts) {
          JsonElement specifiedDirsJson;
          if (SCAccount.Value.TryGetProperty("Dirs", out specifiedDirsJson)) {
            var dirList = JsonSerializer.Deserialize<HashSet<string>>(specifiedDirsJson);
            if (dirList.Contains(repoPath)) {
              UserCred.UserName = SCAccount.Key;

              // Print selected user to help finding duplicate Dir Entries, in case they exist
              Console.WriteLine($"Selected account: {UserCred.UserName}");

              UserCred.GithubToken = SCAccount.Value.GetProperty("GithubToken").GetString();
              UserCred.Email = SCAccount.Value.GetProperty("Email").GetString();
              UserCred.FullName = SCAccount.Value.GetProperty("FullName").GetString();
              UserCred.CommitLogFilePath = SCAccount.Value.GetProperty("CommitLogFilePath").GetString();

              UserCred.SCProvider = service.Key;
              break;
            }
          }
        }
        if (UserCred.SCProvider != string.Empty)
          break;
      }

      if (UserCred.SCProvider == string.Empty) {
        // get default account
        var appSettingsJson = rootElement.GetProperty("application");
        var defaultSCProvider = appSettingsJson.GetProperty("SCProvider").GetString();
        var defaultUserName = appSettingsJson.GetProperty("UserName").GetString();

        var defaultService = services[defaultSCProvider];
        var defaultAccount = defaultService.GetProperty(defaultUserName);

        UserCred.UserName = defaultUserName;
        UserCred.GithubToken = defaultAccount.GetProperty("GithubToken").GetString();
        UserCred.Email = defaultAccount.GetProperty("Email").GetString();
        UserCred.FullName = defaultAccount.GetProperty("FullName").GetString();
        UserCred.CommitLogFilePath = defaultAccount.GetProperty("CommitLogFilePath").GetString();

        UserCred.SCProvider = defaultSCProvider;
      }

      if (fullNameFromRepo != UserCred.FullName || emailFromRepo != UserCred.Email)
        throw new InvalidOperationException("Inavlid user name or email in git config!");
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
      public HashSet<string>? Dirs { get; set; }

      /// <summary>
      /// Source Control Provider
      /// </summary>
      public string SCProvider { get; set; }

      public UserCredential() {
        // mark that the variable doesn't have an account 'Loaded' into it yet
        SCProvider = string.Empty;
        UserName = string.Empty;
        FullName = string.Empty;
        Email = string.Empty;
        GithubToken = string.Empty;
        CommitLogFilePath = string.Empty;
        Dirs = null;
      }
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
