using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Events;

namespace GPM.CustomAnalyses.Analyses.Reconstruction3DM;

internal class Reconstruction3DMMenuFactory : AnalysisMenuFactoryBase
{
	public Reconstruction3DMMenuFactory(IEventAggregator eventAggregator) : base(eventAggregator)
	{
	}

	protected override INodeDisplayInfo DisplayInfo => Reconstruction3DMNode.DisplayInfo;
	protected override string NodeUniqueId => Reconstruction3DMNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;
}
