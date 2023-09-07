using System;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.FrequencyDistribution;

internal class FrequencyDistributionViewModel
	: LegacyCustomAnalysisViewModelBase<FrequencyDistributionNode, GPM_FrequencyDistribution, GPM_FrequencyDistributionOptions>
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.FrequencyDistribution.FrequencyDistributionViewModel";

	public FrequencyDistributionViewModel(IAnalysisViewModelBaseServices services, Func<IViewBuilder> viewBuilderFactory) : base(services, viewBuilderFactory)
	{
	}
}
