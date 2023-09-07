using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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
using Microsoft.Win32.SafeHandles;
//using System.Windows.Forms;  
// BindableBase
/*
#region assembly Cameca.Chart, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
//C:\Program Files\CAMECA Instruments\AP Suite\Cameca.Chart.dll
#endregion


using System.Windows.Media;
*/


namespace GPM.CustomAnalyses.Analyses.ClusterPosition;

internal class GPM_ClusterPosition : ICustomAnalysis<GPM_ClusterPositionOptions>
{
	public IIonDisplayInfo IonDisplayInfo { get; set; } = RandomColorIonDisplayInfo.Instance;

	// Parameters 
	// -------------
	private int iCalculationId = 0;
	private string sSelectedElmtId = "";
	private string sSelectedClusterId = "";
	private int iRepresentationId = 0;
	private float fDistanceThres;


	// Variable declaration
	// ----------------------
	CAtom Atom = CustomAnalysesModule.Atom;
	List<ClusterRepresentation>[] tabCluster;
	Color[] tabColor = { Colors.Blue, Colors.Red, Colors.Green, Colors.Yellow, Colors.Pink, Colors.Brown, Colors.Gray, Colors.Orange, Colors.Purple, Colors.White };

	
	public float[] fSubLimit = new float[3];
	int[] iOldCluId;
	int[] iNewCluId;

	public struct ClusterRepresentation
	{
		public int id;
		public float X;
		public float Y;
		public float Z;
		public int idElmt;
		//public int[] iBlockId;
	}

	Stopwatch ExecutionTime = new Stopwatch();

