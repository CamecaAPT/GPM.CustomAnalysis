using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Microsoft.Win32.SafeHandles;




namespace GPM.CustomAnalyses.Analyses.Clustering;

internal class GPM_Clustering : ICustomAnalysis<GPM_ClusteringOptions>
{
	public IIonDisplayInfo IonDisplayInfo { get; set; } = RandomColorIonDisplayInfo.Instance;


	// Parameters 
	// -------------
	private int iCalculationId = 0;
	private int iSelElementId = 3;
	private int iAllAtomFiltering = 0;
	private float fGridSize = 1.0f;                // Grid size in nm
	private float fGridDelocalization = 0.5f;                   // Delocalization in nm
	private float fCompositionThreshold = 10;                   // Concentration threshold : Min
	private float fAtomicDistance = 0.1f;                      // Concentration distribution visualization : bin size
	private int iMinNbAtomPerCluster = 5;                           // Number of Iteration for the progress "bar"
	private int iAllAtomClustering = 0;


	// Variable declaration
	// ----------------------
	List<float> mass = new List<float>();
	List<Vector3> pos_init = new List<Vector3>();
	List<byte> name = new List<byte>();
	int Name_Id;
	private Vector3[] exp_dist;
	private Vector3[] rnd_dist;

	CMapping Map3d = new CMapping();

	CAtom Atom = CustomAnalysesModule.Atom;

	bool bFilteringState = false;

	int iNbParticuleMax = 50000;
		
	Stopwatch ExecutionTime = new Stopwatch();



	public void Run(IIonData ionData, GPM_ClusteringOptions options, IViewBuilder viewBuilder)
	{

		// Conversion US-FR
		// ---------------------
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");


		// Local variables
		// -----------------
		List<string> impact_name;

		bool[] bShowElt ;

		int i, j;
		int iTest, iNbAtom;
		int iNbElt, iIndex;

		float[] fMapSize = new float[3];
		float[] fLocalCompo = new float[2];

		float fHistogramResolution = (float)0.1;
		int[,] iHistogram = new int[Convert.ToInt32(102 / fHistogramResolution), 2];


		// Menu parameters
		// -----------------
		iCalculationId = options.CalculationId;

		if (iSelElementId != options.SelElementId)
		{
			iSelElementId = options.SelElementId;
			Map3d.bState = false;
		}

		iAllAtomFiltering = options.AllAtomFiltering;

		if (fGridSize != options.GridSize)
		{
			fGridSize = options.GridSize;
			Map3d.bState = false;
		}

		if (fGridDelocalization != options.GridDelocalization)
		{
			fGridDelocalization = options.GridDelocalization;
			Map3d.bState = false;
		}

		fCompositionThreshold = options.CompositionThreshold;

		iAllAtomClustering = options.AllAtomClustering;
		fAtomicDistance = options.AtomicDistance;
		iMinNbAtomPerCluster = options.MinNbAtomPerCluster;

		Console.WriteLine("		");


		// Data extraction for GPM
		// -------------------------
		if (Atom.bState == false)
		{
			Console.WriteLine("Create Atom data memory ...");
			Atom.bInitMemory2(ionData, IonDisplayInfo);
		}
		Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);


		// Test the calculation Id
		// ---------------------------
		if (iCalculationId == 0)
		{
			Console.WriteLine("Select a Calculation Id to compute !!");

			return;
		}


