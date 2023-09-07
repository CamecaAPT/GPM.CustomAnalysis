using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Prism.Events;
using Prism.Services.Dialogs;

namespace GPM.CustomAnalyses.Analyses.ClusterInformation;

internal class ClusterInformationMenuFactory : LegacyAnalysisMenuFactoryBase
{
	protected override INodeDisplayInfo DisplayInfo { get; } = ClusterInformationNode.DisplayInfo;
	protected override string NodeUniqueId { get; } = ClusterInformationNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;

	public ClusterInformationMenuFactory(IEventAggregator eventAggregator, IDialogService dialogService)
		: base(eventAggregator, dialogService)
	{
	}
}
