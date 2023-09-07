using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Printing;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using GPM.CustomAnalyses.Analyses.Clustering;
using CommunityToolkit.HighPerformance;
using Microsoft.VisualBasic;
using Microsoft.Win32.SafeHandles;
//using System.Windows.Forms;  
// BindableBase
/*
#region assembly Cameca.Chart, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
//C:\Program Files\CAMECA Instruments\AP Suite\Cameca.Chart.dll
#endregion


using System.Windows.Media;
*/


namespace GPM.CustomAnalyses.Analyses.MassSpectrum;

internal class GPM_MassSpectrum : ICustomAnalysis<GPM_MassSpectrumOptions>
{
	public IIonDisplayInfo IonDisplayInfo { get; set; } = RandomColorIonDisplayInfo.Instance;

	// Parameters 
	// -------------
	private int iCalculationId = 0;
	private string sSelectedClusterId = "";
	private string sSelectedEltId = "";
	private int iSplit = 0;


	// Variable declaration
	// ----------------------
	CAtom Atom = CustomAnalysesModule.Atom;
	//Dictionary<int,List<ClusterRepresentation>> tabCluster = new Dictionary<int, List<ClusterRepresentation>>();
	List<ClusterRepresentation>[] tabCluster;
	ClusterRepresentation rep = new ClusterRepresentation();
	Color[] tabColor = { Colors.Blue, Colors.Red, Colors.Green, Colors.Yellow, Colors.Pink, Colors.Brown, Colors.Gray, Colors.Orange, Colors.Purple, Colors.White };
	int[] iSelectedClustertId;
	int[] iSelectedEltId;

	public struct ClusterRepresentation
	{
		public int id;
		public float X;
		public float Y;
		public float Z;
		public int idElmt;
		public float fMass;
		public int idClus;
	}

	Stopwatch ExecutionTime = new Stopwatch();

	public void Run(IIonData ionData, GPM_MassSpectrumOptions options, IViewBuilder viewBuilder)
	{
		// Conversion US-FR
		// ---------------------
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");


		// Local variables
		// -----------------
		int iNumberOfThread = Environment.ProcessorCount * 2;

		// Menu parameters
		// -----------------
		iCalculationId = options.CalculationId;
		sSelectedClusterId = options.SelectedClusterId;
		sSelectedEltId = options.SelectedEltId;
		iSplit = options.Split;

		Console.WriteLine("		");

		// Test the calculation Id
		if (iCalculationId == 0)
		{
			Console.WriteLine("Select a Calculation Id to compute  :  {0}", Atom.bState);
			return;
		}

		// Mass spectrum Cluster
		if (iCalculationId == 1)
		{


			//Init Memory Atom
			if (Atom.bState == false)
			{
				Console.WriteLine("Create Atom data memory ...");
				Atom.bInitMemory2(ionData, IonDisplayInfo);
			}
			Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

			iSelectedClustertId = sSelectedClusterId.Split(';').Select(x => int.Parse(x)).ToArray();
			CreateClusterStruct();

			ExecutionTime.Restart();
			int[] idSelectedAtoms;
			List<int> idAtoms = new List<int>();
			if (iSelectedClustertId[0] == -1)
			{
				iSelectedClustertId = new int[Atom.iNbCluster];
				for (int i = 1; i <= Atom.iNbCluster; i++)
				{
					iSelectedClustertId[i - 1] = i;
				}
			}
			foreach (int clusterId in iSelectedClustertId)
			{
				foreach (ClusterRepresentation ato in tabCluster[clusterId])
				{
					idAtoms.Add(ato.id);
				}
			}
			idSelectedAtoms = new int[idAtoms.Count];
			idSelectedAtoms = idAtoms.ToArray();
			ExecutionTime.Stop();
			Console.WriteLine("Select : " + ExecutionTime.Elapsed.TotalSeconds);

			ExecutionTime.Restart();
			var chart2D = viewBuilder.AddChart2D("Histo Cluster", "Mass", "Number of atoms");

			float[,] dataHisto;

			if (iSplit == 0)
			{
				dataHisto = histo(idSelectedAtoms, Atom.iNbElt + 2);
			}
			else
			{
				dataHisto = histo(idSelectedAtoms, Atom.iNbCluster + 2);
			}

			Plot2D(dataHisto, chart2D);

			ExecutionTime.Stop();

			Console.WriteLine("Plot : " + ExecutionTime.Elapsed.TotalSeconds);
		}

		//Mass history
		if (iCalculationId == 2)
		{
			//Init Memory Atom
			if (Atom.bState == false)
			{
				Console.WriteLine("Create Atom data memory ...");
				Atom.bInitMemory2(ionData, IonDisplayInfo);
			}
			Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

			var chart3D = viewBuilder.AddChart3D("Mass History");

			iSelectedEltId = sSelectedEltId.Split(';').Select(x => int.Parse(x)).ToArray();
			if (iSelectedEltId[0] == -1)
			{
				iSelectedEltId = new int[Atom.iNbElt + 1];
				for(int i = 0; i < iSelectedEltId.Length; i++)
				{
					iSelectedEltId[i] = i;
				}
			}

			ClusterRepresentation[] massHistory = new ClusterRepresentation[Atom.iMemSize];
			for (int i = 0; i < Atom.iMemSize; i++)
			{
				rep.X = Atom.fMass[i, 0];
				rep.Y = Atom.fPos[i, 2];
				rep.Z = (Atom.bEltId[i, 0] == 255 ? Atom.iNbElt : Atom.bEltId[i, 0]);
				massHistory[i] = rep;
			}

			foreach(int idElt in iSelectedEltId)
			{
				float[] fX = massHistory.Where(ato => ato.Z == idElt).Select(ato => ato.X).ToArray(); 
				float[] fY = massHistory.Where(ato => ato.Z == idElt).Select(ato => ato.Y).ToArray();
				float[] fZ = massHistory.Where(ato => ato.Z == idElt).Select(ato => ato.Z).ToArray();
				Color color = (idElt == Atom.iNbElt ? Colors.Gray : Atom.EltColor[idElt]);
				chart3D.AddPoints(fX, fY, fZ, color);
			}
		}

		if (iCalculationId == 3)
		{
			
		}

		if (iCalculationId == 4)
		{
			
		}
	}

