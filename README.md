# dotnet-depguard

Inspired by https://github.com/OpenPeeDeeP/depguard with support for blacklisted dependencies only.

## Usage

Put a .depguard.json file in your project directory (see test-projects for an example). List the NuGet packages that are 
disallowed from use in your projects and go. 

Exit code 0 means you are golden, 1 means you have a blacklisted dependency (and it is printed), -1 is an input error.

## Kudos

Built on https://www.nuget.org/packages/DotNetOutdated.Core with also quite a bit of code from the 
main project https://github.com/jerriep/dotnet-outdated (project started with issue https://github.com/jerriep/dotnet-outdated/issues/223
in dotnet-outdated repository). Thanks to the hard work from Jerrie, building my minimal product was super-easy.
