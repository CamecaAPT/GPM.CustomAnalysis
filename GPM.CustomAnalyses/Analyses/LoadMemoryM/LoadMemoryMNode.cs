﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
//using GPM.CustomAnalyses.Analyses.LoadMemoryM.Preview;

namespace GPM.CustomAnalyses.Analyses.LoadMemoryM;

// AnalysisNodeBase and variants (Standard/Legacy) use DefaultViewAttribute to easily link views with the main analysis.
// Base node will create these views when analysis is first created or the node is double-clicked if the view was closed.
// DefaultViewAttribute supports multiple instances, so a single analysis could easily control more than one view.
[DefaultView(LoadMemoryMViewModel.UniqueId, typeof(LoadMemoryMViewModel))]
internal class LoadMemoryMNode : StandardAnalysisNodeBase
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.LoadMemoryM.LoadMemoryMNode";

	// Adding an icon to the main display info instance and using this instance everywhere ensures common use of any custom icon
	public static INodeDisplayInfo DisplayInfo { get; } = new NodeDisplayInfo("LoadMemoryM");

	// Expose protected DataState object publicly for access by the ViewModel
	public INodeDataState? NodeDataState => base.DataState;

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
    private readonly IReconstructionSectionsProvider reconstructionSectionsProvider;
    private readonly IIonDataProvider _ionDataProvider;

	CAtom Atom = CustomAnalysesModule.Atom;
	IIonData IonDataMemory;

	public LoadMemoryMNode(
		IStandardAnalysisNodeBaseServices services,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
        IReconstructionSectionsProvider reconstructionSectionsProvider) : base(services)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
        this.reconstructionSectionsProvider = reconstructionSectionsProvider;
    }

	internal async Task<bool> EnsureSectionsAvailable(
		IIonData ionData, IEnumerable<string> sections,
		IProgress<double>? progress = null, CancellationToken cancellationToken = default)
	{
		var reconstructionSections = reconstructionSectionsProvider.Resolve(InstanceId);
		return await ionData.EnsureSectionsAvailable(sections, reconstructionSections, progress, cancellationToken);
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


	public IIonDisplayInfo GetIonDisplayInfo()
	{
		return _ionDisplayInfoProvider.Resolve(InstanceId);
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