	public void Run(IIonData ionData, GPM_ClusterPositionOptions options, IViewBuilder viewBuilder)
	{
		// Conversion US-FR
		// ---------------------
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");


		// Local variables
		// -----------------
		Color colorRepCluster = Colors.White;
		float[] fDelta = new float[3];
		float[,,] fLimits = new float[Atom.iNbCluster + 1, 3, 2];
		int iNumberOfThread = Environment.ProcessorCount * 2;
		int iThreadStep = 0;

		// Menu parameters
		// -----------------
		iCalculationId = options.CalculationId;
		sSelectedElmtId = options.SelectedElmtId;
		sSelectedClusterId = options.SelectedClusterId;
		iRepresentationId = options.RepresentationId;
		fDistanceThres = options.DistanceThreshold;

		Console.WriteLine("		");

		// Test the calculation Id
		if (iCalculationId == 0)
		{
			Console.WriteLine("Select a Calculation Id to compute  :  {0}", Atom.bState);
			return;
		}

		// Plot cluster position
		if (iCalculationId == 1)
		{
			//Init Memory Atom
			if (Atom.bState == false)
			{
				Console.WriteLine("Create Atom data memory ...");
				Atom.bInitMemory2(ionData, IonDisplayInfo);
			}
			Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

			//Init Clurter struct
			CreateClusterStruct();

			//Plot 3D cluster
			var chart3D = viewBuilder.AddChart3D("Cluster Representation");
			Plot3D(chart3D);
		}

		// Growing
		if (iCalculationId == 2)
		{
			//Init Memory Atom
			if (Atom.bState == false)
			{
				Console.WriteLine("Create Atom data memory ...");
				Atom.bInitMemory2(ionData, IonDisplayInfo);
			}
			Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

			//Init Clurter struct to calculation
			CreateClusterStruct();

			//Find Cluster Limits
			ExecutionTime.Restart();
			for (int i = 1; i <= Atom.iNbCluster; i++)
			{
				fLimits[i, 0, 0] = tabCluster[i].Min(x => x.X) - fDistanceThres;
				fLimits[i, 0, 1] = tabCluster[i].Max(x => x.X) + fDistanceThres;
				fLimits[i, 1, 0] = tabCluster[i].Min(x => x.Y) - fDistanceThres;
				fLimits[i, 1, 1] = tabCluster[i].Max(x => x.Y) + fDistanceThres;
				fLimits[i, 2, 0] = tabCluster[i].Min(x => x.Z) - fDistanceThres;
				fLimits[i, 2, 1] = tabCluster[i].Max(x => x.Z) + fDistanceThres;
				//Console.WriteLine("CluId : " + i + " Clu Limits : " + fLimits[i, 0, 0] + " " + fLimits[i, 0, 1] + " " + fLimits[i, 1, 0] + " " + fLimits[i, 1, 1] + " " + fLimits[i, 2, 0] + " " + fLimits[i, 2, 1]);
			}

			//Growing Calcul
			iNewCluId = Atom.iCluId.ToArray();
			iOldCluId = Atom.iCluId.ToArray();

			iThreadStep = tabCluster[0].Count  / iNumberOfThread;
			Parallel.For(0, iNumberOfThread,  i =>
			{
				int iStart = i * iThreadStep;
				int iStop = (i + 1) * iThreadStep;
				iStop = (i == iNumberOfThread - 1) ? tabCluster[0].Count : iStop;
				Console.WriteLine("id " + i + " start " + iStart + "    stop " + iStop);

				ThreadGrowingLoop(iStart, iStop, fLimits, tabCluster);
			});

			//Replace Atom Cluster ID by new Cluster Id
			Atom.iCluId = iNewCluId.ToArray();

			ExecutionTime.Stop();
			Console.WriteLine("Executioon time Growing : " + ExecutionTime.Elapsed.TotalSeconds);

			//Recalculated ClusterStructure with new Cluster Id
			CreateClusterStruct();

			//Plot 3D cluster
			var chart3D = viewBuilder.AddChart3D("Cluster Representation");
			Plot3D(chart3D);
		}

		//Erosion
		if (iCalculationId == 3)
		{
			//Init Memory Atom
			if (Atom.bState == false)
			{
				Console.WriteLine("Create Atom data memory ...");
				Atom.bInitMemory2(ionData, IonDisplayInfo);
			}
			Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

			//Init Clurter struct to calculation
			CreateClusterStruct();

			//Find Limits for atom arround cluster
			ExecutionTime.Restart();
			for (int i = 1; i <= Atom.iNbCluster; i++)
			{
				if(tabCluster[i].Count != 0)
				{
					fLimits[i, 0, 0] = tabCluster[i].Min(x => x.X) - 0.15f;
					fLimits[i, 0, 1] = tabCluster[i].Max(x => x.X) + 0.15f;
					fLimits[i, 1, 0] = tabCluster[i].Min(x => x.Y) - 0.15f;
					fLimits[i, 1, 1] = tabCluster[i].Max(x => x.Y) + 0.15f;
					fLimits[i, 2, 0] = tabCluster[i].Min(x => x.Z) - 0.15f;
					fLimits[i, 2, 1] = tabCluster[i].Max(x => x.Z) + 0.15f;
				}
				//Console.WriteLine("CluId : " + i + " Clu Limits : " + fLimits[i, 0, 0] + " " + fLimits[i, 0, 1] + " " + fLimits[i, 1, 0] + " " + fLimits[i, 1, 1] + " " + fLimits[i, 2, 0] + " " + fLimits[i, 2, 1]);
			}

			//Erosion Calcul
			iNewCluId = Atom.iCluId.ToArray();
			iOldCluId = Atom.iCluId.ToArray();

			iThreadStep = Atom.iNbCluster  / iNumberOfThread;
			Parallel.For(0, iNumberOfThread, i =>
			{
				int iStart = i * iThreadStep + 1;
				int iStop = (i + 1) * iThreadStep;
				iStop = (i == iNumberOfThread - 1) ? Atom.iNbCluster : iStop;
				Console.WriteLine(" id : " + i + " start " + iStart + "    stop " + iStop);

				ThreadErosionLoop(iStart, iStop, fLimits, tabCluster);
			});

			//Replace Atom Cluster ID by new Cluster Id
			Atom.iCluId = iNewCluId.ToArray();

			ExecutionTime.Stop();
			Console.WriteLine("Execution time test erosion: " + ExecutionTime.Elapsed.TotalSeconds);

			//Recalculated Cluster Struct with new Cluster Id
			CreateClusterStruct();

			//Plot 3D cluster
			var chart3D = viewBuilder.AddChart3D("Cluster Representation");
			Plot3D(chart3D);
		}

		//Undo
		if (iCalculationId == 4)
		{
			Atom.iCluId = iOldCluId.ToArray();

			//Init Clurter struct to calculation
			CreateClusterStruct();

			//Plot 3D cluster
			var chart3D = viewBuilder.AddChart3D("Cluster Representation");
			Plot3D(chart3D);
		}
	}