		// Atom filtering with composition grid
		// -----------------------------------------
		if (iCalculationId == 1)
		{
			Console.WriteLine("GPM  -  Atom filtering with composition grid");
			Console.WriteLine("Grid Size = {0} nm    Delocalization = {1} nm    Composition Threshold = {2} %", fGridSize, fGridDelocalization, fCompositionThreshold);
			ExecutionTime.Restart();


			// Init parameters
			// -----------------
			fMapSize[0] = fMapSize[1] = fMapSize[2] = fGridSize;


			// Composition map
			// -----------------
			if (Map3d.bState == false)
			{
				Console.WriteLine("Calculate Composition Map ...");

				bShowElt = new bool[256];
				Array.Clear(bShowElt, 0, bShowElt.Length);

				bShowElt[iSelElementId] = true;

				Map3d.bBuildComposition(fMapSize, fGridDelocalization, Atom, bShowElt);

				Console.WriteLine("Calculate Composition Map : OK ");
			}


			// Calculate composition for each atom
			// --------------------------------------
			Console.WriteLine("Calculate Atom composition ...");

			int iNbFilteredAtom = 0;

			Array.Clear(iHistogram, 0, iHistogram.Length);

			for (i = 0; i < Atom.iMemSize; i++)
			{
				Map3d.bCalculateAtomComposition(fLocalCompo, Atom.fPos[i, 0], Atom.fPos[i, 1], Atom.fPos[i, 2]);

				// Atom filtering
				Atom.iCluId[i] = 0;
				if (fLocalCompo[0] >= fCompositionThreshold)
				{
					Atom.iCluId[i] = 1;
					iNbFilteredAtom++;
				}

				// Composition histogram
				for (j = 0; j < 2; j++)
				{
					iIndex = Convert.ToInt32(Math.Truncate((fLocalCompo[j]) / fHistogramResolution));
					iHistogram[iIndex, j]++;
					iHistogram[1001, j]++;
				}
			}

			Console.WriteLine("Calculate Atom composition : OK  /   Nb filtered atom = {0}", iNbFilteredAtom);


			// Display Composition curves
			// --------------------------------
			float[] xVals = new float[Convert.ToInt32(100 / fHistogramResolution)];
			float[] exp_dist = new float[Convert.ToInt32(100 / fHistogramResolution)];
			float[] rnd_dist = new float[Convert.ToInt32(100 / fHistogramResolution)];

			for (i = 0; i < exp_dist.Length; i++)
			{
				xVals[i] = (float)(i * fHistogramResolution + fHistogramResolution / 2);
				exp_dist[i] = iHistogram[i, 0] * 100f / (float)iHistogram[1001, 0];
				rnd_dist[i] = iHistogram[i, 1] * 100f / (float)iHistogram[1001, 1];
			}

			var freqDistChart = viewBuilder.AddChart2D("Composition Frequency distribution", "Concentration (ion%)", "Relative frequency (%)");
			freqDistChart.AddLine(xVals, exp_dist, Colors.Red, "Experimental");
			freqDistChart.AddLine(xVals, rnd_dist, Colors.Blue, "Randomized");


			// Save data in atom file
			// -------------------------
			Atom.bAtomFileAccess(true, "D:\\Documents\\Desktop\\Travail\\Labo\\APsuite\\Experiment files\\test.ato");


			bFilteringState = true;

			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");

			return;
		}


