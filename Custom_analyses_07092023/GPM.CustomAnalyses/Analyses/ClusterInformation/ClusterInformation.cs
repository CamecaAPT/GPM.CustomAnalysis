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
using GPM.CustomAnalyses.Analyses.Clustering;
using Microsoft.Win32.SafeHandles;
using Prism.Mvvm;
//using System.Windows.Forms;  
// Vector3
// BindableBase
/*
#region assembly Cameca.Chart, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
//C:\Program Files\CAMECA Instruments\AP Suite\Cameca.Chart.dll
#endregion


using System.Windows.Media;
*/
using Colors = System.Windows.Media.Colors;




namespace GPM.CustomAnalyses.Analyses.ClusterInformation;

internal class GPM_ClusterInformation : ICustomAnalysis<GPM_ClusterInformationOptions>
{
	public IIonDisplayInfo IonDisplayInfo { get; set; } = RandomColorIonDisplayInfo.Instance;

	// Parameters 
	// -------------
	private int iCalculationId = 0;             // Calculation Id
	private int iSelectedClusterId = 1;         // Selected cluster (-1 for all)
	private int iMinNbAtomPerCluster = 5;       // Min nb atom per cluster
	private float fClassSize = 0.1f;            // Class size for profiles (nm)
	private float fDistanceMax = 2.0f;          // Maximum distance for profiles (nm)



	// Variable declaration
	// ----------------------
	List<float> mass = new List<float>();
	List<Vector3> pos_init = new List<Vector3>();
	List<byte> name = new List<byte>();
	List<Color> EltColor;

	int Name_Id;
	private Vector3[] exp_dist;
	private Vector3[] rnd_dist;

	CMapping Map3d = new CMapping();

	CAtom Atom = CustomAnalysesModule.Atom;

	bool bFilteringState = false;

	int iNbParticuleMax = 50000;


	Stopwatch ExecutionTime = new Stopwatch();



