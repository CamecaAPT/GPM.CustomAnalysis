using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using AnalysisFilterTest.Base;
using AnalysisFilterTest.SmallExamples;
using System.Linq;

namespace GPM.CustomAnalyses.Analyses.DataFilteringM;

// AnalysisNodeBase and variants (Standard/Legacy) use DefaultViewAttribute to easily link views with the main analysis.
// Base node will create these views when analysis is first created or the node is double-clicked if the view was closed.
// DefaultViewAttribute supports multiple instances, so a single analysis could easily control more than one view.
[DefaultView(DataFilteringMViewModel.UniqueId, typeof(DataFilteringMViewModel))]
[NodeType(NodeType.DataFilter)]

internal class DataFilteringMNode : NodeBase<DataFilterExampleProperties, DataFilterExampleSaveState>
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.DataFilteringM.DataFilteringMNode";

	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("DataFilteringM");

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly INodeDataFilterProvider _nodeDataFilterProvider;
	private readonly IIonDataProvider _ionDataProvider;

	public DataFilteringMNode(IStandardAnalysisFilterNodeBaseServices services) : base(services)
	{
	}

	/*******Read IIondata*******/
	public async Task<IIonData> GetIonData1()
	{
		return await Services.IonDataProvider.GetIonData(InstanceId);
	}

	public System.Guid GetId()
	{
		return InstanceId;
	}

	public System.Guid GetParentId()
	{
		return Services.IonDataProvider.Resolve(InstanceId) is { OwnerNodeId: { } ownerNodeId } ? ownerNodeId : InstanceId;
	}


	public IIonDisplayInfo GetIonDisplayInfo()
	{
		return _ionDisplayInfoProvider.Resolve(InstanceId);
	}

	public async Task<IIonData?> GetParentIonData(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
	{
		return Services.IonDataProvider.Resolve(InstanceId) is { OwnerNodeId: { } ownerNodeId }

			  ? await Services.IonDataProvider.GetIonData(ownerNodeId, progress, cancellationToken)

			  : null;
	}

#pragma warning disable CS1998
	protected override async IAsyncEnumerable<ReadOnlyMemory<ulong>> OutputIndices(
		IIonData inputIonData,
		IProgress<double>? progress = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore CS1998
	{
		List<ulong> indices = DataFilteringMViewModel.outIndices;
		yield return indices.ToArray();
	}
}


