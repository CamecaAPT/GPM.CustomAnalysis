using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.ClusterPosition;

[DefaultView(ClusterPositionViewModel.UniqueId, typeof(ClusterPositionViewModel))]
internal class ClusterPositionNode : LegacyCustomAnalysisNodeBase<GPM_ClusterPosition, GPM_ClusterPositionOptions>
{
	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("GPM Cluster Position");

	public const string UniqueId = "GPM.CustomAnalyses.Analyses.ClusterPosition.ClusterPositionNode";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;

	public ClusterPositionNode(IStandardAnalysisNodeBaseServices services, GPM_ClusterPosition analysis, IIonDisplayInfoProvider ionDisplayInfoProvider)
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
