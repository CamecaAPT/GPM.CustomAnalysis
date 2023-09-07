using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace GPM.CustomAnalyses.Analyses.LoadMemoryM;

internal class LoadMemoryMMenuFactory : AnalysisMenuFactoryBase
{
	public LoadMemoryMMenuFactory(IEventAggregator eventAggregator) : base(eventAggregator)
	{
	}

	protected override INodeDisplayInfo DisplayInfo => LoadMemoryMNode.DisplayInfo;
	protected override string NodeUniqueId => LoadMemoryMNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
