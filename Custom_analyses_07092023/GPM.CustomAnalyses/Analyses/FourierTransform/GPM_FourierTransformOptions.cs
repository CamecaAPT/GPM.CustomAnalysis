using System.ComponentModel.DataAnnotations;
using Prism.Mvvm;

namespace GPM.CustomAnalyses.Analyses.FourierTransform;

// Module parameters
// ---------------------
#pragma warning disable 1591
public class GPM_FourierTransformOptions : BindableBase
{
	private int iCalculationId = 0;
	private float fSampling = 0;
	private int iSpaceX = 0;
	private int iSpaceY = 0;
	private int iSpaceZ = 0;

	[Display(Name = "CalculationId", Description = "Type of calculation")]
	public int CalculationId
	{
		get => iCalculationId;
		set => SetProperty(ref iCalculationId, value);
	}

	[Display(Name = "Sampling", Description = "Sampling masse spectrum")]
	public float Sampling
	{
		get => fSampling;
		set => SetProperty(ref fSampling, value);
	}

	[Display(Name = "Space X (nm-1)", Description = "Size Fourier space X")]
	public int SpaceX
	{
		get => iSpaceX;
		set => SetProperty(ref iSpaceX, value);
	}

	[Display(Name = "Space Y (nm-1)", Description = "Size Fourier space Y")]
	public int SpaceY
	{
		get => iSpaceY;
		set => SetProperty(ref iSpaceY, value);
	}

	[Display(Name = "Space Z (nm-1)", Description = "Size Fourier space Z")]
	public int SpaceZ
	{
		get => iSpaceZ;
		set => SetProperty(ref iSpaceZ, value);
	}
}
#pragma warning restore 1591
