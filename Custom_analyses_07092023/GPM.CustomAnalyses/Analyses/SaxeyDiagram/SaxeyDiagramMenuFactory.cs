using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Prism.Events;
using Prism.Services.Dialogs;

namespace GPM.CustomAnalyses.Analyses.SaxeyDiagram;

internal class SaxeyDiagramMenuFactory : LegacyAnalysisMenuFactoryBase
{
	protected override INodeDisplayInfo DisplayInfo { get; } = SaxeyDiagramNode.DisplayInfo;
	protected override string NodeUniqueId { get; } = SaxeyDiagramNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;

	public SaxeyDiagramMenuFactory(IEventAggregator eventAggregator, IDialogService dialogService)
		: base(eventAggregator, dialogService)
	{
	}
}
