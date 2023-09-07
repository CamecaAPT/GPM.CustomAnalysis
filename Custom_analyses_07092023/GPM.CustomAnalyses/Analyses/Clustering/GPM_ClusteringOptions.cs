using System.ComponentModel.DataAnnotations;
using Prism.Mvvm;

namespace GPM.CustomAnalyses.Analyses.Clustering;

// Module parameters
// ---------------------
#pragma warning disable 1591
public class GPM_ClusteringOptions : BindableBase
{
	private int iCalculationId = 0;
	private int iSelElementId = 3;
	private int iAllAtomFiltering = 0;
	private float fGridSize = 1.0f;
	private float fGridDelocalization = 0.5f;
	private float fCompositionThreshold = 10.0f;                   // Concentration threshold : Min
	private float fAtomicDistance = 0.5f;                      // Concentration distribution visualization : bin size
	private int iMinNbAtomPerCluster = 5;                           // Number of Iteration for the progress "bar"
	private int iAllAtomClustering = 0;

	[Display(Name = "CalculationId", Description = "Type of calculation")]
	public int CalculationId
	{
		get => iCalculationId;
		set => SetProperty(ref iCalculationId, value);
	}

	[Display(Name = "SelElementId", Description = "Selected element")]
	public int SelElementId
	{
		get => iSelElementId;
		set => SetProperty(ref iSelElementId, value);
	}

	[Display(Name = "AllAtomFiltering", Description = "Solute / All atom filtering")]
	public int AllAtomFiltering
	{
		get => iAllAtomFiltering;
		set => SetProperty(ref iAllAtomFiltering, value);
	}

	[Display(Name = "Grid size", Description = "Grid size")]
	public float GridSize
	{
		get => fGridSize;
		set => SetProperty(ref fGridSize, value);
	}

	[Display(Name = "Delocalization", Description = "Delocalization")]
	public float GridDelocalization
	{
		get => fGridDelocalization;
		set => SetProperty(ref fGridDelocalization, value);
	}

	[Display(Name = "Threshold", Description = "Composition Threshold")]
	public float CompositionThreshold
	{
		get => fCompositionThreshold;
		set => SetProperty(ref fCompositionThreshold, value);
	}

	[Display(Name = "AllAtomClustering", Description = "Add all atoms for clustering")]
	public int AllAtomClustering
	{
		get => iAllAtomClustering;
		set => SetProperty(ref iAllAtomClustering, value);
	}

	[Display(Name = "Distance", Description = "Distance")]
	public float AtomicDistance
	{
		get => fAtomicDistance;
		set => SetProperty(ref fAtomicDistance, value);
	}

	[Display(Name = "MinNbAtom", Description = "Min Nb Atom")]
	public int MinNbAtomPerCluster
	{
		get => iMinNbAtomPerCluster;
		set => SetProperty(ref iMinNbAtomPerCluster, value);
	}

}
#pragma warning restore 1591
