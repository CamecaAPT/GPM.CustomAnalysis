using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Microsoft.Win32.SafeHandles;




namespace GPM.CustomAnalyses.Analyses.FourierTransform;

internal class GPM_FourierTransform : ICustomAnalysis<GPM_FourierTransformOptions>
{
	public IIonDisplayInfo IonDisplayInfo { get; set; } = RandomColorIonDisplayInfo.Instance;

	Stopwatch ExecutionTime = new Stopwatch();

	private int iCalculationId = 0;
	private float fSampling = (float)0.01;
	private int iSpaceX = 10;
	private int iSpaceY = 10;
	private int iSpaceZ = 10;
	CAtom Atom = CustomAnalysesModule.Atom;
	

	public void Run(IIonData ionData, GPM_FourierTransformOptions options, IViewBuilder viewBuilder)
	{
		iCalculationId = options.CalculationId;
		
		// Test the calculation Id
		if (iCalculationId == 0)
		{
			Console.WriteLine("Fourier");
			return;
		}

		//Fourier X
		if (iCalculationId == 1)
		{
			//Data extract
			if (Atom.bState == false)
			{
				Console.WriteLine("Create Atom data memory ...");
				Atom.bInitMemory2(ionData, IonDisplayInfo);
			}
			Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

			float[,] resultFFT = FFT(iSpaceX, fSampling, 0);
			var testChart = viewBuilder.AddChart2D("FFT X", "Distance", "Intensity");
			testChart.AddLine(GetColumn(resultFFT, 0), GetColumn(resultFFT, 1), Colors.Black, "FFT X");			
		}

		//Fourier Y
		if (iCalculationId == 2)
		{
			//Data extract
			if (Atom.bState == false)
			{
				Console.WriteLine("Create Atom data memory ...");
				Atom.bInitMemory2(ionData, IonDisplayInfo);
			}
			Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

			float[,] resultFFT = FFT(iSpaceY, fSampling, 1);
			var testChart = viewBuilder.AddChart2D("FFT Y", "Distance", "Intensity");
			testChart.AddLine(GetColumn(resultFFT, 0), GetColumn(resultFFT, 1), Colors.Black, "FFT Y");
		}

		//Fourier Z
		if (iCalculationId == 3)
		{
			//Data extract
			if (Atom.bState == false)
			{
				Console.WriteLine("Create Atom data memory ...");
				Atom.bInitMemory2(ionData, IonDisplayInfo);
			}
			Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

			float[,] resultFFT = FFT(iSpaceZ, fSampling, 2);
			var testChart = viewBuilder.AddChart2D("FFT Z", "Distance", "Intensity");
			testChart.AddLine(GetColumn(resultFFT, 0), GetColumn(resultFFT, 1), Colors.Black, "FFT Z");
		}
	}

	public float[,] FFT(int sizeSpace, float sampling, byte colXYZ)
	{
		int nbStep = (int)(Math.Ceiling(sizeSpace / sampling));
		float[,] result = new float[nbStep,2];
		Complex j = new Complex(0, 1);
		Complex FF;
		float c = (float) (sizeSpace / 2.0);
		for (int i = 0; i < nbStep; i++, c -= (float)sampling)
		{
			FF = 0; 
			for (int jj = 0; jj < Atom.iMemSize; jj++)
			{
				FF += Atom.fMass[jj,0] * (Complex.Exp(2 * 3.14159 * j * (c * (Atom.fPos[jj, colXYZ]))));
				//Console.WriteLine(Atom.fPos[jj, col]);
			}
			result[i, 0] = c;
			result[i, 1] = (float)FF.Real;
		}
		float max = GetColumn(result,1).Max();
		for (int i = 0; i < nbStep; i++)
		{
			result[i,1] = Math.Abs(result[i, 1] / max);
		}
		return result;
	}

	public float[] GetColumn(float[,] matrix, int columnNumber)
	{
		return Enumerable.Range(0, matrix.GetLength(0))
				.Select(x => matrix[x, columnNumber])
				.ToArray();
	}
}