	public bool Plot3D(IChart3DBuilder chart3D)
	{
		int idElt = 0;
		bool[] repElt = new bool[Atom.iNbElt + 1];
		bool[] repClu = new bool[Atom.iNbCluster + 1];
		Color colorRepCluster = Colors.White;

		Array.Fill(repElt, false);
		Array.Fill(repClu, false);
		int[] iSelectedElmtId = sSelectedElmtId.Split(';').Select(x => int.Parse(x)).ToArray();
		if (iSelectedElmtId[0] == -1)
		{
			for (int id =0; id < Atom.iNbElt; id++)
			{
				repElt[id] = true;
				Console.WriteLine("Sleced Elt ID: " + id);
			}
		}
		else
		{
			foreach (int id in iSelectedElmtId)
			{
				repElt[id] = true;
				Console.WriteLine("Sleced Elt ID: " + id);
			}
		}

		int[] iSelectedCluId = sSelectedClusterId.Split(';').Select(x => int.Parse(x)).ToArray();
		if (iSelectedCluId[0] == -1)
		{
			for (int id = 1; id <= Atom.iNbCluster; id++)
			{
				repClu[id] = true;
				Console.WriteLine("Sleced Clu ID: " + id);
			}
		}
		else
		{
			foreach (int id in iSelectedCluId)
			{
				repClu[id] = true;
				Console.WriteLine("Sleced Clu ID: " + id);
			}
		}

		for (int ii = 0; ii <= Atom.iNbElt; ii++)
		{
			if (repElt[ii] == true)
			{
				idElt = (ii == Atom.iNbElt ? 255 : ii);
				for (int i = 0; i <= Atom.iNbCluster; i++)
				{
					if (repClu[i] == true)
					{
						colorRepCluster = (iRepresentationId == 1) ? tabColor[i % 10] : (iRepresentationId == 0 && idElt != 255) ? Atom.EltColor[ii] : Colors.Gray;
						chart3D.AddPoints(FindElementPos(tabCluster[i], idElt, 'X'), FindElementPos(tabCluster[i], idElt, 'Y'), FindElementPos(tabCluster[i], idElt, 'Z'), colorRepCluster); ;

					}
				}
			}
		}
		return true;
	}

	public float[] FindElementPos(List<ClusterRepresentation> values, int id, char position)
	{
		if (position == 'X')
		{
			return values.Where(x => x.idElmt == id).Select(y => y.X).ToArray();
		}
		else if (position == 'Y')
		{
			return values.Where(x => x.idElmt == id).Select(y => y.Y).ToArray();
		}
		else if (position == 'Z')
		{
			return values.Where(x => x.idElmt == id).Select(y => y.Z).ToArray();
		}
		else
		{
			return new float[0];
		}
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
			//rep.iBlockId = new int[3] {(int)((Atom.fPos[i, 0] - fSubLimit[0])/fSampling), (int)((Atom.fPos[i, 1] - fSubLimit[1]) / fSampling), (int)((Atom.fPos[i, 2] - fSubLimit[2]) / fSampling) };
			tabCluster[Atom.iCluId[i]].Add(rep);
		}

