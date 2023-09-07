using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace GPM.CustomAnalyses.Analyses.DataFilteringM;

internal class DataFilteringMMenuFactory : AnalysisMenuFactoryBase
{
	public DataFilteringMMenuFactory(IEventAggregator eventAggregator) : base(eventAggregator)
	{
	}

	protected override INodeDisplayInfo DisplayInfo => DataFilteringMNode.DisplayInfo;
	protected override string NodeUniqueId => DataFilteringMNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
