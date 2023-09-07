using System;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.ClusterInformation;

internal class ClusterInformationViewModel
	: LegacyCustomAnalysisViewModelBase<ClusterInformationNode, GPM_ClusterInformation, GPM_ClusterInformationOptions>
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.ClusterInformation.ClusterInformationViewModel";

	public ClusterInformationViewModel(IAnalysisViewModelBaseServices services, Func<IViewBuilder> viewBuilderFactory)
		: base(services, viewBuilderFactory)
	{
	}
}
