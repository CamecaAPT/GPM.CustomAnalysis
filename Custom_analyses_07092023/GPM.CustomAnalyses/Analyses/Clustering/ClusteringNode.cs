using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;

namespace GPM.CustomAnalyses.Analyses.Clustering;

[DefaultView(ClusteringViewModel.UniqueId, typeof(ClusteringViewModel))]
internal class ClusteringNode : LegacyCustomAnalysisNodeBase<GPM_Clustering, GPM_ClusteringOptions>
{
	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("GPM Clustering");

	public const string UniqueId = "GPM.CustomAnalyses.Analyses.Clustering.ClusteringNode";
	
	public ClusteringNode(IStandardAnalysisNodeBaseServices services, GPM_Clustering analysis)
		: base(services, analysis)
	{
	}
}
