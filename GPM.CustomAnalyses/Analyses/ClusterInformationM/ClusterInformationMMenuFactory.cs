using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace GPM.CustomAnalyses.Analyses.ClusterInformationM;

internal class ClusterInformationMMenuFactory : AnalysisMenuFactoryBase
{
	public ClusterInformationMMenuFactory(IEventAggregator eventAggregator) : base(eventAggregator)
	{
	}

	protected override INodeDisplayInfo DisplayInfo => ClusterInformationMNode.DisplayInfo;
	protected override string NodeUniqueId => ClusterInformationMNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
