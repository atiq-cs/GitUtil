## Git Utility
A utility to automate some of the commonly used Git Actions.

For example, to do push some changes to a git repository, this is a common pattern of commands,

    git add --update
    git commit --file commit_log.txt
    git push origin BRANCH_NAME

This utility does all of that when we run,

    GitUtil push mod

**CLA Examples**

    GitUtil info
    GitUtil push mod
    GitUtil pull
    GitUtil push message
    GitUtil push mod --amend


### References
- CLA Design [wiki](https://github.com/atiq-cs/GitUtil/wiki/Command-Line-Arguments-Design)
