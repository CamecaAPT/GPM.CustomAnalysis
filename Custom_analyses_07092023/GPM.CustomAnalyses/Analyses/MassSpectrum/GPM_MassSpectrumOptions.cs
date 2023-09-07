using System.ComponentModel.DataAnnotations;
using Prism.Mvvm;

namespace GPM.CustomAnalyses.Analyses.MassSpectrum;

// Module parameters
// ---------------------
#pragma warning disable 1591
public class GPM_MassSpectrumOptions : BindableBase
{
	private int iCalculationId = 0;
	private string sSelectedClusterId = "0";
	private string sSelectedEltId = "0";
	private int iSplit = 0;


	[Display(Name = "CalculationId", Description = "Type of calculation")]
	public int CalculationId
	{
		get => iCalculationId;
		set => SetProperty(ref iCalculationId, value);
	}

	[Display(Name = "SelectedCluId", Description = "Selected Cluster")]
	public string SelectedClusterId
	{
		get => sSelectedClusterId;
		set => SetProperty(ref sSelectedClusterId, value);
	}

	[Display(Name = "SelectedEltId", Description = "Selected Element")]
	public string SelectedEltId
	{
		get => sSelectedEltId;
		set => SetProperty(ref sSelectedEltId, value);
	}

	[Display(Name = "Split", Description = "0 : all cluster histo; 1 : split histo")]
	public int Split
	{
		get => iSplit;
		set => SetProperty(ref iSplit, value);
	}

}
#pragma warning restore 1591