		return true;
	}

	public void ThreadErosionLoop(int start, int stop, float[,,] fCluLimits, List<ClusterRepresentation>[] tab)
	{
		ClusterRepresentation[] slectMatrixCluster;
		float[] fDelta = new float[3];
		float dist = 0;
		float squareThreshold = fDistanceThres * fDistanceThres;
		int iNbAtoSelcted = 0;
		for (int i = start; i <= stop; i++) // Loop on clusters
		{
			//Select Matrix atoms arround cluster
			slectMatrixCluster = tab[0].Where(o => o.X > fCluLimits[i, 0, 0] && o.X < fCluLimits[i, 0, 1]
				&& o.Y > fCluLimits[i, 1, 0] && o.Y < fCluLimits[i, 1, 1]
				&& o.Z > fCluLimits[i, 2, 0] && o.Z < fCluLimits[i, 2, 1]).ToArray();
			iNbAtoSelcted = slectMatrixCluster.Count();
			for (int j = 0; j < tab[i].Count; j++) //Loop on Atoms in cluster
			{
				for (int k = 0; k < iNbAtoSelcted; k++) // Loop on matrix selected Atoms
				{
					fDelta[0] = (tab[i][j].X - slectMatrixCluster[k].X); fDelta[0] *= fDelta[0];
					fDelta[1] = (tab[i][j].Y - slectMatrixCluster[k].Y); fDelta[1] *= fDelta[1];
					fDelta[2] = (tab[i][j].Z - slectMatrixCluster[k].Z); fDelta[2] *= fDelta[2];
					dist = fDelta[0] + fDelta[1] + fDelta[2];
					if (dist < squareThreshold)
					{
						iNewCluId[tab[i][j].id] = 0;
						//Console.WriteLine ("Cluster id : " + i + " ato Id : " + ato.id + " old Cul id : " + Atom.iCluId[ato.id] + " new clu id " + iNewCluId[ato.id]);
						break;
					}
				}
			}
		}
	}

	public void ThreadGrowingLoop(int start, int stop, float[,,] fCluLimits, List<ClusterRepresentation>[] tab)
	{
		float[] fDelta = new float[3];
		float dist = 0;
		float squareThreshold = fDistanceThres * fDistanceThres;
		for (int k = start; k < stop; k++) //Loop on Matrix atoms
		{
			for (int i = 1; i <= Atom.iNbCluster; i++) //Loop on Cluster
			{
				if (tab[0][k].X > fCluLimits[i, 0, 0] && tab[0][k].X < fCluLimits[i, 0, 1]
					&& tab[0][k].Y > fCluLimits[i, 1, 0] && tab[0][k].Y < fCluLimits[i, 1, 1]
					&& tab[0][k].Z > fCluLimits[i, 2, 0] && tab[0][k].Z < fCluLimits[i, 2, 1])
				{
					//Console.WriteLine(" ato Id : " + ato.id);
					for (int j = 0; j < tab[i].Count; j++) //Loop on Atoms in cluster
					{
						fDelta[0] = (tab[i][j].X - tab[0][k].X); fDelta[0] *= fDelta[0];
						fDelta[1] = (tab[i][j].Y - tab[0][k].Y); fDelta[1] *= fDelta[1];
						fDelta[2] = (tab[i][j].Z - tab[0][k].Z); fDelta[2] *= fDelta[2];
						dist = fDelta[0] + fDelta[1] + fDelta[2];
						if (dist < squareThreshold)
						{
							iNewCluId[tab[0][k].id] = i;
							//Console.WriteLine ("Cluster id : " + i + " ato Id : " + ato.id + " old Cul id : " + Atom.iCluId[ato.id] + " new clu id " + iNewCluId[ato.id]);
							break;
						}
					}
				}
			}
		}
	}

}