	public void Run(IIonData ionData, GPM_ClusterInformationOptions options, IViewBuilder viewBuilder)
	{

		// Conversion US-FR
		// ---------------------
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");


		// Local variables
		// -----------------
		List<string> impact_name;

		bool[] bShowElt;
		bool[] bShowCluster;

		int i, j, k;
		int iTest, iNbAtom;
		int iNbElt, iIndex;

		float fHistogramResolution = (float)0.1;
		int[,] iHistogram = new int[Convert.ToInt32(102 / fHistogramResolution), 2];


		// Menu parameters
		// -----------------
		iCalculationId = options.CalculationId;
		fClassSize = options.ClassSize;
		iMinNbAtomPerCluster = options.MinNbAtomPerCluster;
		fDistanceMax = options.DistanceMax;
		iSelectedClusterId = options.SelectedClusterId;


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
		if (iCalculationId == 0)
		{
			Console.WriteLine("Select a Calculation Id to compute !!");
			return;
		}


		// Data extraction for CAMECA
		// ----------------------------
		/*		foreach (var chunk in ionData.CreateSectionDataEnumerable(IonDataSectionName.IonType))
				{
					var ionTypes = chunk.ReadSectionData<byte>(IonDataSectionName.IonType);
					for (int index = 0; index < chunk.Length; index++)
					{
						name.Add(ionTypes.Span[index]);
					}
				}


				impact_name = ionData.Ions.Select(o => o.Name).ToList();
				EltColor = ionData.Ions.Select(x => IonDisplayInfo.GetColor(x.Formula)).ToList();

		*/

		// Cluster composition - parameters table
		// -----------------------------------------
		if (iCalculationId == 1)
		{
			Console.WriteLine("GPM  -  Cluster composition - parameters table");
			Console.WriteLine("Nb cluster = {0}", Atom.iNbCluster);
			ExecutionTime.Restart();

			int iLocalNbCluster, iBlocId, iElementId;

			iLocalNbCluster = Atom.iNbCluster + 2;
			int[,] iNbAtomElt = new int[iLocalNbCluster, 256];
			float[,] fVolumeCenter = new float[iLocalNbCluster, 4];
			float[,,] fLgValue = new float[iLocalNbCluster, Atom.iNbElt + 2, 4];
			float[] fDstValue = new float[4];


			// Cluster : Nb atom per elt - Center
			// ----------------------------
			for (i = 0; i < Atom.iMemSize; i++)
			{
				iBlocId = Atom.iCluId[i];

				iNbAtomElt[iBlocId, Atom.bEltId[i, 0]]++;
				if (Atom.bEltId[i, 0] < Atom.iNbElt)
					iNbAtomElt[iBlocId, Atom.iNbElt]++;

				for (j = 0; j < 3; j++)
					fVolumeCenter[iBlocId, j] += Atom.fPos[i, j];
				fVolumeCenter[iBlocId, 3]++;
			}

			for (i = 0; i <= Atom.iNbCluster; i++)
			for (j = 0; j < 3; j++)
				fVolumeCenter[i, j] /= (float)Math.Max(fVolumeCenter[i, 3], 1);


			// Cluster information
			// -----------------------
			for (i = 0; i < Atom.iMemSize; i++)
				if (Atom.bEltId[i, 0] < Atom.iNbElt)
				{
					iElementId = Atom.bEltId[i, 0];
					iBlocId = Atom.iCluId[i];

					for (j = 0, fDstValue[3] = 0; j < 3; j++)
					{
						fDstValue[j] = Atom.fPos[i, j] - fVolumeCenter[iBlocId, j];
						fDstValue[j] *= fDstValue[j];
						fDstValue[3] += fDstValue[j];
					}

					for (j = 0; j < 4; j++)
					{
						fDstValue[j] = (float)Math.Sqrt(fDstValue[j]);
						fLgValue[iBlocId, iElementId, j] += fDstValue[j];
						fLgValue[iBlocId, Atom.iNbElt, j] += fDstValue[j];
					}
				}


			for (i = 0; i <= Atom.iNbCluster; i++)
			for (k = 0; k <= Atom.iNbElt; k++)
			for (j = 0; j < 4; j++)
				fLgValue[i, k, j] /= (float)Math.Max(iNbAtomElt[i, k], 1);


			// Display cluster information
			// -------------------------------
			int[] iVal = new int[10];
			float[] fVal1 = new float[10];
			float[] fVal2 = new float[10];

			List<ClusterTableForDisplay2> tableContent = new List<ClusterTableForDisplay2>();

			for (i = 0; i <= Atom.iNbCluster; i++)
			{
				for (j = 0; j < Atom.iNbElt; j++)
				{
					iVal[j] = iNbAtomElt[i, j];
					fVal1[j] = 100 * (float)iNbAtomElt[i, j] / (float)iNbAtomElt[i, Atom.iNbElt];
				}

				for (j = 0; j < 4; j++)
					fVal2[j] = fLgValue[i, Atom.iNbElt, j];

				fVal2[4] = (float)Math.Sqrt(5.0 / 3.0) * fLgValue[i, Atom.iNbElt, 3];

				for (j = 5; j < 8; j++)
					fVal2[j] = fVolumeCenter[i, j - 5];

				tableContent.Add(new ClusterTableForDisplay2(i, iNbAtomElt[i, Atom.iNbElt], iNbAtomElt[i, 255], iVal, fVal1, fVal2));
			}

			viewBuilder.AddTable("Cluster information", tableContent);


			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");


			return;
		}


		// Cluster histogram
		// -----------------------------------------
		if (iCalculationId == 2)
		{
			Console.WriteLine("GPM  -  Cluster histogram");
			Console.WriteLine("Nb cluster = {0}", Atom.iNbCluster);
			ExecutionTime.Restart();

			int iLocalNbCluster, iBlocId, iElementId;

			iLocalNbCluster = Atom.iNbCluster + 2;
			int[,] iNbAtomElt = new int[iLocalNbCluster, 256];


			// Cluster : Nb atom per cluster
			// --------------------------------
			for (i = 0; i < Atom.iMemSize; i++)
			{
				iBlocId = Atom.iCluId[i];

				iNbAtomElt[iBlocId, Atom.bEltId[i, 0]]++;
				if (Atom.bEltId[i, 0] < Atom.iNbElt)
					iNbAtomElt[iBlocId, Atom.iNbElt]++;
			}


			// Display Cluster histogramm
			// --------------------------------
			fHistogramResolution = 0.01f;
			float[] xVals = new float[Convert.ToInt32(iLocalNbCluster / fHistogramResolution)];
			float[] exp_dist = new float[Convert.ToInt32(iLocalNbCluster / fHistogramResolution)];

			for (i = 0; i < iLocalNbCluster / fHistogramResolution; i++)
				xVals[i] = (float)i * fHistogramResolution;

			var freqDistChart = viewBuilder.AddChart2D("Distribution of the number of atoms / cluster", "Cluster Id", "Number of atoms");

			// Applies color under histogram
			List<IChart2DSlice> slices;
			for (i = 0; i < Atom.iNbElt; i++)
			{
				Array.Clear(exp_dist, 0, exp_dist.Length);

				for (j = 1; j <= Atom.iNbCluster; j++)
				{
					iBlocId = (int)(j / fHistogramResolution) - 1 + 4 * (i + 1);
					exp_dist[iBlocId] = exp_dist[iBlocId + 1] = exp_dist[iBlocId + 2] = (float)iNbAtomElt[j, i];
				}

				slices = new List<IChart2DSlice> { new Chart2DSlice(xVals[0], xVals[^1], Atom.EltColor[i])};
				freqDistChart.AddHistogram(xVals, exp_dist, Atom.EltColor[i], slices, Atom.EltName[i], 1);
			}

			Array.Clear(exp_dist, 0, exp_dist.Length);
			for (j = 1; j <= Atom.iNbCluster; j++)
			{
				iBlocId = (int)(j / fHistogramResolution) - 1;
				exp_dist[iBlocId] = exp_dist[iBlocId + 1] = exp_dist[iBlocId + 2] = (float)iNbAtomElt[j, Atom.iNbElt];
			}
			slices = new List<IChart2DSlice> { new Chart2DSlice(xVals[0], xVals[^1], Colors.Black) };
			freqDistChart.AddHistogram(xVals, exp_dist, Colors.Black, slices, "All", 1);


			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");


			return;
		}


		// Cluster size ordering
		// ------------------------
		if (iCalculationId == 3)
		{
			Console.WriteLine("GPM  -  Cluster size ordering");
			Console.WriteLine("Nb cluster = {0}    Min Nb atom / cluster = {1}", Atom.iNbCluster, iMinNbAtomPerCluster);
			ExecutionTime.Restart();

			int iLocalNbCluster, iBlocId, iNbClusterFound;

			iLocalNbCluster = Atom.iNbCluster + 2;
			int[,] iNbAtomElt = new int[iLocalNbCluster, 256];
			int[] iNbAtomPerCluster = new int[iLocalNbCluster];
			int[] iClusterId = new int[iLocalNbCluster];
			bool[] bState = new bool[iLocalNbCluster];


			// Nb atom per cluster
			// --------------------------------
			for (i = 0; i < Atom.iMemSize; i++)
				iNbAtomPerCluster[Atom.iCluId[i]]++;

			// Sort clusters size
			// --------------------------------
			for (i = 1; i <= Atom.iNbCluster; i++)
				if (iNbAtomPerCluster[i] >= iMinNbAtomPerCluster)
					bState[i] = true;

			iNbClusterFound = 1;

			for (i = 1; i <= Atom.iNbCluster; i++)
			{
				k = 0;

				for (j = 1; j <= Atom.iNbCluster; j++)
					if (bState[j] == true && iNbAtomPerCluster[j] < iNbAtomPerCluster[k])
						k = j;

				if (k > 0)
				{
					iClusterId[k] = iNbClusterFound ;
					bState[k] = false;
					iNbClusterFound++;
				}
			}


			// Update Cluster Id and histogram
			// ----------------------------------
			for (i = 0; i < Atom.iMemSize; i++)
			{
				Atom.iCluId[i] = iClusterId[Atom.iCluId[i]];
				iBlocId = Atom.iCluId[i];

				iNbAtomElt[iBlocId, Atom.bEltId[i, 0]]++;
				if (Atom.bEltId[i, 0] < Atom.iNbElt)
					iNbAtomElt[iBlocId, Atom.iNbElt]++;
			}


			// Display Cluster histogramm
			// --------------------------------
			fHistogramResolution = 0.01f;
			float[] xVals = new float[Convert.ToInt32(iLocalNbCluster / fHistogramResolution)];
			float[] exp_dist = new float[Convert.ToInt32(iLocalNbCluster / fHistogramResolution)];

			for (i = 0; i < iLocalNbCluster / fHistogramResolution; i++)
				xVals[i] = (float)i * fHistogramResolution;

			var freqDistChart = viewBuilder.AddChart2D("Distribution of the number of atoms / cluster", "Cluster Id", "Number of atoms");

			// Applies color under histogram
			List<IChart2DSlice> slices;
			for (i = 0; i < Atom.iNbElt; i++)
			{
				Array.Clear(exp_dist, 0, exp_dist.Length);

				for (j = 1; j <= Atom.iNbCluster; j++)
				{
					iBlocId = (int)(j / fHistogramResolution) - 1 + 4 * (i + 1);
					exp_dist[iBlocId] = exp_dist[iBlocId + 1] = exp_dist[iBlocId + 2] = (float)iNbAtomElt[j, i];
				}

				slices = new List<IChart2DSlice> { new Chart2DSlice(xVals[0], xVals[^1], Atom.EltColor[i]) };
				freqDistChart.AddHistogram(xVals, exp_dist, Atom.EltColor[i], slices, Atom.EltName[i], 1);
			}

			Array.Clear(exp_dist, 0, exp_dist.Length);
			for (j = 1; j <= Atom.iNbCluster; j++)
			{
				iBlocId = (int)(j / fHistogramResolution) - 1;
				exp_dist[iBlocId] = exp_dist[iBlocId + 1] = exp_dist[iBlocId + 2] = (float)iNbAtomElt[j, Atom.iNbElt];
			}
			slices = new List<IChart2DSlice> { new Chart2DSlice(xVals[0], xVals[^1], Colors.Black) };
			freqDistChart.AddHistogram(xVals, exp_dist, Colors.Black, slices, "All", 1);


			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");


			return;
		}
			
			
		// Cluster Radial composition
		// -----------------------------
		if (iCalculationId == 11)
		{
			Console.WriteLine("GPM  -  Cluster Radial composition");
			if ( iSelectedClusterId == 0)
				Console.WriteLine("Nb cluster = {0}    All clusters are selected", Atom.iNbCluster);
			else
				Console.WriteLine("Nb cluster = {0}    Selected cluster = {1}", Atom.iNbCluster, iSelectedClusterId);
			Console.WriteLine("Class size = {0} nm    Maximum distance = {1} nm", fClassSize, fDistanceMax);
			ExecutionTime.Restart();

			int iLocalNbCluster, iBlocId, iElementId;

			iLocalNbCluster = Atom.iNbCluster + 2;
			int[,] iNbAtomElt = new int[iLocalNbCluster, 256];
			float[,] fCluCenter = new float[iLocalNbCluster, 4];
			float[,,] fCluLimit = new float[iLocalNbCluster, 3, 2];
			float[] fDstValue = new float[4];


			// Cluster center and limits
			// ----------------------------
			for (i = 0; i < iLocalNbCluster; i++)
			for (j = 0; j < 3; j++)
			{
				fCluLimit[i, j, 0] = 10000;
				fCluLimit[i, j, 1] = -10000;
			}

			for (i = 0; i < Atom.iMemSize; i++)
				if (Atom.bEltId[i, 0] < Atom.iNbElt)
				{
					iBlocId = Atom.iCluId[i];

					for (j = 0; j < 3; j++)
					{
						fCluLimit[iBlocId, j, 0] = Math.Min(Atom.fPos[i, j], fCluLimit[iBlocId, j, 0]);
						fCluLimit[iBlocId, j, 1] = Math.Max(Atom.fPos[i, j], fCluLimit[iBlocId, j, 1]);
						fCluCenter[iBlocId, j] += Atom.fPos[i, j];
					}

					fCluCenter[iBlocId, 3]++;
				}

			for (k = 0; k < iLocalNbCluster; k++)
			for (j = 0; j < 3; j++)
			{
				fCluLimit[k, j, 0] -= fDistanceMax;
				fCluLimit[k, j, 1] += fDistanceMax;
			}

			for (i = 0; i <= Atom.iNbCluster; i++)
			for (j = 0; j < 3; j++)
				fCluCenter[i, j] /= (float)Math.Max(fCluCenter[i, 3], 1);


			// Composition Profile
			// -----------------------
			int iProfileSize = 2 + (int)(fDistanceMax / fClassSize);
			float[,] fProfile = new float[Atom.iNbElt + 2, iProfileSize];

			for (i = 0; i < Atom.iMemSize; i++)
				if (Atom.bEltId[i, 0] < Atom.iNbElt)
					for (k = 1; k <= Atom.iNbCluster; k++)
						if (iSelectedClusterId == 0 || k == iSelectedClusterId)
							if (Atom.fPos[i, 0] >= fCluLimit[k, 0, 0] && Atom.fPos[i, 0] <= fCluLimit[k, 0, 1])
								if (Atom.fPos[i, 1] >= fCluLimit[k, 1, 0] && Atom.fPos[i, 1] <= fCluLimit[k, 1, 1])
									if (Atom.fPos[i, 2] >= fCluLimit[k, 2, 0] && Atom.fPos[i, 2] <= fCluLimit[k, 2, 1])
									{
										for (j = 0, fDstValue[3] = 0; j < 3; j++)
										{
											fDstValue[j] = Atom.fPos[i, j] - fCluCenter[k, j];
											fDstValue[3] += fDstValue[j] * fDstValue[j];
										}

										fDstValue[3] = (float)Math.Sqrt(fDstValue[3]);

										if (fDstValue[3] < fDistanceMax)
										{
											iIndex = (int)(fDstValue[3] / fClassSize);

											if (iIndex < iProfileSize)
											{
												fProfile[Atom.bEltId[i, 0], iIndex]++;
												fProfile[Atom.iNbElt, iIndex]++;
											}
										}

									}


			// Display Composition profile
			// --------------------------------
			float[] xVals = new float[iProfileSize];

			for (i = 0; i < iProfileSize; i++)
				xVals[i] = (float)(i * fClassSize);

			var compositionProfileChart = viewBuilder.AddChart2D("Radial Composition profile", "Concentration (ion%)", "Relative frequency (%)");

			for (i = 0; i < Atom.iNbElt; i++)
			{
				float[] exp_dist = new float[iProfileSize];
				for (j = 0; j < iProfileSize; j++)
					exp_dist[j] = 100 * fProfile[i, j] / Math.Max(fProfile[Atom.iNbElt, j], 1);

				compositionProfileChart.AddLine(xVals, exp_dist, Atom.EltColor[i], Atom.EltName[i]);
			}


			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");


			return;
		}


		// Cluster Erosion composition
		// -----------------------------
		if (iCalculationId == 12)
		{
			Console.WriteLine("GPM  -  Cluster Erosion composition");
			if (iSelectedClusterId == -1)
				Console.WriteLine("Nb cluster = {0}    All clusters are selected", Atom.iNbCluster);
			else
				Console.WriteLine("Nb cluster = {0}    Selected cluster = {1}", Atom.iNbCluster, iSelectedClusterId);
			Console.WriteLine("Class size = {0} nm    Maximum distance = {1} nm", fClassSize, fDistanceMax);
			ExecutionTime.Restart();

			int iLocalNbCluster, iBlocId, iElementId;
			int iMaxNbAtomId;

			iLocalNbCluster = Atom.iNbCluster + 2;
			int[,] iNbAtomElt = new int[iLocalNbCluster, 256];
			int[,] iNbAtomId = new int[iLocalNbCluster, 2];
			float[,] fCluCenter = new float[iLocalNbCluster, 4];
			float[,,] fCluLimit = new float[iLocalNbCluster, 3, 2];
			float[] fDstValue = new float[4];

			float fMaxDistanceFound;


			// Cluster selection
			// ---------------------
			bShowCluster = new bool[Atom.iNbCluster + 2];

			if (iSelectedClusterId == 0)
				for (i = 1; i <= Atom.iNbCluster; i++)
					bShowCluster[i] = true;
			else
			{
				for (i = 1; i <= Atom.iNbCluster; i++)
					bShowCluster[i] = false;
				bShowCluster[iSelectedClusterId] = true;
			}


			// Cluster centers and limits
			// ----------------------------
			Console.WriteLine("Cluster centers and limits ...");
			for (i = 0; i <= Atom.iNbCluster; i++)
			for (j = 0; j < 3; j++)
			{
				fCluLimit[i, j, 0] = 10000;
				fCluLimit[i, j, 1] = -10000;
			}

			for (i = 0; i < Atom.iMemSize; i++)
			{
				iBlocId = Atom.iCluId[i];

				for (j = 0; j < 3; j++)
				{
					fCluLimit[iBlocId, j, 0] = Math.Min(Atom.fPos[i, j], fCluLimit[iBlocId, j, 0]);
					fCluLimit[iBlocId, j, 1] = Math.Max(Atom.fPos[i, j], fCluLimit[iBlocId, j, 1]);
					fCluCenter[iBlocId, j] += Atom.fPos[i, j];
				}

				fCluCenter[iBlocId, 3]++;
			}

			fMaxDistanceFound = 0;
			for (k = 1; k <= Atom.iNbCluster; k++)
			{
				for (j = 0; j < 3; j++)
				{
					fCluLimit[k, j, 0] = Math.Max(fCluLimit[k, j, 0] - fDistanceMax, Atom.fLimit[j, 0]);
					fCluLimit[k, j, 1] = Math.Min(fCluLimit[k, j, 1] + fDistanceMax, Atom.fLimit[j, 1]);
					fMaxDistanceFound = Math.Max(Math.Abs(fCluLimit[k, j, 1] - fCluLimit[k, j, 0]), fMaxDistanceFound);

//						if ( k==1)
//							Console.WriteLine("fCluLimit[k, j, 0] = {0}    fCluLimit[k, j, 0] = {1}    Diff = {2}", fCluLimit[k, j, 0], fCluLimit[k, j, 1], fCluLimit[k, j, 1]- fCluLimit[k, j, 0]);
				}
			}

			for (i = 0; i <= Atom.iNbCluster; i++)
			for (j = 0; j < 3; j++)
				fCluCenter[i, j] /= (float)Math.Max(fCluCenter[i, 3], 1);

//				Console.WriteLine("fMaxDistanceFound = {0}", fMaxDistanceFound);


			// Prepare memory for cluster and matrix fast calculation
			// --------------------------------------------------------
			Console.WriteLine("Prepare memories for calculation ...");
			iMaxNbAtomId = 0;
			for (i = 0; i < Atom.iMemSize; i++)
			{
				if (Atom.iCluId[i] > 0)
				{
					iNbAtomId[Atom.iCluId[i], 1]++;
					iMaxNbAtomId = Math.Max(iNbAtomId[Atom.iCluId[i], 1], iMaxNbAtomId);
				}

				else
					for (j = 1; j <= Atom.iNbCluster; j++)
						if(bShowCluster[j] == true)
							if (Atom.fPos[i, 0] >= fCluLimit[j, 0, 0] && Atom.fPos[i, 0] <= fCluLimit[j, 0, 1]
							                                          && Atom.fPos[i, 1] >= fCluLimit[j, 1, 0] && Atom.fPos[i, 1] <= fCluLimit[j, 1, 1]
							                                          && Atom.fPos[i, 2] >= fCluLimit[j, 2, 0] && Atom.fPos[i, 2] <= fCluLimit[j, 2, 1])
							{
								iNbAtomId[j, 0]++;
								iMaxNbAtomId = Math.Max(iNbAtomId[j, 0], iMaxNbAtomId);
							}
			}

			int[,] iIndexC = new int[iLocalNbCluster, iMaxNbAtomId + 2];
			int[,] iIndexM = new int[iLocalNbCluster, iMaxNbAtomId + 2];
			float[,] fDstMin = new float[iMaxNbAtomId + 2, 2];
			Array.Clear(iNbAtomId, 0, iNbAtomId.Length);

			for (i = 0; i < Atom.iMemSize; i++)
			{
				if (Atom.iCluId[i] > 0)
				{
					iIndexC[Atom.iCluId[i], iNbAtomId[Atom.iCluId[i], 1]] = i;
					iNbAtomId[Atom.iCluId[i], 1]++;
				}

				else
					for (j = 1; j <= Atom.iNbCluster; j++)
						if (bShowCluster[j] == true)
							if (Atom.fPos[i, 0] >= fCluLimit[j, 0, 0] && Atom.fPos[i, 0] <= fCluLimit[j, 0, 1]
							                                          && Atom.fPos[i, 1] >= fCluLimit[j, 1, 0] && Atom.fPos[i, 1] <= fCluLimit[j, 1, 1]
							                                          && Atom.fPos[i, 2] >= fCluLimit[j, 2, 0] && Atom.fPos[i, 2] <= fCluLimit[j, 2, 1])
							{
								iIndexM[j, iNbAtomId[j, 0]] = i;
								iNbAtomId[j, 0]++;
							}
			}


			int iLocalSize = 2 + (int)(2 * fMaxDistanceFound / fClassSize);
			int[,,] iCountAtom = new int[iLocalSize, Atom.iNbElt + 2, 2];
			int[] iMaxBlocId = new int[2];
			iMaxBlocId[0] = iMaxBlocId[1] = 0;


//				Console.WriteLine("iMaxNbAtomId = {0}", iMaxNbAtomId);


//				for ( i = 1; i < 10; i ++)
//					Console.WriteLine("Cluster {0} : NbClu = {1}    NbMat = {2}    Center = {3}  {4}  {5}", i, iNbAtomId[i, 1], iNbAtomId[i, 0], fCluCenter[i,0], fCluCenter[i, 1], fCluCenter[i, 2]);


			// Calculate Erosion profiles for each cluster
			Console.WriteLine("Calculate Erosion profiles ...");



			for (i = 1; i <= Atom.iNbCluster; i++)
			{
				if (bShowCluster[i] == true)
				{
					for (j = 0; j < iMaxNbAtomId; j++)
						fDstMin[j, 0] = fDstMin[j, 1] = 500000;

					// Calculate matrix and cluster distances
					// ----------------------------------------
					Parallel.For(0, iNbAtomId[i, 1], mm =>
					{
						int nn;
						float[] fLocalParam = new float[10];

						for (nn = 0; nn < iNbAtomId[i, 0]; nn++)
						{
							fLocalParam[0] = Atom.fPos[iIndexM[i, nn], 0] - Atom.fPos[iIndexC[i, mm], 0];
							fLocalParam[3] = fLocalParam[0] * fLocalParam[0];
							if (fLocalParam[3] < fDstMin[mm, 1])
							{
								fLocalParam[1] = Atom.fPos[iIndexM[i, nn], 1] - Atom.fPos[iIndexC[i, mm], 1];
								fLocalParam[3] += fLocalParam[1] * fLocalParam[1];
								if (fLocalParam[3] < fDstMin[mm, 1])
								{
									fLocalParam[2] = Atom.fPos[iIndexM[i, nn], 2] - Atom.fPos[iIndexC[i, mm], 2];
									fLocalParam[3] += fLocalParam[2] * fLocalParam[2];
									fDstMin[mm, 1] = Math.Min(fDstMin[mm, 1], fLocalParam[3]);
								}
							}
						}

						fDstMin[mm, 1] = (float)Math.Sqrt(fDstMin[mm, 1]);
					});


					Parallel.For(0, iNbAtomId[i, 0], mm =>
					{
						int nn;
						float[] fLocalParam = new float[10];

						for (nn = 0; nn < iNbAtomId[i, 1]; nn++)
						{
							fLocalParam[0] = Atom.fPos[iIndexM[i, mm], 0] - Atom.fPos[iIndexC[i, nn], 0];
							fLocalParam[3] = fLocalParam[0] * fLocalParam[0];
							if (fLocalParam[3] < fDstMin[mm, 0])
							{
								fLocalParam[1] = Atom.fPos[iIndexM[i, mm], 1] - Atom.fPos[iIndexC[i, nn], 1];
								fLocalParam[3] += fLocalParam[1] * fLocalParam[1];
								if (fLocalParam[3] < fDstMin[mm, 0])
								{
									fLocalParam[2] = Atom.fPos[iIndexM[i, mm], 2] - Atom.fPos[iIndexC[i, nn], 2];
									fLocalParam[3] += fLocalParam[2] * fLocalParam[2];
									fDstMin[mm, 0] = Math.Min(fDstMin[mm, 0], fLocalParam[3]);
								}
							}
						}

						fDstMin[mm, 0] = (float)Math.Sqrt(fDstMin[mm, 0]);
					});


//						if (i ==1)
//							for ( j = 0; j < 10; j ++)
//								Console.WriteLine("j = {0}     dst1 = {1}    dst2 = {2}", j, fDstMin[j, 0], fDstMin[j, 1]);


					// Calculate composition profile
					// -------------------------------
					for (j = 0; j < iNbAtomId[i, 1]; j++)
						if (fDstMin[j, 1] != 500000)
							if (Atom.bEltId[iIndexC[i, j], 0] < Atom.iNbElt)
							{
								iBlocId = (int)((fDstMin[j, 1]) / fClassSize);
								iMaxBlocId[1] = Math.Max(iMaxBlocId[1], iBlocId);

								iCountAtom[iBlocId, Atom.bEltId[iIndexC[i, j], 0], 1]++;
								iCountAtom[iBlocId, Atom.iNbElt, 1]++;
							}

					for (j = 0; j < iNbAtomId[i, 0]; j++)
						if (fDstMin[j, 0] != 500000)
							if (Atom.bEltId[iIndexM[i, j], 0] < Atom.iNbElt)
							{
								iBlocId = (int)((fDstMin[j, 0]) / fClassSize);
								iMaxBlocId[0] = Math.Max(iMaxBlocId[0], iBlocId);

								iCountAtom[iBlocId, Atom.bEltId[iIndexM[i, j], 0], 0]++;
								iCountAtom[iBlocId, Atom.iNbElt, 0]++;
							}

					Console.WriteLine("Cluster {0} / {1} : Nb atom from cluster = {2}    Nb Atom from matrix = {3}", i, Atom.iNbCluster, iNbAtomId[i, 1], iNbAtomId[i, 0]);

				}

			}

//				Console.WriteLine("iLocalSize = {0}", iLocalSize);

//				for ( i = 0; i < 10; i ++)
			//				Console.WriteLine("i = {0}    iCountAtom = {1}   /   {2}", i, iCountAtom[i, Atom.iNbElt, 0], iCountAtom[i, Atom.iNbElt, 1]);


			// Adjust classes vs nb atom
			// -----------------------------
			Console.WriteLine("Adjust classes vs nb atom");
			int p, iCountAverage;
			int[] iNbClassForDst = new int[2];
			float[] fDeltaDst = new float[2];


			j = k = 0;

			for ( p = 0; p < 2; p++)
			for (i = 0; i <= iMaxBlocId[p]; i++)
				if (iCountAtom[i, Atom.iNbElt, p] > 0)
				{
					j += iCountAtom[i, Atom.iNbElt, p];
					k++;
				}

			iCountAverage = j / k;


			for (p = 0; p < 2; p++)
			{
				iNbClassForDst[p] = 1;
				fDeltaDst[p] = fClassSize;

				for (i = 0; i <= iMaxBlocId[p] / 2; i++)
				{
					if (iCountAtom[i, Atom.iNbElt, p] < iCountAverage / 2)
					{
						fDeltaDst[p] += (float)(i + 2) * fClassSize;
						iNbClassForDst[p]++;

						for (j = 0; j < Atom.iNbElt + 1; j++)
						{
							iCountAtom[i+1, j, p] += iCountAtom[i, j, p];
							iCountAtom[i, j, p] = 0;
						}
					}
					else
						break;
				}
			}


			// Build the profile
			// ----------------------
			Console.WriteLine("Build the profile");
				
			int iProfileSize, iMiddleBloc, iNbBloc;

			iProfileSize = iMiddleBloc = 0;

			for (i = 0; i <= iMaxBlocId[1]; i++)
				if (iCountAtom[i, Atom.iNbElt, 1] > 0)
					iProfileSize++;

			iMiddleBloc = iProfileSize;

			for (i = 0; i <= iMaxBlocId[0]; i++)
				if (iCountAtom[i, Atom.iNbElt, 0] > 0)
					iProfileSize++;

			iProfileSize += 2;
			float[,] fProfile = new float[iProfileSize, Atom.iNbElt + 2 ];

			iNbBloc = 0;
				
			for (i = 0, k = iMiddleBloc - 1; i <= iMaxBlocId[1] && k >= 0; i++)
				if (iCountAtom[i, Atom.iNbElt, 1] > 0)
				{
					if (k == iMiddleBloc - 1)
						fProfile[k, Atom.iNbElt + 1] = -fDeltaDst[0] / (float)iNbClassForDst[0];
					else
						fProfile[k, Atom.iNbElt + 1] = -(float)(i) * fClassSize;

					for (j = 0; j <= Atom.iNbElt; j++)
						fProfile[k, j] = (float)iCountAtom[i, j, 1];

					k--;
					iNbBloc++;
				}


			for (i = 0, k = iMiddleBloc; i <= iMaxBlocId[0] && k < iProfileSize; i++)
				if (iCountAtom[i, Atom.iNbElt, 0] > 0)
				{
					fProfile[k, Atom.iNbElt + 1] = (float)(k - iMiddleBloc + 1) * fClassSize;
					for (j = 0; j <= Atom.iNbElt; j++)
						fProfile[k, j] = (float)iCountAtom[i, j, 0];

					k++;
					iNbBloc++;
				}


			// Display Composition profile
			// --------------------------------
			iProfileSize = iNbBloc;

			float[] xVals = new float[iProfileSize];

			for (j = 0; j < iProfileSize; j++)
				xVals[j] = fProfile[j, Atom.iNbElt + 1];

			var erosionProfileChart = viewBuilder.AddChart2D("Erosion Composition profile", "Atomic distance (nm)", "Composition (%)");

			for (i = 0; i < Atom.iNbElt; i++)
			{
				float[] exp_dist = new float[iProfileSize];
				for (j = 0; j < iProfileSize; j++)
				{
					if (fProfile[j, Atom.iNbElt] > 0)
						exp_dist[j] = 100 * fProfile[j, i] / Math.Max(fProfile[j, Atom.iNbElt], 1);
					else if ( j > 0 )
						exp_dist[j] = exp_dist[j - 1];
				}

				erosionProfileChart.AddLine(xVals, exp_dist, Atom.EltColor[i], Atom.EltName[i]);
			}


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



	internal class Chart2DSlice : BindableBase, IChart2DSlice
	{
		public Chart2DSlice(float min, float max, Color color)
		{
			Min = min;
			Max = max;
			Color = color;
		}

		public float Min { get; }
		public float Max { get; }
		public Color Color { get; set; }
		public bool IsSelected { get; set; } = false;
	}

}
