using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Prism.Events;
using Prism.Services.Dialogs;

namespace GPM.CustomAnalyses.Analyses.FourierTransform;

internal class FourierTransformMenuFactory : LegacyAnalysisMenuFactoryBase
{
	protected override INodeDisplayInfo DisplayInfo { get; } = FourierTransformNode.DisplayInfo;
	protected override string NodeUniqueId { get; } = FourierTransformNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;

	public FourierTransformMenuFactory(IEventAggregator eventAggregator, IDialogService dialogService)
		: base(eventAggregator, dialogService)
	{
	}
}
