﻿using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
//using GPM.CustomAnalyses.Analyses.ClusterPositionM.Preview;
using GPM.CustomAnalyses.Images;

namespace GPM.CustomAnalyses.Analyses.ClusterPositionM;

// AnalysisNodeBase and variants (Standard/Legacy) use DefaultViewAttribute to easily link views with the main analysis.
// Base node will create these views when analysis is first created or the node is double-clicked if the view was closed.
// DefaultViewAttribute supports multiple instances, so a single analysis could easily control more than one view.
[DefaultView(ClusterPositionMViewModel.UniqueId, typeof(ClusterPositionMViewModel))]
internal class ClusterPositionMNode : StandardAnalysisNodeBase
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.ClusterPositionM.ClusterPositionMNode";

	// Adding an icon to the main display info instance and using this instance everywhere ensures common use of any custom icon
	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("ClusterPositionM", ImageResource.PieChartIcon);

	// Expose protected DataState object publicly for access by the ViewModel
	public INodeDataState? NodeDataState => base.DataState;

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;

	public bool test  = false;

	public ClusterPositionMNode(
		IStandardAnalysisNodeBaseServices services,
		IIonDisplayInfoProvider ionDisplayInfoProvider) : base(services)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
	}
	protected override byte[]? GetSaveContent()
	{
		return base.GetSaveContent();
	}

	/*******Read IIondata*******/
	public async Task<IIonData> GetIonData1()
	{
		return await Services.IonDataProvider.GetIonData(InstanceId);
	}

	public IIonData GetIonData()
	{
		return Services.IonDataProvider.GetResolvedIonData(InstanceId);
	}

	public IIonDisplayInfo GetIonDisplayInfo()
	{
		return _ionDisplayInfoProvider.Resolve(InstanceId);
	}

	public bool GetIsLoad()
	{
		return test;
	}

	protected override void OnAdded(NodeAddedEventArgs eventArgs)
	{
		base.OnAdded(eventArgs);
		// Can do initialization work here
		if (eventArgs.Trigger == EventTrigger.Create)
		{
			// Only called the very first time an analysis is created
		}
		if (eventArgs.Trigger == EventTrigger.Load)
		{
			test = true;
		}
	}

	public async Task<IReadOnlyDictionary<IIonTypeInfo, ulong>> GetIonTypeCounts()
	{
		var results = await Services.IonDataProvider.GetIonData(InstanceId) is { } ionData
			? ionData.GetIonTypeCounts()
			: new Dictionary<IIonTypeInfo, ulong>();
		if (DataState is not null) DataState.IsValid = true;
		return results;
	}
}
