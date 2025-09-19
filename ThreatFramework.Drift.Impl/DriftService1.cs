using ThreatFramework.Core.CoreEntities;
using ThreatFramework.Drift.Contract;
using ThreatFramework.Drift.Contract.Model;
using ThreatFramework.Infra.Contract;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.CompilerServices;

namespace ThreatFramework.Drift.Impl
{
    public class DriftService1 : IDriftService
    {
        private readonly IYamlDriftBuilder _yamlDriftBuilder;
        private readonly ILibraryDriftAggregator _libraryDriftAggregator;

        public DriftService1(IYamlDriftBuilder yamlDriftBuilder, ILibraryDriftAggregator libraryDriftAggregator)
        {
            _yamlDriftBuilder = yamlDriftBuilder;
            _libraryDriftAggregator = libraryDriftAggregator;
        }

        public async Task<DriftAnalyzeResponse> AnalyzeAsync(DriftAnalyzeRequest request, CancellationToken ct = default)
        {
            var yamlReport = new YamlFilesDriftReport
            {
                BaseLineFolderPath = request.BaselineFolderPath,
                TargetFolderPath = request.TargetFolderPath,
                AddedFiles = (List<string>)(request.DriftSummaryResponse?.AddedFiles ?? new List<string>()),
                RemovedFiles = (List<string>)(request.DriftSummaryResponse?.RemovedFiles ?? new List<string>()),
                ModifiedFiles = (List<string>)(request.DriftSummaryResponse?.ModifiedFiles ?? new List<string>()),
            };

            return await AnalyzeYamlFilesAsync(yamlReport, ct);
        }

        public async Task<DriftAnalyzeResponse> AnalyzeYamlFilesAsync(YamlFilesDriftReport request, CancellationToken ct = default)
        {
            var report = await _yamlDriftBuilder.BuildAsync(request, ct);
            var librarywiseDrif = await _libraryDriftAggregator.AggregateAsync(report, cancellationToken: ct);
            return new DriftAnalyzeResponse
            {
                Libraries = librarywiseDrif.ToList(),
                Global = new GlobalDrift
                {
                }
            };
        }

    }

}
