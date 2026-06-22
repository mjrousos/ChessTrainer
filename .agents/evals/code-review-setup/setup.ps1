#!/usr/bin/env pwsh
#
# Workspace setup helper for .agents/evals/code-review.eval.yaml.
#
# The eval YAML used to embed each of these steps as inline
# `pwsh -NoProfile -Command "..."` commands, but on the Linux GitHub Actions
# runner Vally executes each command via `/bin/sh -c`, and the inline pwsh
# strings (especially the one with embedded `\"` backtick-escaped quotes)
# tripped sh's quoting/command-substitution rules before pwsh was ever
# invoked. Routing through `pwsh -NoProfile -File <path>` keeps the shell
# command argument-free so /bin/sh has nothing to mis-parse, while all the
# PowerShell-flavoured quoting stays inside this script.
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Clean', 'AddBanner', 'EditAbout')]
    [string] $Step
)

$ErrorActionPreference = 'Stop'

switch ($Step) {
    'Clean' {
        # Scrub build artifacts and node_modules carried in from the local
        # checkout so trials are deterministic and the workdir stays small.
        Get-ChildItem -Path . -Include bin, obj, node_modules `
            -Directory -Recurse -Force -ErrorAction SilentlyContinue |
            Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    }
    'AddBanner' {
        # Create the one new file committed on `feature/example-change` so
        # `pre-pr-diff-against-main` (and any other stimulus reading
        # `git diff main`) has a small, deterministic, plain-ASCII diff to
        # review.
        $bannerPath = 'src/ChessTrainerApp/app/example-banner.ts'
        $bannerContents = @'
export const exampleBanner = "Welcome to the puzzle solver!";
'@
        New-Item -Path $bannerPath -ItemType File -Force -Value $bannerContents | Out-Null
    }
    'EditAbout' {
        # Leave one file with unstaged modifications so `git status` reports
        # changes for `unstaged-changes-review`. We append a benign Razor
        # comment so `git diff` shows real content and the file itself
        # remains functional.
        Add-Content -Path 'src/ChessTrainerApp/Pages/About.razor' `
            -Value '@* TODO: extend with author bio and contact info *@'
    }
}
