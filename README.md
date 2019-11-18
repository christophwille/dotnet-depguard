# dotnet-depguard

Inspired by https://github.com/OpenPeeDeeP/depguard with support for blacklisted dependencies only.

## Usage

Put a .depguard.json file in your project directory (see test-projects for an example). List the NuGet packages that are 
disallowed from use in your projects and go. 

Exit code 0 means you are golden, 1 means you have a blacklisted dependency (and it is printed), -1 is an input error.

## Installation

Download and install the [.NET Core 2.1 SDK](https://www.microsoft.com/net/download) or newer. Once installed, run the following command:

```bash
dotnet tool install --global depguard
```

If you already have a previous version of **depguard** installed, you can upgrade to the latest version using the following command:

```bash
dotnet tool update --global depguard
```

## Kudos

Built on https://www.nuget.org/packages/DotNetOutdated.Core with also quite a bit of code from the 
main project https://github.com/jerriep/dotnet-outdated (project started with issue https://github.com/jerriep/dotnet-outdated/issues/223
in dotnet-outdated repository). Thanks to the hard work from Jerrie, building my minimal product was super-easy.
