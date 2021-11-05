namespace SCMApp {
  using System;
  using LibGit2Sharp;
  using LibGit2Sharp.Handlers;
  using System.Text.Json;
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
    /// Github user name for example, 'coolgeek'
    /// </summary>
    private string JsonConfigFilePath { get; set; }


    public JsonConfig() {
      JsonConfigFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        + @"\GitUtilConfig.json";

      if (!System.IO.File.Exists(JsonConfigFilePath)) {
        throw new InvalidOperationException($"Required config: {JsonConfigFilePath} not found!" + 
          "Please create the config file and run this application again.");
      }
    }

    /// <summary>
    /// Structure to read json config
    /// </summary>
    class UserCredential {
    /// <summary>
    /// Github user name for example, 'coolgeek'
    /// </summary>
      public string UserName { get; set; }
    /// <summary>
    /// Actual Name for example, 'Esther Arkin' 
    /// </summary>
      public string FullName { get; set; }
      public string Email { get; set; }
      public string GithubToken { get; set; }
      public string CommitLogFilePath { get; set; }
      public HashSet<string> Dirs { get; set; }
    }

    /// <summary>
    /// <remarks>
    /// ref,
    ///  https://docs.microsoft.com/en-us/dotnet/api/system.environment.specialfolder
    /// </remarks>
    /// </summary>
    public async Task Load(string repoFullName, string repoEmail, string RepoPath) {
      using System.IO.FileStream openStream = System.IO.File.OpenRead(JsonConfigFilePath);
      var root = await JsonSerializer.DeserializeAsync<Dictionary<string, UserCredential>>(openStream);

      if (! root.ContainsKey("default"))
        throw new InvalidOperationException($"Invalid json config file '{JsonConfigFilePath}': field " +
          "'default' missing!");

      if (! root.ContainsKey("specified"))
        throw new InvalidOperationException($"Invalid json config file '{JsonConfigFilePath}': field " +
          "'specified' missing!");

      UserCred = root["default"];
      var spCred = root["specified"];
      var dirList = spCred.Dirs;
      if (dirList == null) {
        throw new InvalidOperationException($"Invalid json config file '{JsonConfigFilePath}': field "
        + "'Dirs' missing!");
      }

      if (dirList.Contains(RepoPath)) {
        Console.WriteLine("Setting specified config for this repository.");

        UserCred = spCred;
      }

      if (repoFullName != UserCred.FullName || repoEmail != UserCred.Email)
        throw new InvalidOperationException("Inavlid user name or email in git config!");
    }

    /// <summary>
    /// Read Credentials from json file
    /// ref, https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to
    /// </summary>
    public CredentialsHandler GetCredentials() {
      Console.WriteLine($"{UserCred.UserName} and {UserCred.GithubToken}");

      return new CredentialsHandler(
          (url, usernameFromUrl, types) => new UsernamePasswordCredentials()
          {
            Username = UserCred.UserName,
            Password = UserCred.GithubToken
          });
    }

    public string GetCommitFilePath() => UserCred.CommitLogFilePath;
  }
}
