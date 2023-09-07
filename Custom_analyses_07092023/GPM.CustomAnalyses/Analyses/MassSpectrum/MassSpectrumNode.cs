using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.MassSpectrum;

[DefaultView(MassSpectrumViewModel.UniqueId, typeof(MassSpectrumViewModel))]
internal class MassSpectrumNode : LegacyCustomAnalysisNodeBase<GPM_MassSpectrum, GPM_MassSpectrumOptions>
{
	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("GPM MassSpectrum");

	public const string UniqueId = "GPM.CustomAnalyses.Analyses.MassSpectrum.MassSpectrumNode";
	
	public MassSpectrumNode(IStandardAnalysisNodeBaseServices services, GPM_MassSpectrum analysis)
		: base(services, analysis)
	{
	}
}
