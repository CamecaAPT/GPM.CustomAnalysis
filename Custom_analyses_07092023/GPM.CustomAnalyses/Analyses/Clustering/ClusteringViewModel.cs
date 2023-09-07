using System;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.Clustering;

internal class ClusteringViewModel
	: LegacyCustomAnalysisViewModelBase<ClusteringNode, GPM_Clustering, GPM_ClusteringOptions>
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.Clustering.ClusteringViewModel";

	public ClusteringViewModel(IAnalysisViewModelBaseServices services, Func<IViewBuilder> viewBuilderFactory)
		: base(services, viewBuilderFactory)
	{
	}
}
