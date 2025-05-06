using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace GPM.CustomAnalyses.Analyses.ClusteringM;

internal class ClusteringMMenuFactory : AnalysisMenuFactoryBase
{
	public ClusteringMMenuFactory(IEventAggregator eventAggregator) : base(eventAggregator)
	{
	}

	protected override INodeDisplayInfo DisplayInfo => ClusteringMNode.DisplayInfo;
	protected override string NodeUniqueId => ClusteringMNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
