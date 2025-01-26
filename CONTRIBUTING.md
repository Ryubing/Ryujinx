# Contribution to Ryujinx

You can contribute to Ryujinx with PRs, testing of PRs and issues. Contributing code and other implementations is greatly appreciated alongside simply filing issues for problems you encounter.
Please read the entire document before continuing as it can potentially save everyone involved a significant amount of time.

# Quick Links

* [Code Style Documentation](docs/coding-guidelines/coding-style.md)
* [Pull Request Guidelines](docs/workflow/pr-guide.md)

## Reporting Issues

We always welcome bug reports, feature proposals and overall feedback. Here are a few tips on how you can make reporting your issue as effective as possible.

### Finding Existing Issues

Before filing a new issue, please search our [open issues](https://github.com/Ryubing/Ryujinx/issues) to check if it already exists.

If you do find an existing issue, please include your own feedback in the discussion. Do consider upvoting (👍 reaction) the original post, as this helps us prioritize popular issues in our backlog.

### Writing a Good Feature Request

Please review any feature requests already opened to both check it has not already been suggested, and to familiarize yourself with the format. When ready to submit a proposal, please use the [Feature Request issue template](https://github.com/Ryubing/Ryujinx/issues/new?assignees=&labels=&projects=&template=feature_request.yml&title=%5BFeature+Request%5D).

### Writing a Good Bug Report

Good bug reports make it easier for maintainers to verify and root cause the underlying problem. The better a bug report, the faster the problem will be resolved. 
Ideally, a bug report should contain the following information:

* A high-level description of the problem.
* A _minimal reproduction_, i.e. the smallest time commitment/configuration required to reproduce the wrong behavior. This can be in the form of a small homebrew application, or by providing a save file and reproduction steps for a specific game.
* A description of the _expected behavior_, contrasted with the _actual behavior_ observed.
* Information on the environment: OS/distro, CPU, GPU (including driver), RAM etc.
* A Ryujinx log file of the run instance where the issue occurred. Log files can be found in `[Executable Folder]/Logs` and are named chronologically.
* Additional information, e.g. is it a regression from previous versions? Are there any known workarounds?

When ready to submit a bug report, please use the [Bug Report issue template](https://github.com/Ryubing/Ryujinx/issues/new?assignees=&labels=bug&projects=&template=bug_report.yml&title=%5BBug%5D).

## Contributing Changes

Project maintainers will merge changes that both improve the project and meet our standards for code quality.

The [Pull Request Guide](docs/workflow/pr-guide.md) and [License](https://github.com/Ryubing/Ryujinx/blob/master/LICENSE.txt) docs define additional guidance.

### DOs and DON'Ts

Please do:

* **DO** follow our [coding style](docs/coding-guidelines/coding-style.md) (C# code-specific).
* **DO** give priority to the current style of the project or file you're changing even if it diverges from the general guidelines.
* **DO** keep the discussions focused. When a new or related topic comes up
  it's often better to create new issue than to side track the discussion.
* **DO** clearly state on an issue that you are going to take on implementing it.
* **DO** blog and tweet (or whatever) about your contributions, frequently!

Please do not:

* **DON'T** make PRs for style changes.
* **DON'T** surprise us with big pull requests. Instead, file an issue and talk with us on Discord to start
  a discussion so we can agree on a direction before you invest a large amount
  of time.
* **DON'T** commit code that you didn't write. If you find code that you think is a good fit to add to Ryujinx, file an issue or talk to us on Discord to start a discussion before proceeding.
* **DON'T** submit PRs that alter licensing related files or headers. If you believe there's a problem with them, file an issue and we'll be happy to discuss it.

### Suggested Workflow

We use and recommend the following workflow:

1. Create or find an issue for your work.
    - You can skip this step for trivial changes.
    - Get agreement from the team and the community that your proposed change is a good one if it is of significant size or changes core functionality.
    - Clearly state that you are going to take on implementing it, if that's the case. You can request that the issue be assigned to you. Note: The issue filer and the implementer don't have to be the same person.
2. Create a personal fork of the repository on GitHub (if you don't already have one).
3. In your fork, create a branch off of main (`git checkout -b mybranch`).
    - Branches are useful since they isolate your changes from incoming changes from upstream. They also enable you to create multiple PRs from the same fork.
4. Make and commit your changes to your branch.
    - [Build Instructions](https://github.com/Ryubing/Ryujinx/blob/master/COMPILING.md) explains how to build and test.
    - Commit messages should be clear statements of action and intent.
6. Build the repository with your changes.
    - Make sure that the builds are clean.
    - Make sure that `dotnet format` has been run and any corrections tested and committed.
7. Create a pull request (PR) against the Ryujinx/Ryujinx repository's **main** branch.
    - State in the description what issue or improvement your change is addressing.
    - Check if all the Continuous Integration checks are passing. Refer to [Actions](https://github.com/Ryubing/Ryujinx/actions) to check for outstanding errors.
8. Wait for feedback or approval of your changes from the core development team
    - Details about the pull request [review procedure](docs/workflow/pr-guide.md).
9. When the team members have signed off, and all checks are green, your PR will be merged.
    - The next official build will automatically include your change.
    - You can delete the branch you used for making the change.

### Good First Issues

The team marks the most straightforward issues as [good first issues](https://github.com/Ryubing/Ryujinx/issues?q=is%3Aopen+is%3Aissue+label%3A%22good+first+issue%22). This set of issues is the place to start if you are interested in contributing but new to the codebase.

### Commit Messages

Please format commit messages as follows (based on [A Note About Git Commit Messages](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html)):

```
Summarize change in 50 characters or less

Provide more detail after the first line. Leave one blank line below the
summary and wrap all lines at 72 characters or less.

If the change fixes an issue, leave another blank line after the final
paragraph and indicate which issue is fixed in the specific format
below.

Fix #42
```

Also do your best to factor commits appropriately, not too large with unrelated things in the same commit, and not too small with the same small change applied N times in N different commits.

### PR - CI Process

The [Ryujinx continuous integration](https://github.com/Ryubing/Ryujinx/actions) (CI) system will automatically perform the required builds and run tests (including the ones you are expected to run) for PRs. Builds and test runs must be clean or have bugs properly filed against flaky/unexpected failures that are unrelated to your change.

If the CI build fails for any reason, the PR actions tab should be consulted for further information on the failure. There are a few usual suspects for such a failure:
* `dotnet format` has not been run on the PR and has outstanding stylistic issues.
* There is an error within the PR that fails a test or errors the compiler.
* Random failure of the workflow can occasionally result in a CI failure. In this scenario a maintainer will manually restart the job.

### PR Feedback

Ryujinx team and community members will provide feedback on your change. Community feedback is highly valued. You may see the absence of team feedback if the community has already provided good review feedback.

Two Ryujinx team members must review and approve every PR prior to merge. They will often reply with "LGTM, see nit". That means that the PR will be merged once the feedback is resolved. "LGTM" == "looks good to me".

There are lots of thoughts and [approaches](https://github.com/antlr/antlr4-cpp/blob/master/CONTRIBUTING.md#emoji) for how to efficiently discuss changes. It is best to be clear and explicit with your feedback. Please be patient with people who might not understand the finer details about your approach to feedback.

#### Copying Changes from Other Projects

Ryujinx uses some implementations and frameworks from other projects. The following rules must be followed for PRs that include changes from another project:

- The license of the file is [permissive](https://en.wikipedia.org/wiki/Permissive_free_software_licence).
- The license of the file is left in-tact.
- The contribution is correctly attributed in the [3rd party notices](https://github.com/Ryubing/Ryujinx/blob/master/distribution/legal/THIRDPARTY.md) file in the repository, as needed.