		// Atom filtering with local composition
		// ------------------------------------------
		if (iCalculationId == 2)
		{
			Console.WriteLine("GPM  -  Atom filtering with local composition");
			Console.WriteLine("Atomic distance = {0} nm    Composition Threshold = {1} %", fGridSize, fCompositionThreshold);
			ExecutionTime.Restart();


			// Init parameters
			// -----------------
			int iStep;
			int iNbFilteredAtom;

			float[,] fAtomComposition = new float[Atom.iMemSize, 4];
			bool[] bEltCalc = new bool[256];
			Array.Clear(bEltCalc, 0, bEltCalc.Length);

			bool[] bEltCompo = new bool[256];
			Array.Clear(bEltCompo, 0, bEltCompo.Length);

			bEltCompo[iSelElementId] = bEltCalc[iSelElementId] = true;

			if (iAllAtomFiltering == 1)
				for (i = 0; i < 256; i++)
					bEltCalc[i] = true;


			// Optimization map
			// -----------------
			Console.WriteLine("Build optimization map ...");
			CMapping OptMap = new CMapping();
			OptMap.bBuildOptimization(fGridSize, 0, Atom, null);
			iStep = Math.Max(1, Convert.ToInt32(fGridSize / OptMap.fNewSampSize[0]));
			if (fAtomicDistance / OptMap.fNewSampSize[0] - (float)iStep >= 0.5)
				iStep++;

			Console.WriteLine("Build optimization map : OK  /  iNbStep = {0}", iStep);


			// Calculate local composition for each atom
			// -------------------------------------------
			Console.WriteLine("Calculate Atom composition ...");

			Parallel.For(0, Atom.iMemSize, ii =>
			{
				int jj;
				int aa, bb, cc, dd;
				int[] iLocalBlocId = new int[3];

				float fX, fY, fZ;

				if (bEltCalc[Atom.bEltId[ii, 0]] == true)
				{
					for (jj = 0; jj < 3; jj++)
						iLocalBlocId[jj] = (int)((Atom.fPos[ii, jj] - OptMap.fSubLimit[jj]) / OptMap.fNewSampSize[jj]);

					for (cc = Math.Max(iLocalBlocId[2] - iStep, 0); cc <= Math.Min(iLocalBlocId[2] + iStep, OptMap.iNbStep[2] - 1); cc++)
					for (bb = Math.Max(iLocalBlocId[1] - iStep, 0); bb <= Math.Min(iLocalBlocId[1] + iStep, OptMap.iNbStep[1] - 1); bb++)
					for (aa = Math.Max(iLocalBlocId[0] - iStep, 0); aa <= Math.Min(iLocalBlocId[0] + iStep, OptMap.iNbStep[0] - 1); aa++)
					for (jj = 0; jj < OptMap.iNbAtom[aa, bb, cc]; jj++)
					{
						dd = OptMap.iAtomId[aa, bb, cc, jj];

						fX = Atom.fPos[dd, 0] - Atom.fPos[ii, 0];
						fY = Atom.fPos[dd, 1] - Atom.fPos[ii, 1];
						fZ = Atom.fPos[dd, 2] - Atom.fPos[ii, 2];

						if (fX * fX + fY * fY + fZ * fZ <= fGridSize * fGridSize)
						{
							if (Atom.bEltId[dd, 0] < Atom.iNbElt)
							{
								if (bEltCompo[Atom.bEltId[dd, 0]] == true)
									fAtomComposition[ii, 0]++;
								fAtomComposition[ii, 1]++;
							}

							if (Atom.bEltId[dd, 1] < Atom.iNbElt)
							{
								if (bEltCompo[Atom.bEltId[dd, 1]] == true)
									fAtomComposition[ii, 2]++;
								fAtomComposition[ii, 3]++;
							}
						}
					}
				}

			});



			iNbFilteredAtom = 0;

			for (i = 0; i < Atom.iMemSize; i++)
			{
				fAtomComposition[i, 0] = fAtomComposition[i, 0] * 100 / Math.Max(fAtomComposition[i, 1], 1);
				fAtomComposition[i, 2] = fAtomComposition[i, 2] * 100 / Math.Max(fAtomComposition[i, 3], 1);

				// Atom filtering
				Atom.iCluId[i] = 0;
				if (fAtomComposition[i, 0] >= fCompositionThreshold)
				{
					Atom.iCluId[i] = 1;
					iNbFilteredAtom++;
				}

				// Composition histogram
				for (j = 0; j < 2; j++)
				{
					iIndex = Convert.ToInt32(Math.Truncate((fAtomComposition[i, 2*j]) / fHistogramResolution));
					iHistogram[iIndex, j]++;
					iHistogram[1001, j]++;
				}
			}

			Console.WriteLine("Calculate Atom composition : OK  /   Nb filtered atom = {0}", iNbFilteredAtom);


			// Display Composition histogramm
			// --------------------------------
			float[] xVals = new float[Convert.ToInt32(100 / fHistogramResolution)];
			float[] exp_dist = new float[Convert.ToInt32(100 / fHistogramResolution)];
			float[] rnd_dist = new float[Convert.ToInt32(100 / fHistogramResolution)];

			for (i = 0; i < exp_dist.Length; i++)
			{
				xVals[i] = (float)(i * fHistogramResolution + fHistogramResolution / 2);
				exp_dist[i] = iHistogram[i, 0] * 100f / (float)iHistogram[1001, 0];
				rnd_dist[i] = iHistogram[i, 1] * 100f / (float)iHistogram[1001, 1];
			}

			var freqDistChart = viewBuilder.AddChart2D("Composition Frequency distribution", "Concentration (ion%)", "Relative frequency (%)");
			freqDistChart.AddLine(xVals, exp_dist, Colors.Red, "Experimental");
			freqDistChart.AddLine(xVals, rnd_dist, Colors.Blue, "Randomized");


			// Save data in atom file
			// -------------------------
			Atom.bAtomFileAccess(true, "D:\\Documents\\Desktop\\Travail\\Labo\\APsuite\\Experiment files\\test.ato");


			bFilteringState = true;

			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");

			return;
		}
			
			
		// Cluster identification with distance
		// -----------------------------------------
		if (iCalculationId == 11)
		{
			if (bFilteringState == false)
			{
				Console.WriteLine("   !! Apply Filtering before any Clustering method !!  ");
				return;
			}

			Console.WriteLine("GPM  -  Atom clustering with distance");
			Console.WriteLine("Atomic distance = {0} nm    Min NbAtom / cluster = {1}", fAtomicDistance, iMinNbAtomPerCluster);
			ExecutionTime.Restart();


			// Init parameters
			// -----------------
			CMapping OptMap = new CMapping();

			int a, b, c, d;
			int iStep;
			int iLocalNbCluster , iMinCluId;

			int[] iCluIdTable = new int[iNbParticuleMax + 2];
			int[] iNbAtomCluster = new int[iNbParticuleMax + 2];
			bool[] bClusterIsFound = new bool[iNbParticuleMax + 2];
			int[] iNewCluId = new int[iNbParticuleMax + 2];
			int[] iBlocId = new int[3];

			float fDistance;


			// Add other atoms to solute atoms (user option)
			// ------------------------------------------------
			if (iAllAtomClustering == 1)
			{
				Console.WriteLine("Option - Add other atoms to solute atoms ...");

				OptMap.bBuildOptimization(fAtomicDistance, 3, Atom, null);
				iStep = Math.Max(1, Convert.ToInt32(fAtomicDistance / OptMap.fNewSampSize[0]));
				if (fAtomicDistance / OptMap.fNewSampSize[0] - (float)iStep >= 0.5)
					iStep++;

				Parallel.For(0, Atom.iMemSize, ii =>
				{
					int jj;
					int aa, bb, cc, dd;
					int[] iLocalBlocId = new int[3];

					float fX, fY, fZ;

					for (jj = 0; jj < 3; jj++)
						iLocalBlocId[jj] = (int)((Atom.fPos[ii, jj] - OptMap.fSubLimit[jj]) / OptMap.fNewSampSize[jj]);

					for (cc = Math.Max(iLocalBlocId[2] - iStep, 0); cc <= Math.Min(iLocalBlocId[2] + iStep, OptMap.iNbStep[2] - 1); cc++)
					for (bb = Math.Max(iLocalBlocId[1] - iStep, 0); bb <= Math.Min(iLocalBlocId[1] + iStep, OptMap.iNbStep[1] - 1); bb++)
					for (aa = Math.Max(iLocalBlocId[0] - iStep, 0); aa <= Math.Min(iLocalBlocId[0] + iStep, OptMap.iNbStep[0] - 1); aa++)
					for (jj = 0; jj < OptMap.iNbAtom[aa, bb, cc]; jj++)
					{
						dd = OptMap.iAtomId[aa, bb, cc, jj];

						fX = Atom.fPos[dd, 0] - Atom.fPos[ii, 0];
						fY = Atom.fPos[dd, 1] - Atom.fPos[ii, 1];
						fZ = Atom.fPos[dd, 2] - Atom.fPos[ii, 2];

						if (fX * fX + fY * fY + fZ * fZ <= fAtomicDistance * fAtomicDistance)
						{
							Atom.iCluId[ii] = 1;
							break;
						}
					}
				});
				Console.WriteLine("Option - Add other atoms to solute atoms : OK");
			}


			// Optimization map
			// -----------------
			Console.WriteLine("Build optimization map ...");				
			OptMap.bBuildOptimization(fAtomicDistance, 3, Atom, null);
			iStep = Math.Max(1, Convert.ToInt32(fAtomicDistance / OptMap.fNewSampSize[0]));
			if (fAtomicDistance / OptMap.fNewSampSize[0] - (float)iStep >= 0.5)
				iStep++;

			Console.WriteLine("Build optimization map : OK  /  iNbStep = {0}", iStep);


			// Init memory
			// ---------------
			int[] iLocalCluId = new int[Atom.iMemSize];
			for (i = 0; i < Atom.iMemSize; i++)
			{
				iLocalCluId[i] = Atom.iCluId[i];
				Atom.iCluId[i] = 0;
			}


			// Step 1 : Global cluster identification
			// ----------------------------------------
			Console.WriteLine("Identification - Step1 ...");

			iLocalNbCluster = 0;

			for (i = 0; i < Atom.iMemSize; i++)
				if (iLocalCluId[i] > 0 && Atom.iCluId[i] == 0)
				{
					for (j = 0; j < 3; j++)
						iBlocId[j] = (int)((Atom.fPos[i, j] - OptMap.fSubLimit[j]) / OptMap.fNewSampSize[j]);

					for (c = Math.Max(iBlocId[2] - iStep, 0); c <= Math.Min(iBlocId[2] + iStep, OptMap.iNbStep[2] - 1); c++)
					for (b = Math.Max(iBlocId[1] - iStep, 0); b <= Math.Min(iBlocId[1] + iStep, OptMap.iNbStep[1] - 1); b++)
					for (a = Math.Max(iBlocId[0] - iStep, 0); a <= Math.Min(iBlocId[0] + iStep, OptMap.iNbStep[0] - 1); a++)
					{
						for (j = 0; j < OptMap.iNbAtom[a, b, c]; j++)
						{
							d = OptMap.iAtomId[a, b, c, j];

							fDistance = 0;
							for (int k = 0; k < 3; k++)
								fDistance += (Atom.fPos[i, k] - Atom.fPos[d, k]) * (Atom.fPos[i, k] - Atom.fPos[d, k]);

							if (fDistance <= fAtomicDistance * fAtomicDistance)
							{
								if (Atom.iCluId[i] > 0 && Atom.iCluId[d] > 0)
								{
									iMinCluId = Math.Min(Atom.iCluId[i], iCluIdTable[Atom.iCluId[i]]);
									iMinCluId = Math.Min(Atom.iCluId[d], iMinCluId);
									iMinCluId = Math.Min(iCluIdTable[Atom.iCluId[d]], iMinCluId);

									Atom.iCluId[i] = iCluIdTable[Atom.iCluId[i]] = iMinCluId;
									Atom.iCluId[d] = iCluIdTable[Atom.iCluId[d]] = iMinCluId;
								}

								else if (Atom.iCluId[i] > 0 && Atom.iCluId[d] == 0)
								{
									Atom.iCluId[d] = Math.Min(Atom.iCluId[i], iCluIdTable[Atom.iCluId[i]]);
								}

								else if (Atom.iCluId[i] == 0 && Atom.iCluId[d] > 0)
								{
									Atom.iCluId[i] = Math.Min(Atom.iCluId[d], iCluIdTable[Atom.iCluId[d]]);
								}

								else
								{
									iLocalNbCluster++;
									if (iLocalNbCluster > iNbParticuleMax)
									{
										Console.WriteLine("!! The number of clusters is > {0}", iNbParticuleMax);
										return;
									}

									Atom.iCluId[i] = Atom.iCluId[d] = iLocalNbCluster;
									iCluIdTable[iLocalNbCluster] = iLocalNbCluster;
								}
							}
						}
					}
				}


			// Step 2 : Selective Cluster identification
			// --------------------------------------------

			Console.WriteLine("Identification - Step2 ... ");

			bShowElt = new bool[256];
			Array.Clear(bShowElt, 0, bShowElt.Length);
			bShowElt[3] = true;

			for (i = 0; i < Atom.iMemSize; i++)
				if (iLocalCluId[i] > 0)
					if (Atom.bEltId[i, 0] < Atom.iNbElt && bShowElt[Atom.bEltId[i, 0]] == true)
						iNbAtomCluster[iCluIdTable[Atom.iCluId[i]]]++;

			int m = 0;
			for (i = 1; i <= iLocalNbCluster; i++)
				if (iNbAtomCluster[iCluIdTable[i]] >= iMinNbAtomPerCluster)
				{
					if (bClusterIsFound[iCluIdTable[i]] == false)
					{
						m++;
						iNewCluId[iCluIdTable[i]] = m;
					}

					else
						iNewCluId[i] = iNewCluId[iCluIdTable[i]];

					bClusterIsFound[i] = true;
				}


			// Update the number of cluster
			Atom.iNbCluster = iLocalNbCluster = m;


			// Update the cluster Id in Atom memory
			for (i = 0; i < Atom.iMemSize; i++)
				if (iLocalCluId[i] > 0)
				{
					j = Atom.iCluId[i];

					if (bClusterIsFound[j] == true)
						Atom.iCluId[i] = iNewCluId[j];
					else
						Atom.iCluId[i] = 0;
				}

			Console.WriteLine("Cluster Identification : Nb cluster = {0}", iLocalNbCluster);


			// Display cluster information
			// -------------------------------
			int[,] iNbAtomElt = new int[Atom.iNbCluster + 2, 256];

			for (i = 0; i < Atom.iMemSize; i++)
			{
				iNbAtomElt[Atom.iCluId[i], Atom.bEltId[i, 0]]++;
				if (Atom.bEltId[i, 0]<Atom.iNbElt)
					iNbAtomElt[Atom.iCluId[i], Atom.iNbElt]++;
			}

			int[] iVal = new int[10];
			float[] fVal = new float[10];


			List<ClusterTableForDisplay> tableContent = new List<ClusterTableForDisplay>();

			for (i = 0; i <= iLocalNbCluster; i++)
			{
				for (j = 0; j < Atom.iNbElt; j++)
				{
					iVal[j] = iNbAtomElt[i, j];
					fVal[j] = 100 * (float)iNbAtomElt[i, j]/(float)iNbAtomElt[i, Atom.iNbElt];
				}

				tableContent.Add(new ClusterTableForDisplay(i, iNbAtomElt[i, Atom.iNbElt], iNbAtomElt[i, 255], iVal, fVal));

				Console.WriteLine("Test : i = {0}    NbAtom0 = {1}", i, iNbAtomElt[i, 0]);


			}

			viewBuilder.AddTable("Cluster information", tableContent);


			// Save data in atom file
			// -------------------------
			Atom.bAtomFileAccess(true, "D:\\Documents\\Desktop\\Travail\\Labo\\APsuite\\Experiment files\\test.ato");


			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");
		}
	}



	// Functions to extract data
	private void iterator_mass(float m)
	{
		mass.Add(m);
	}

	private void iterator_pos(float x, float y, float z)
	{
		pos_init.Add(new Vector3(x, y, z));
	}

	private void iterator_name(byte nom)
	{
		name.Add(nom);
	}

	// Function Console
	public class ConsoleHelper
	{
		[DllImport("kernel32.dll")]
		public static extern IntPtr GetStdHandle(int nStdHandle);

		/// <summary>
		/// Allocates a new console for current process.
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern bool AllocConsole();

		/// <summary>
		/// Frees the console.
		/// </summary>
		[DllImport("kernel32.dll")]
		public static extern bool FreeConsole();

		public const int STD_OUTPUT_HANDLE = -11;
		public const int MY_CODE_PAGE = 437;
	}

	// Function Random
	static Random random = new Random();
	public static double GetRandomNumber(int minimum, int maximum)
	{
		return random.NextDouble() * (maximum - minimum) + minimum;
	}

	// Console properties
	public void vPrepareConsole()
	{
		IntPtr stdHandle = ConsoleHelper.GetStdHandle(ConsoleHelper.STD_OUTPUT_HANDLE);
		SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
		FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
		Encoding encoding = System.Text.Encoding.GetEncoding(ConsoleHelper.MY_CODE_PAGE);
		StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
		standardOutput.AutoFlush = true;
		Console.SetOut(standardOutput);
	}

	// Grid structure
	public struct Grid_st
	{
		public double N_int;
		public double N_com;
	}

	public struct X_gridst
	{
		public double Exp;
		public double Rnd;
	}

	// Return the greater value of two numbers 
	public static int GetMax(int first, int second)
	{
		if (first > second)
		{
			return first;
		}
		else
		{
			return second;
		}
	}

	// Return the lower value of two numbers 
	public static int GetMin(int first, int second)
	{
		if (first > second)
		{
			return second;
		}
		else
		{
			return first;
		}
	}





}
