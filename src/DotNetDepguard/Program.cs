using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DotNetDepguard.Models;
using DotNetOutdated.Core.Models;
using DotNetOutdated.Core.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

//
// A lot verbatim or simplified from https://github.com/jerriep/dotnet-outdated/blob/master/src/DotNetOutdated/Program.cs
//
namespace DotNetDepguard
{
	[VersionOptionFromMember(MemberName = nameof(GetVersion))]
	class Program
	{
		private readonly IFileSystem _fileSystem;
		private readonly IProjectAnalysisService _projectAnalysisService;
		private readonly IProjectDiscoveryService _projectDiscoveryService;

		public static int Main(string[] args)
		{
			using (var services = new ServiceCollection()
				.AddSingleton<IConsole, PhysicalConsole>()
				.AddSingleton<IFileSystem, FileSystem>()
				.AddSingleton<IProjectDiscoveryService, ProjectDiscoveryService>()
				.AddSingleton<IProjectAnalysisService, ProjectAnalysisService>()
				.AddSingleton<IDotNetRunner, DotNetRunner>()
				.AddSingleton<IDependencyGraphService, DependencyGraphService>()
				.AddSingleton<IDotNetRestoreService, DotNetRestoreService>()
				.AddSingleton<INuGetPackageInfoService, NuGetPackageInfoService>()
				.AddSingleton<INuGetPackageResolutionService, NuGetPackageResolutionService>()
				.BuildServiceProvider())
			{
				var app = new CommandLineApplication<Program>
				{
					ThrowOnUnexpectedArgument = false
				};
				app.Conventions
					.UseDefaultConventions()
					.UseConstructorInjection(services);

				return app.Execute(args);
			}
		}

		public static string GetVersion() => typeof(Program)
			.Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			.InformationalVersion;

		public Program(IFileSystem fileSystem, IProjectAnalysisService projectAnalysisService, IProjectDiscoveryService projectDiscoveryService)
		{
			_fileSystem = fileSystem;
			_projectAnalysisService = projectAnalysisService;
			_projectDiscoveryService = projectDiscoveryService;
		}

		private Dictionary<string, string> _blacklistedDependencies = new Dictionary<string, string>();
		public async Task<int> OnExecute(CommandLineApplication app, IConsole console)
		{
			string path = _fileSystem.Directory.GetCurrentDirectory();
			string configFilePath = Path.Combine(path, ".depguard.json");
			if (!File.Exists(configFilePath))
			{
				console.WriteLine("Configuration file .depguard.json does not exist");
				return -1;
			}

			string configText = await File.ReadAllTextAsync(configFilePath);
			Config config = JsonConvert.DeserializeObject<Config>(configText);

			if (!config.Packages.Any())
			{
				console.WriteLine("No blacklisted packages configured, nothing to do");
				return 0;
			}

			_blacklistedDependencies = config.Packages.ToDictionary(k => k.ToLowerInvariant());

			string projectPath = _projectDiscoveryService.DiscoverProject(path);
			var projects = _projectAnalysisService.AnalyzeProject(projectPath, true, 1);

			var matchedProjects = AnalyzeDependencies(projects, console);
			if (matchedProjects.Any())
			{
				ReportDependencies(matchedProjects, console);
				return 1;
			}
			else
			{
				console.WriteLine("No blacklisted dependencies were detected");
			}

			return 0;
		}

		private List<AnalyzedProject> AnalyzeDependencies(List<Project> projects, IConsole console)
		{
			var matchedProjects = new List<AnalyzedProject>();

			foreach (var project in projects)
			{
				var targetFrameworks = new List<AnalyzedTargetFramework>();

				foreach (var targetFramework in project.TargetFrameworks)
				{
					var matchingDependencies = new List<AnalyzedDependency>();

					var deps = targetFramework.Dependencies
						.Where(d => d.IsAutoReferenced == false);

					var dependencies = deps.OrderBy(dependency => dependency.IsTransitive)
						.ThenBy(dependency => dependency.Name)
						.ToList();

					for (var index = 0; index < dependencies.Count; index++)
					{
						var dependency = dependencies[index];

						if (_blacklistedDependencies.ContainsKey(dependency.Name.ToLowerInvariant()))
						{
							matchingDependencies.Add(new AnalyzedDependency(dependency));
						}
					}

					if (matchingDependencies.Count > 0)
						targetFrameworks.Add(new AnalyzedTargetFramework(targetFramework.Name, matchingDependencies));
				}

				if (targetFrameworks.Count > 0)
					matchedProjects.Add(new AnalyzedProject(project.Name, project.FilePath, targetFrameworks));
			}

			return matchedProjects;
		}

		private void ReportDependencies(List<AnalyzedProject> projects, IConsole console)
		{
			foreach (var project in projects)
			{
				console.WriteLine($"» {project.Name}");

				// Process each target framework with its related dependencies
				foreach (var targetFramework in project.TargetFrameworks)
				{
					console.WriteLine($"[{targetFramework.Name}]");

					var dependencies = targetFramework.Dependencies
						.OrderBy(d => d.Name)
						.ToList();

					foreach (var dependency in dependencies)
					{
						console.Write(dependency.Description);
						console.WriteLine();
					}
				}

				console.WriteLine();
			}
		}
	}
}