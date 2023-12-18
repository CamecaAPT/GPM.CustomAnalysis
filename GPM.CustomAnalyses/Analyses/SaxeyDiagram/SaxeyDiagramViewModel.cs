using System;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.SaxeyDiagram;

internal class SaxeyDiagramViewModel
	: LegacyCustomAnalysisViewModelBase<SaxeyDiagramNode, GpmSaxeyDiagram, SaxeyDiagramOptions>
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.SaxeyDiagram.SaxeyDiagramViewModel";

	public SaxeyDiagramViewModel(IAnalysisViewModelBaseServices services, Func<IViewBuilder> viewBuilderFactory) : base(services, viewBuilderFactory)
	{
	}
}
