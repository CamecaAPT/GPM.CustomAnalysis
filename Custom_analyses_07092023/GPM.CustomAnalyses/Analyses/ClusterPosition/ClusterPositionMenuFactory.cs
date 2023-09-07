using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Prism.Events;
using Prism.Services.Dialogs;

namespace GPM.CustomAnalyses.Analyses.ClusterPosition;

internal class ClusterPositionMenuFactory : LegacyAnalysisMenuFactoryBase
{
	protected override INodeDisplayInfo DisplayInfo { get; } = ClusterPositionNode.DisplayInfo;
	protected override string NodeUniqueId { get; } = ClusterPositionNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;

	public ClusterPositionMenuFactory(IEventAggregator eventAggregator, IDialogService dialogService)
		: base(eventAggregator, dialogService)
	{
	}
}
