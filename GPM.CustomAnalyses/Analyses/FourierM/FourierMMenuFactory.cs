using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace GPM.CustomAnalyses.Analyses.FourierM;

internal class FourierMMenuFactory : AnalysisMenuFactoryBase
{
	public FourierMMenuFactory(IEventAggregator eventAggregator) : base(eventAggregator)
	{
	}

	protected override INodeDisplayInfo DisplayInfo => FourierMNode.DisplayInfo;
	protected override string NodeUniqueId => FourierMNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
