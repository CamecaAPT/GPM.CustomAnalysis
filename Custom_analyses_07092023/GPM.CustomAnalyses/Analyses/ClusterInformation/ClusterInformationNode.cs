using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.ClusterInformation;

[DefaultView(ClusterInformationViewModel.UniqueId, typeof(ClusterInformationViewModel))]
internal class ClusterInformationNode : LegacyCustomAnalysisNodeBase<GPM_ClusterInformation, GPM_ClusterInformationOptions>
{

	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("GPM Cluster Information");

	public const string UniqueId = "GPM.CustomAnalyses.Analyses.ClusterInformation.ClusterInformationNode";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;

	public ClusterInformationNode(IStandardAnalysisNodeBaseServices services, GPM_ClusterInformation analysis, IIonDisplayInfoProvider ionDisplayInfoProvider)
		: base(services, analysis)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
	}
	protected override void OnCreated(NodeCreatedEventArgs eventArgs)
	{
		base.OnCreated(eventArgs);
		// When the node is instantiated, check if we can resolve an ion display info service. If so, register with the analysis.
		// This service will allow retrieval of ion colors that can be configured at the analysis set level, with fallback to the current color theme.
		if (_ionDisplayInfoProvider.Resolve(InstanceId) is { } ionDisplayInfo)
		{
			Analysis.IonDisplayInfo = ionDisplayInfo;
		}
	}
}
