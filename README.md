## Git Utility
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
        "default": {
            "UserName": "USER_NAME_1",
            "GithubToken": "ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
            "Dirs": [],
            "FullName": "First Last",
            "Email": "user@corp.com",
            "CommitLogFilePath": "D:\\code\\commit_log_user1.txt"
        },
        "specified": {
            "UserName": "USER_NAME_2",
            "GithubToken": "ghp_yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy",
            "Dirs": [
                "D:\\Code\\OtherOrg\\BIApp"
            ],
            "FullName": "First Last",
            "Email": "user2@corp.com",
            "CommitLogFilePath": "D:\\code\\commit_log_user2.txt"
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
