using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace GPM.CustomAnalyses.Analyses.GibbsM;

internal class GibbsMMenuFactory : AnalysisMenuFactoryBase
{
	public GibbsMMenuFactory(IEventAggregator eventAggregator) : base(eventAggregator)
	{
	}

	protected override INodeDisplayInfo DisplayInfo => GibbsMNode.DisplayInfo;
	protected override string NodeUniqueId => GibbsMNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
