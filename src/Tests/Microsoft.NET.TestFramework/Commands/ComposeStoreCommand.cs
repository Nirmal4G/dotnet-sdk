// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NETCOREAPP

using System.IO;
using System.Runtime.InteropServices;
using Xunit.Abstractions;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.NET.TestFramework.Utilities;

namespace Microsoft.NET.TestFramework.Commands
{
    public sealed class ComposeStoreCommand : MSBuildCommand
    {
        private const string PublishSubfolderName = "packages";

        public ComposeStoreCommand(ITestOutputHelper log, string projectPath, string relativePathToProject = null)
            : base(log, "ComposeStore", projectPath, relativePathToProject)
        {
        }

        public override DirectoryInfo GetOutputDirectory(string targetFramework = "netcoreapp1.0", string configuration = "Debug", string runtimeIdentifier = "")
        {
            if (runtimeIdentifier.Length == 0)
            {
                runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
            }
            string arch = runtimeIdentifier.Substring(runtimeIdentifier.LastIndexOf("-") + 1);
            string relativeToBaseOutputPath = Path.Combine(configuration, arch, targetFramework, runtimeIdentifier, PublishSubfolderName);
            return base.GetBaseOutputDirectory().Sub(relativeToBaseOutputPath);

            // TODO: SDK-Style projects must not contain arch in the platform. Replace the above logic with the following once it's fixed.
            // DirectoryInfo baseDirectory = base.GetBaseOutputDirectory(targetFramework, configuration, runtimeIdentifier);
            // return baseDirectory.Sub(PublishSubfolderName);
        }

        public string GetPublishedAppPath(string appName)
        {
            return Path.Combine(GetOutputDirectory().FullName, $"{appName}.dll");
        }
    }
}

#endif
