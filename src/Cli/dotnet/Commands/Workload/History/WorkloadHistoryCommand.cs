// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using Microsoft.DotNet.Cli.NuGetPackageDownloader;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.NET.Sdk.WorkloadManifestReader;
using Microsoft.Deployment.DotNet.Releases;
using Microsoft.DotNet.Cli.Commands.Workload.Install;
using LocalizableStrings = Microsoft.DotNet.Workloads.Workload.History.LocalizableStrings;

namespace Microsoft.DotNet.Cli.Commands.Workload.History;

internal class WorkloadHistoryCommand : WorkloadCommandBase
{
    private readonly IInstaller _workloadInstaller;
    private readonly IWorkloadResolver _workloadResolver;
    private readonly ReleaseVersion _sdkVersion;
    private readonly SdkFeatureBand _sdkFeatureBand;

    public WorkloadHistoryCommand(
        ParseResult parseResult,
        IReporter reporter = null,
        IInstaller workloadInstaller = null,
        INuGetPackageDownloader nugetPackageDownloader = null
    ) : base(parseResult, CommonOptions.HiddenVerbosityOption, reporter, null, nugetPackageDownloader)
    {
        var creationResult = new WorkloadResolverFactory().Create();

        var userProfileDir = creationResult.UserProfileDir;
        _sdkVersion = creationResult.SdkVersion;
        _workloadResolver = creationResult.WorkloadResolver;
        _sdkFeatureBand = new SdkFeatureBand(_sdkVersion);

        _workloadInstaller = workloadInstaller ??
                             WorkloadInstallerFactory.GetWorkloadInstaller(Reporter, _sdkFeatureBand,
                                 _workloadResolver, Verbosity, userProfileDir, VerifySignatures, PackageDownloader, creationResult.DotnetPath, TempDirectoryPath,
                                 packageSourceLocation: null, _parseResult.ToRestoreActionConfig());
    }

    public override int Execute()
    {
        var displayRecords = WorkloadHistoryDisplay.ProcessWorkloadHistoryRecords(_workloadInstaller.GetWorkloadHistoryRecords(_sdkFeatureBand.ToString()), out bool unknownRecordsPresent);

        if (displayRecords.Count == 0)
        {
            Reporter.WriteLine(LocalizableStrings.NoHistoryFound);
        }
        else
        {
            var table = new PrintableTable<WorkloadHistoryDisplay.DisplayRecord>();
            table.AddColumn(LocalizableStrings.Id, r => r.ID?.ToString() ?? "");
            table.AddColumn(LocalizableStrings.Date, r => r.TimeStarted?.ToString() ?? "");
            table.AddColumn(LocalizableStrings.Command, r => r.Command);
            table.AddColumn(LocalizableStrings.Workloads, r => string.Join(", ", r.HistoryState.InstalledWorkloads ?? []));
            table.AddColumn(LocalizableStrings.GlobalJsonVersion, r => r.GlobalJsonVersion ?? string.Empty);
            table.AddColumn(LocalizableStrings.WorkloadSetVersion, r => r.HistoryState.WorkloadSetVersion ?? string.Empty);

            Reporter.WriteLine();
            table.PrintRows(displayRecords, l => Reporter.WriteLine(l));
            Reporter.WriteLine();

            if (unknownRecordsPresent)
            {
                Reporter.WriteLine(LocalizableStrings.UnknownRecordsInformationalMessage);
                Reporter.WriteLine();
            }
        }

        return 0;
    }

}
