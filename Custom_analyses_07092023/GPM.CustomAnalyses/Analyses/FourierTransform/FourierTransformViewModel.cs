using System;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.FourierTransform;

internal class FourierTransformViewModel
	: LegacyCustomAnalysisViewModelBase<FourierTransformNode, GPM_FourierTransform, GPM_FourierTransformOptions>
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.FourierTransform.FourierTransformViewModel";

	public FourierTransformViewModel(IAnalysisViewModelBaseServices services, Func<IViewBuilder> viewBuilderFactory)
		: base(services, viewBuilderFactory)
	{
	}
}
