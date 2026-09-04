# Project Map — Phase 0

## Repository baseline
- `PulseInspector.sln`
- `PulseInspector/` — application
- `PulseInspector.Tests/` — tests
- `.github/workflows/` — CI workflows

## Application areas
### Models
Current repository includes model types such as:
- FeatureVector
- GroupData
- GroupDecisionPolicy
- GroupInspectionResult
- InspectionModel
- InspectionResult
- SubgroupInspectionResult
- WaveformData
- WaveformRecord

### Services
Current repository includes service areas for:
- FeatureExtractor
- SignalProcessor
- StatisticsService
- GroupInspectionService
- SubgroupInspectionService
- InspectionService
- CSV loading/export

### UI
- Forms: MainForm, SettingsForm, TrainingForm, AboutForm
- Controls: WaveformControl, FeatureGrid, FeatureDeviationGrid, HistogramControl, ScatterPlotControl, StatusIndicator

## Architectural observation
The repository already separates Models, Services and UI to a useful degree. The modernization should strengthen that boundary rather than move existing analysis code into WPF.

## Next analysis required
- call graph from each Form
- service dependency graph
- control-to-service dependencies
- event/state ownership
- persistence/configuration paths
- test coverage around protected behavior
