using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Prism.Events;
using Prism.Services.Dialogs;

namespace GPM.CustomAnalyses.Analyses.MassSpectrum;

internal class MassSpectrumMenuFactory : LegacyAnalysisMenuFactoryBase
{
	protected override INodeDisplayInfo DisplayInfo { get; } = MassSpectrumNode.DisplayInfo;
	protected override string NodeUniqueId { get; } = MassSpectrumNode.UniqueId;
	public override AnalysisMenuLocation Location { get; } = AnalysisMenuLocation.Analysis;

	public MassSpectrumMenuFactory(IEventAggregator eventAggregator, IDialogService dialogService)
		: base(eventAggregator, dialogService)
	{
	}
}
