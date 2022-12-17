## Git Utility
Git Utility to handle multiple git accounts.
A utility to automate some of the commonly used Git Actions.

For example, to push changes to a git repository, this is a common sequence of commands,

    git add --update
    git commit --file commit_log.txt
    git push origin BRANCH_NAME

This utility does all of that when we run,

    GitUtility push mod

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
*CLA Design follows POSIX style arguments.*

Commands,

    GitUtility info
    GitUtility push mod
    GitUtility pull
    GitUtility push mod --amend

Above commands assume we are inside the git repo directory.

To run while being outside of the repo dir, we have,

    GitUtility --repo-path D:\CoolApp info

Or, to specify an input json config file path,

    GitUtility --config-file-path D:\Workspace\Config.json info

If it's not provided, default path is: `$Env:LOCALAPPDATA\GitUtilConfig.json`.

`push mod` only pushes modified files.

**Info / Status related arguments**

Show basic information about the repository,

    GitUtility --repo-path D:\CoolApp info

Show modified files that will be added to next `push mod` command,

    GitUtility --repo-path D:\CoolApp stat

Please visit the design wiki for command line arguments (in reference section) to know more about the
arguments.

To add a single file to commit and to push it to remote,

    GitUtility push single relative_file_path

*Argument `push single` also supports `--amend` to overwrite the previous commit.*

To add all files and to push,

    GitUtility push all


`push all` supports `--amend` which allows modifying/ammending the last commit and pushing to remote.

**Remote URL examples**
Set remote origin,

    GitUtility set-url https://github.com/repo/project.git

Set remote upstream (useful when you are only pulling changes from an upstream source, but pushing to your own repository),

    GitUtility set-url https://github.com/repo/project.git --upstream

#### Pull Examples
Pull from remote upstream (set upstream before running this using example in previous section),

    GitUtility pull --upstream

To pull from origin,

    GitUtility pull

For help on CLA try,

    GitUtility help


### References
- CLA Design [wiki](https://github.com/atiq-cs/GitUtil/wiki/Command-Line-Arguments-Design)
