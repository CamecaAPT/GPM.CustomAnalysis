using System.ComponentModel.DataAnnotations;
using Prism.Mvvm;

namespace GPM.CustomAnalyses.Analyses.FrequencyDistribution;

// Module parameters
// ---------------------
#pragma warning disable 1591
public class GPM_FrequencyDistributionOptions : BindableBase
{
	private int iCalculationId = 0;
	private int iSelectedElementId = 1;
	private int iBlocSize = 100;
	private int iCalculationType = 0;
	private int iTheoricalDst = 0;
	private int iMinBlocSize = 50;
	private int iMaxBlocSize = 200;
	private int iBlocStep = 50;


	[Display(Name = "CalculationId", Description = "Type of calculation")]
	public int CalculationId
	{
		get => iCalculationId;
		set => SetProperty(ref iCalculationId, value);
	}

	[Display(Name = "SelectedElementId", Description = "Selected Element")]
	public int SelectedElementId
	{
		get => iSelectedElementId;
		set => SetProperty(ref iSelectedElementId, value);
	}

	[Display(Name = "BlocSize", Description = "Nb atom / Bloc")]
	public int BlocSize
	{
		get => iBlocSize;
		set => SetProperty(ref iBlocSize, value);
	}

	[Display(Name = "CalculationType", Description = "Calculation type : 0 in Atoms / 1 in %")]
	public int CalculationType
	{
		get => iCalculationType;
		set => SetProperty(ref iCalculationType, value);
	}

	[Display(Name = "TheoricalDst", Description = "Theorical distribution")]
	public int TheoricalDst
	{
		get => iTheoricalDst;
		set => SetProperty(ref iTheoricalDst, value);
	}

	[Display(Name = "MinBlocSize", Description = "Min bloc size")]
	public int MinBlocSize
	{
		get => iMinBlocSize;
		set => SetProperty(ref iMinBlocSize, value);
	}

	[Display(Name = "MaxBlocSize", Description = "Max bloc size")]
	public int MaxBlocSize
	{
		get => iMaxBlocSize;
		set => SetProperty(ref iMaxBlocSize, value);
	}

	[Display(Name = "BlocStep", Description = "Bloc step increment")]
	public int BlocStep
	{
		get => iBlocStep;
		set => SetProperty(ref iBlocStep, value);
	}
	
}
#pragma warning restore 1591
