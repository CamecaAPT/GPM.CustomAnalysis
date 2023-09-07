using System;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.ClusterPosition;

internal class ClusterPositionViewModel
	: LegacyCustomAnalysisViewModelBase<ClusterPositionNode, GPM_ClusterPosition, GPM_ClusterPositionOptions>
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.ClusterPosition.ClusterPositionViewModel";

	public ClusterPositionViewModel(IAnalysisViewModelBaseServices services, Func<IViewBuilder> viewBuilderFactory)
		: base(services, viewBuilderFactory)
	{
	}
}
