using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.FourierTransform;

[DefaultView(FourierTransformViewModel.UniqueId, typeof(FourierTransformViewModel))]
internal class FourierTransformNode : LegacyCustomAnalysisNodeBase<GPM_FourierTransform, GPM_FourierTransformOptions>
{
	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("GPM FourierTransform");

	public const string UniqueId = "GPM.CustomAnalyses.Analyses.FourierTransform.FourierTransformNode";
	
	public FourierTransformNode(IStandardAnalysisNodeBaseServices services, GPM_FourierTransform analysis)
		: base(services, analysis)
	{
	}
}
