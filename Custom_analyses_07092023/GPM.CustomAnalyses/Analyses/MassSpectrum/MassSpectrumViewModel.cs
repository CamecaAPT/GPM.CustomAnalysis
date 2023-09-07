using System;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.MassSpectrum;

internal class MassSpectrumViewModel
	: LegacyCustomAnalysisViewModelBase<MassSpectrumNode, GPM_MassSpectrum, GPM_MassSpectrumOptions>
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.MassSpectrum.MassSpectrumViewModel";

	public MassSpectrumViewModel(IAnalysisViewModelBaseServices services, Func<IViewBuilder> viewBuilderFactory)
		: base(services, viewBuilderFactory)
	{
	}
}
