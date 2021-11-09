## Git Utility
Git Utility to handle multiple git accounts.
A utility to automate some of the commonly used Git Actions.

For example, to push changes to a git repository, this is a common sequence of commands,

    git add --update
    git commit --file commit_log.txt
    git push origin BRANCH_NAME

This utility does all of that when we run,

    GitUtil push mod

The tool requires an initial json config file to start with. The path of config file is:
`%LOCALAPPDATA%\GitUtilConfig.json`

Example json config,

    {
        "github.com/USER_NAME_1": {
            "GithubToken": "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
            "FullName": "First Last",
            "Email": "user@github.com",
            "CommitLogFilePath": "D:\\code\\commit_log_user1.txt",
            "IsDefault": "True"
        },
        "bitbucket.com/USER_NAME_2": {
            "Password": "YOUR_PASS_2",
            "Dirs": [
                "D:\\Code\\OtherOrg\\BIApp"
            ],
            "FullName": "First Last",
            "Email": "user2@bitbucket.com",
            "CommitLogFilePath": "D:\\code\\commit_log_user2.txt"
        }
        "localhost/USER_NAME_3": {
            "Password": "YOUR_PASS_3",
            "Dirs": [
                "D:\\Code\\LocalProjects\\BIApp"
            ],
            "FullName": "First Last",
            "Email": "user3@corp.com",
            "CommitLogFilePath": "D:\\code\\commit_log_use3.txt"
        }
    }


**CLA Examples**
Commands,

    GitUtil info
    GitUtil push mod
    GitUtil pull
    GitUtil push message
    GitUtil push mod --amend

Example run with POSIX style arguments,

    GitUtil --repo-path D:\pwsh-scripts info
    GitUtil --config-file-path D:\Workspace\Config.json info

Other examples,

    dotnet run -- --repo-path D:\pwsh-scripts info

Please visit the design wiki for command line arguments (in reference section) to know more about the
arguments.

For help on CLA try,

    GitUtil help


### References
- CLA Design [wiki](https://github.com/atiq-cs/GitUtil/wiki/Command-Line-Arguments-Design)
