using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.SaxeyDiagram;

[DefaultView(SaxeyDiagramViewModel.UniqueId, typeof(SaxeyDiagramViewModel))]
internal class SaxeyDiagramNode : LegacyCustomAnalysisNodeBase<GpmSaxeyDiagram, SaxeyDiagramOptions>
{
	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("GPM Saxey Diagram");

	public const string UniqueId = "GPM.CustomAnalyses.Analyses.SaxeyDiagram.SaxeyDiagramNode";

	public SaxeyDiagramNode(IStandardAnalysisNodeBaseServices services, GpmSaxeyDiagram analysis)
		: base(services, analysis)
	{
	}
}
