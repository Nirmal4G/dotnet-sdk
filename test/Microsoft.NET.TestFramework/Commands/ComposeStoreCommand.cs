// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETCOREAPP

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Utils;
using Xunit.Abstractions;

namespace Microsoft.NET.TestFramework.Commands
{
    public sealed class ComposeStoreCommand : MSBuildCommand
    {
        private const string PublishSubfolderName = "packages";

        public ComposeStoreCommand(ITestOutputHelper log, string projectPath, string? relativePathToProject = null)
            : base(log, "ComposeStore", projectPath, relativePathToProject)
        {
        }

        public override DirectoryInfo GetOutputDirectory(string? targetFramework = "netcoreapp1.0", string? configuration = "Debug", string? runtimeIdentifier = "", string? platformIdentifier = "")
        {
            string output = Path.Combine(ProjectRootPath, "bin", BuildRelativeOutputPath(targetFramework, configuration, runtimeIdentifier, platformIdentifier));
            return new DirectoryInfo(output);
        }

        public string GetPublishedAppPath(string appName)
        {
            return Path.Combine(GetOutputDirectory().FullName, $"{appName}.dll");
        }

        private string BuildRelativeOutputPath(string? targetFramework, string? configuration, string? runtimeIdentifier, string? platformIdentifier)
        {
            if (runtimeIdentifier?.Length == 0)
            {
                runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
            }

            string arch = runtimeIdentifier?.Substring(runtimeIdentifier.LastIndexOf("-") + 1) ?? string.Empty;
            return Path.Combine(platformIdentifier ?? string.Empty, configuration ?? string.Empty, arch, targetFramework ?? string.Empty, PublishSubfolderName);
        }
    }
}

#endif
