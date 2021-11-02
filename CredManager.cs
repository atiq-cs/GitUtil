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
  /// - performs some validation checks
  /// </summary>
  class CredManager {
    /// <summary>
    /// Github user name for example, 'coolgeek'
    /// </summary>
    private string UserName { get; set; }
    /// <summary>
    /// Actual Name for example, 'Esther Arkin' 
    /// </summary>
    private string FullName { get; set; }
    private string Email { get; set; }
    /// <summary>
    /// Github Token
    /// </summary>
    private string GHToken { get; set; }

    public CredManager() {
    }

    // required structure to read json config
    class RepoCred {
      public string UserName { get; set; }
      public string FullName { get; set; }
      public string Email { get; set; }
      public string GithubToken { get; set; }
      public HashSet<string> Dirs { get; set; }
    }


    public async Task LoadConfig(string repoFullName, string repoEmail, string RepoPath) {
      var appDataDir = System.IO.Directory.GetParent(Environment.GetFolderPath(Environment.
        SpecialFolder.ApplicationData)) + @"\Local";

      string filePath = appDataDir + @"\GitUtilConfig.json";
      // Console.WriteLine(filePath);
      using System.IO.FileStream openStream = System.IO.File.OpenRead(filePath);
      var root = await JsonSerializer.DeserializeAsync<Dictionary<string, RepoCred>>(openStream);

      if (! root.ContainsKey("default")) {
        throw new InvalidOperationException("Invalid json config file: default cred missing!");
      }

      if (! root.ContainsKey("specified")) {
        throw new InvalidOperationException("Invalid json config file: specified cred missing!");
      }

      var defaultCred = root["default"];
      UserName = defaultCred.UserName;
      FullName = defaultCred.FullName;
      Email = defaultCred.Email;
      GHToken = defaultCred.GithubToken;

      var spCred = root["specified"];
      var dirList = spCred.Dirs;

      if (dirList.Contains(RepoPath)) {
        Console.WriteLine("Setting specified config for this repository.");
        UserName = spCred.UserName;
        FullName = spCred.FullName;
        Email = spCred.Email;
        GHToken = spCred.GithubToken;
      }

      if (repoFullName != FullName || repoEmail != Email)
        throw new InvalidOperationException("Inavlid user name or email in git config!");
    }

    public Signature GetSignature() {
      // ShowUserInfo(repo);
      var signature = new Signature(new Identity(FullName, Email), DateTimeOffset.Now);
      return signature;
    }


    /// <summary>
    /// Read Credentials from json file
    /// ref, https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-how-to
    /// </summary>
    public CredentialsHandler GetCredentials() {
      Console.WriteLine($"{UserName} and {GHToken}");

      return new CredentialsHandler(
          (url, usernameFromUrl, types) => new UsernamePasswordCredentials()
          {
            Username = UserName,
            Password = GHToken
          });
    }
  }
}
