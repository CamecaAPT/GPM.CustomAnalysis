using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace GPM.CustomAnalyses.Analyses.ClusterPositionM;

internal class ClusterPositionMMenuFactory : AnalysisMenuFactoryBase
{
	public ClusterPositionMMenuFactory(IEventAggregator eventAggregator) : base(eventAggregator)
	{
	}

	protected override INodeDisplayInfo DisplayInfo => ClusterPositionMNode.DisplayInfo;
	protected override string NodeUniqueId => ClusterPositionMNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
