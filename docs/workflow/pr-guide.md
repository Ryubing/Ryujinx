# Pull Request Guide

## Contributing Rules

All contributions to GreemDev/Ryujinx repository are made via pull requests (PRs) rather than through direct commits. The pull requests are reviewed and merged by the maintainers after a review and at least two approvals from the core development team.

To merge pull requests, you must have write permissions in the repository.

## Quick Code Review Rules

* Do not mix unrelated changes in one pull request. For example, a code style change should never be mixed with a bug fix.
* All changes should follow the existing code style. You can read more about our code style at [docs/coding-style](../coding-guidelines/coding-style.md).
* Adding external dependencies is to be avoided unless not doing so would introduce _significant_ complexity. Any dependency addition should be justified and discussed before merge.
* Use Draft pull requests for changes you are still working on but want early CI loop feedback. When you think your changes are ready for review, [change the status](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/changing-the-stage-of-a-pull-request) of your pull request.
* Rebase your changes when required or directly requested. Changes should always be commited on top of the upstream branch, not the other way around.
* If you are asked to make changes during the review process do them as a new commit.
* Only resolve GitHub conversations with reviewers once they have been addressed with a commit, or via a mutual agreement.

## Pull Request Ownership

Every pull request will automatically have labels and reviewers assigned. The label not only indicates the code segment which the change touches but also the area reviewers to be assigned.

If during the code review process a merge conflict occurs, the PR author is responsible for its resolution. Help will be provided if necessary although GitHub makes this easier by allowing simple conflict resolution using the [conflict-editor](https://help.github.com/en/github/collaborating-with-issues-and-pull-requests/resolving-a-merge-conflict-on-github).

## Pull Request Builds

When submitting a PR to the `Ryubing/Ryujinx` repository, various builds will run validating many areas to ensure we keep developer productivity and product quality high. These various workflows can be tracked in the [Actions](https://github.com/Ryubing/Ryujinx/actions) tab of the repository. If the job continues to completion, the build artifacts will be uploaded and posted as a comment in the PR discussion.

## Review Turnaround Times

Ryujinx is a project that is maintained by volunteers on a completely free-time basis. As such we cannot guarantee any particular timeframe for pull request review and approval. Weeks to months are common for larger (>500 line) PRs but there are some additional best practises to avoid review purgatory.

* Make the reviewers life easier wherever possible. Make use of descriptive commit names, code comments and XML docs where applicable.
* If there is disagreement on feedback then always lean on the side of the development team and community over any personal opinion.
* We're human. We miss things. We forget things. If there has been radio silence on your changes for a substantial period of time then do not hesitate to reach out directly either with something simple like "bump" on GitHub or a directly on Discord.

To re-iterate, make the review as easy for us as possible, respond promptly and be comfortable to interact directly with us for anything else.

## Merging Pull Requests

Anyone with write access can merge a pull request manually when the following conditions have been met:

* The PR has been approved by two reviewers and any other objections are addressed.
    * You can request follow up reviews from the original reviewers if they requested changes.
* The PR successfully builds and passes all tests in the Continuous Integration (CI) system. In case of failures, refer to the [Actions](https://github.com/Ryubing/Ryujinx/actions) tab of your PR.

Typically, PRs are merged as one commit (squash merges). It creates a simpler history than a Merge Commit. "Special circumstances" are rare, and typically mean that there are a series of cleanly separated changes that will be too hard to understand if squashed together, or for some reason we want to preserve the ability to dissect them.

## Blocking Pull Request Merging

If for whatever reason you would like to move your pull request back to an in-progress status to avoid merging it in the current form, you can turn the PR into a draft PR by selecting the option under the reviewers section. Alternatively, you can do that by adding [WIP] prefix to the pull request title.

## Old Pull Request Policy

From time to time we will review older PRs and check them for relevance. If we find the PR is inactive or no longer applies, we will close it. As the PR owner, you can simply reopen it if you feel your closed PR needs our attention.

