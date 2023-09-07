using System.ComponentModel.DataAnnotations;
using Prism.Mvvm;

namespace GPM.CustomAnalyses.Analyses.ClusterInformation;

// Module parameters
// ---------------------
#pragma warning disable 1591
public class GPM_ClusterInformationOptions : BindableBase
{
	private int iCalculationId = 0;
	private int iSelectedClusterId = 1;
	private int iMinNbAtomPerCluster = 5;
	private float fClassSize = 0.1f;
	private float fDistanceMax = 2.0f;


	[Display(Name = "CalculationId", Description = "Type of calculation")]
	public int CalculationId
	{
		get => iCalculationId;
		set => SetProperty(ref iCalculationId, value);
	}

	[Display(Name = "SelectedClusterId", Description = "Selected cluster (0 for all)")]
	public int SelectedClusterId
	{
		get => iSelectedClusterId;
		set => SetProperty(ref iSelectedClusterId, value);
	}

	[Display(Name = "MinNbAtomPerCluster", Description = "Min Nb atom / cluster")]
	public int MinNbAtomPerCluster
	{
		get => iMinNbAtomPerCluster;
		set => SetProperty(ref iMinNbAtomPerCluster, value);
	}

	[Display(Name = "ClassSize", Description = "Class size (nm)")]
	public float ClassSize
	{
		get => fClassSize;
		set => SetProperty(ref fClassSize, value);
	}

	[Display(Name = "DistanceMax", Description = "Maximum distance (nm)")]
	public float DistanceMax
	{
		get => fDistanceMax;
		set => SetProperty(ref fDistanceMax, value);
	}

}
#pragma warning restore 1591