	public bool Plot2D(float[,] data, IChart2DBuilder chart2D)
	{
		float[] fX = GetColumn(data, 0);
		float[] fY;
		Color color;
		List<IChart2DSlice> slices;

		if (iSplit == 0)
		{
			for (int i = 1; i <= Atom.iNbElt + 1; i++)
			{
				color = (i == Atom.iNbElt + 1 ? Colors.Gray : Atom.EltColor[i - 1]);
				fY = GetColumn(data, i);
				slices = new List<IChart2DSlice> { new Chart2DSlice(fX.Min(),fX.Max(), color) };
				chart2D.AddHistogram(fX, fY, color, slices, "histo");
			}
		}
		else
		{
			for (int i = 0; i < iSelectedClustertId.Length; i++)
			{
				color = tabColor[iSelectedClustertId[i] % 10];
				fY = GetColumn(data, iSelectedClustertId[i]).Select(n => n * (float)Math.Pow(10, 2 * i) + (float)Math.Pow(10, 2 * i)).ToArray();
				chart2D.AddHistogram(fX, fY, color, null, "histo");
			}
		}
		return true;
	}

	public bool CreateClusterStruct()
	{
		ClusterRepresentation rep = new ClusterRepresentation();

		tabCluster = new List<ClusterRepresentation>[Atom.iNbCluster + 1];
		for (int i = 0; i <= Atom.iNbCluster; i++)
		{
			tabCluster[i] = new List<ClusterRepresentation>();
		}

		for (int i = 0; i < Atom.iMemSize; i++)
		{
			rep.id = i;
			rep.X = Atom.fPos[i, 0];
			rep.Y = Atom.fPos[i, 1];
			rep.Z = Atom.fPos[i, 2];
			rep.idElmt = Atom.bEltId[i, 0];
			rep.fMass = Atom.fMass[i, 0];
			rep.idClus = Atom.iCluId[i];
			//rep.iBlockId = new int[3] {(int)((Atom.fPos[i, 0] - fSubLimit[0])/fSampling), (int)((Atom.fPos[i, 1] - fSubLimit[1]) / fSampling), (int)((Atom.fPos[i, 2] - fSubLimit[2]) / fSampling) };
			tabCluster[Atom.iCluId[i]].Add(rep);
		}
		return true;
	}

	public float[,] histo(int[] id, int nb)
	{
		float fSampling = 0.01f;
		float fMax = GetColumn(Atom.fMass, 0).Max();
		int idElt;
		int idClu;
		float[,] hist;

		hist = new float[(int)Math.Ceiling((fMax / fSampling)), nb];

		for (int i = 0; i < fMax / fSampling; i++)
		{
			hist[i, 0] = i * fSampling;
		}

		if (iSplit == 0)
		{
			for (int i = 0; i < id.Length; i++)
			{
				idElt = (Atom.bEltId[id[i], 0] == 255 ? Atom.iNbElt + 1 : Atom.bEltId[id[i], 0] + 1);
				hist[(int)(Atom.fMass[id[i], 0] / fSampling), idElt]++;
			}
		}
		else
		{
			for (int i = 0; i < id.Length; i++)
			{
				idClu = Atom.iCluId[id[i]];
				hist[(int)(Atom.fMass[id[i], 0] / fSampling), idClu]++;
			}
		}
		return hist;
	}

	public float[] GetColumn(float[,] matrix, int columnNumber)
	{
		return Enumerable.Range(0, matrix.GetLength(0)).Select(x => matrix[x, columnNumber]).ToArray();
	}

}
