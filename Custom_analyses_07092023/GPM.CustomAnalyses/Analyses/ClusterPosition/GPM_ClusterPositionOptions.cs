using System.ComponentModel.DataAnnotations;
using Prism.Mvvm;

namespace GPM.CustomAnalyses.Analyses.ClusterPosition;

// Module parameters
// ---------------------
#pragma warning disable 1591
public class GPM_ClusterPositionOptions : BindableBase
{
	private int iCalculationId = 0;
	private string sSelectedElmtId = "-1";
	private string sSelectedClusterId = "-1";
	private int iRepresentationId = 0;
	private float fDistanceThres = 0.3f;


	[Display(Name = "CalculationId", Description = "Type of calculation")]
	public int CalculationId
	{
		get => iCalculationId;
		set => SetProperty(ref iCalculationId, value);
	}

	[Display(Name = "SelectedElmtId", Description = "Selected Element")]
	public string SelectedElmtId
	{
		get => sSelectedElmtId;
		set => SetProperty(ref sSelectedElmtId, value);
	}

	[Display(Name = "SelectedClusterId", Description = "-1:All, 0:MAtrix, id:Id Cluster")]
	public string SelectedClusterId
	{
		get => sSelectedClusterId;
		set => SetProperty(ref sSelectedClusterId, value);
	}

	[Display(Name = "RepresentationId", Description = "0:rdm color, 1:SelectedElmtId color")]
	public int RepresentationId
	{
		get => iRepresentationId;
		set => SetProperty(ref iRepresentationId, value);
	}

	[Display(Name = "Distance threshold", Description = "Distance threshold")]
	public float DistanceThreshold
	{
		get => fDistanceThres;
		set => SetProperty(ref fDistanceThres, value);
	}

}
#pragma warning restore 1591
