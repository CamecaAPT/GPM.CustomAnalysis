using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Cameca.Extensions.Controls;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using LiveCharts.Wpf.Charts.Base;
using Prism.Commands;
using Color = System.Windows.Media.Color;

namespace GPM.CustomAnalyses.Analyses.ClusteringM;

internal class ClusteringMViewModel : AnalysisViewModelBase<ClusteringMNode>
{	
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.ClusteringM.ClusteringMViewModel";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IRenderDataFactory _renderDataFactory;

	private IIonDisplayInfo? _ionDisplayInfo = null;

	public SeriesCollection DataSeries { get; } = new();

	public DataTable tableCompo { get; set; }
	public ObservableCollection<IRenderData> ExampleChartData { get; } = new();
	public ObservableCollection<CheckBoxItem> CheckBoxItemsElt { get; } = new();
	public ObservableCollection<ClusterTableForDisplay> tableContent { get; } = new();
	public ICommand AtomFilteringCommand { get; }
	public ICommand AtomClusteringCommand { get; }
	public ICommand LoadAtomMemoryCommand { get; }
	public ICommand UpdateEltCommand { get; }


	public AsyncRelayCommand UpdateCommand { get; }

	// Parameters 
	// -------------
	public string SelElementId { get; set; } = "0";
	public bool AllAtomFiltering { get; set; } = false;
	public string GridSize { get; set; } = "1.0";                // Grid size in nm
	public string GridDelocalization { get; set; } = "0.5";      // Delocalization in nm
	public string CompositionThreshold { get; set; } = "10";      // Concentration threshold : Min
	public string AtomicDistance { get; set; } = "0.5";          // Concentration distribution visualization : bin size
	public string MinNbAtomPerCluster { get; set; } = "5";          // Number of Iteration for the progress "bar"
	public bool AllAtomClustering { get; set; } = false;
	public int SelectedFilteringId { get; set; } = 0;
	public int SelectedClusteringId { get; set; } = 0;
	public DataTable dataTable { get; set; } = new DataTable();

	// Variable declaration
	// ----------------------
	List<float> mass = new List<float>();
	List<Vector3> pos_init = new List<Vector3>();
	List<byte> name = new List<byte>();
	int Name_Id;
	private Vector3[] exp_dist;
	private Vector3[] rnd_dist;

	private int iSelElementId;
	private float fGridSize;               // Grid size in nm
	private float fGridDelocalization;      // Delocalization in nm
	private float fCompositionThreshold;      // Concentration threshold : Min
	private float fAtomicDistance;          // Concentration distribution visualization : bin size
	private int iMinNbAtomPerCluster;          // Number of Iteration for the progress "bar"
	private bool[] bEltSelected = new bool[256];
	private bool isCheckBoxLoad = false;


	CMapping Map3d = new CMapping();

	CAtom Atom = CustomAnalysesModule.Atom;

	bool bFilteringState = false;

	int iNbParticuleMax = 50000;

	Stopwatch ExecutionTime = new Stopwatch();

	private bool _displayUpdateOverlay;
	public bool DisplayUpdateOverlay
	{
		get => _displayUpdateOverlay;
		set => SetProperty(ref _displayUpdateOverlay, value);
	}

	public ClusteringMViewModel(
		IAnalysisViewModelBaseServices services,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
		IRenderDataFactory renderDataFactory)
		: base(services)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
		_renderDataFactory = renderDataFactory;

		AtomFilteringCommand = new DelegateCommand(AtomFiltering);
		AtomClusteringCommand = new DelegateCommand(AtomClustering);
		LoadAtomMemoryCommand = new DelegateCommand(LoadAtomMemory);
		UpdateEltCommand = new DelegateCommand(UpdateElt);

		Array.Fill(bEltSelected, false);
		CreateTableColumns();

		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

		AddCheckBox();
	}

	public async void LoadAtomMemory()
	{
		Task<IIonData> IonDataTask = Node.GetIonData1();
		IIonData IonDataMemory = await IonDataTask;
		IIonDisplayInfo IonDisplayInfoMemory = Node.GetIonDisplayInfo();
		Console.WriteLine("Create Atom data memory ...");
		Atom.bInitMemory2(IonDataMemory, IonDisplayInfoMemory);
		
		Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

		AddCheckBox();
	}

	public async void UpdateElt()
	{

		Task<IIonData> IonDataTask = Node.GetIonData1();
		IIonData IonDataMemory = await IonDataTask;
		IIonDisplayInfo IonDisplayInfoMemory = Node.GetIonDisplayInfo();
		Console.WriteLine("Update Eltement ...");
		Atom.bUpdateElt(IonDataMemory, IonDisplayInfoMemory);

		Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

		AddCheckBox();
	}

	private void AddCheckBox()
	{
		CheckBoxItemsElt.Clear();
		for (int i = 0; i <= Atom.iNbElt; i++)
		{
			string name = (i == Atom.iNbElt ? "Noise" : Atom.EltName[i]);
			Color color = (i == Atom.iNbElt ? Colors.Gray : Atom.EltColor[i]);
			bool isRep = false;
			CheckBoxItemsElt.Add(new CheckBoxItem(name, color, isRep, "Element"));
		}
		isCheckBoxLoad = true;
	}

	private void InitMenuParameters()
	{

		for(int i =0; i<=Atom.iNbElt; i++)
		{
			if (bEltSelected[i] != CheckBoxItemsElt[i].IsSelected)
			{
				bEltSelected[i] = CheckBoxItemsElt[i].IsSelected;
				Map3d.bState = false;
			}
		}

		if (fGridSize != float.Parse(GridSize))
		{
			fGridSize = float.Parse(GridSize);
			Map3d.bState = false;
		}

		if (fGridDelocalization != float.Parse(GridDelocalization))
		{
			fGridDelocalization = float.Parse(GridDelocalization);
			Map3d.bState = false;
		}

		fCompositionThreshold = float.Parse(CompositionThreshold);
		fAtomicDistance = float.Parse(AtomicDistance);
		iMinNbAtomPerCluster = int.Parse(MinNbAtomPerCluster);

	}

	private void CreateTableColumns()
	{
		// Here we create a DataTable 
		tableCompo = new DataTable();

		tableCompo.Columns.Add("CLusterId", typeof(int));
		tableCompo.Columns.Add("NbAtoForCompo", typeof(int));
		tableCompo.Columns.Add("NbAtoNoise", typeof(int));
		for (int i = 0; i < Atom.iNbElt; i++)
		{
			tableCompo.Columns.Add("NbAto " + Atom.EltName[i], typeof(int));
			tableCompo.Columns.Add("Compo " + Atom.EltName[i], typeof(float));
		}
	}

	private void AtomFiltering()
	{
		bool[] bShowElt;

		int iIndex;

		float[] fMapSize = new float[3];
		float[] fLocalCompo = new float[2];

		float fHistogramResolution = (float)0.1;
		int[,] iHistogram = new int[Convert.ToInt32(102 / fHistogramResolution), 2];

		ExampleChartData.Clear();
		if (Atom.bState == false)
		{
			LoadAtomMemory();
		}
		InitMenuParameters();

		if (SelectedFilteringId == 0)// Grid composition
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

				//bShowElt[iSelElementId] = true;
				bShowElt = bEltSelected;

				Map3d.bBuildComposition(fMapSize, fGridDelocalization, Atom, bShowElt);

				Console.WriteLine("Calculate Composition Map : OK ");
			}

			// Calculate composition for each atom
			// --------------------------------------
			Console.WriteLine("Calculate Atom composition ...");

			int iNbFilteredAtom = 0;

			Array.Clear(iHistogram, 0, iHistogram.Length);

			for (int i = 0; i < Atom.iMemSize; i++)
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
				for (int j = 0; j < 2; j++)
				{
					iIndex = Convert.ToInt32(Math.Truncate((fLocalCompo[j]) / fHistogramResolution));
					iHistogram[iIndex, j]++;
					iHistogram[1001, j]++;
				}
			}

			Console.WriteLine("Calculate Atom composition : OK  /   Nb filtered atom = {0}", iNbFilteredAtom);

			// Display Composition curves
			// --------------------------------
			//float[] xVals = new float[Convert.ToInt32(100 / fHistogramResolution)];
			//float[] exp_dist = new float[Convert.ToInt32(100 / fHistogramResolution)];
			//float[] rnd_dist = new float[Convert.ToInt32(100 / fHistogramResolution)];
			Vector3[] dataRenderExp = new Vector3[Convert.ToInt32(100 / fHistogramResolution)];
			Vector3[] dataRenderRnd = new Vector3[Convert.ToInt32(100 / fHistogramResolution)];

			for (int i = 0; i < dataRenderExp.Length; i++)
			{
				//xVals[i] = (float)(i * fHistogramResolution + fHistogramResolution / 2);
				//exp_dist[i] = iHistogram[i, 0] * 100f / (float)iHistogram[1001, 0];
				//rnd_dist[i] = iHistogram[i, 1] * 100f / (float)iHistogram[1001, 1];
				dataRenderExp[i] = new Vector3(i * fHistogramResolution + fHistogramResolution / 2, 0, iHistogram[i, 0] * 100f / (float)iHistogram[1001, 0]);
				dataRenderRnd[i] = new Vector3(i * fHistogramResolution + fHistogramResolution / 2, 0, iHistogram[i, 1] * 100f / (float)iHistogram[1001, 1]);
			}

			ExampleChartData.Add(_renderDataFactory.CreateLine(dataRenderExp, Colors.Red, thickness: 2, name: "Experimental", isVisible: true));
			ExampleChartData.Add(_renderDataFactory.CreateLine(dataRenderRnd, Colors.Blue, thickness: 2, name: "Randomized", isVisible: true));

			//var freqDistChart = viewBuilder.AddChart2D("Composition Frequency distribution", "Concentration (ion%)", "Relative frequency (%)");
			//freqDistChart.AddLine(xVals, exp_dist, Colors.Red, "Experimental");
			//freqDistChart.AddLine(xVals, rnd_dist, Colors.Blue, "Randomized");

			// Save data in atom file
			// -------------------------
			//Atom.bAtomFileAccess(true, "D:\\Documents\\Desktop\\Travail\\Labo\\APsuite\\Experiment files\\test.ato");


			bFilteringState = true;

			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");

		} 
		else if (SelectedFilteringId == 1)//Local composition
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

			bEltCompo = bEltCalc = bEltSelected;

			if (AllAtomFiltering == true)
			{
				for (int i = 0; i < 256; i++)
				{
					bEltCalc[i] = true;
				}	
			}

			// Optimization map
			// -----------------
			Console.WriteLine("Build optimization map ...");
			CMapping OptMap = new CMapping();
			OptMap.bBuildOptimization(fGridSize, 0, Atom, null);
			iStep = Math.Max(1, Convert.ToInt32(fGridSize / OptMap.fNewSampSize[0]));
			if (fAtomicDistance / OptMap.fNewSampSize[0] - (float)iStep >= 0.5)
				iStep++;

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

			for (int i = 0; i < Atom.iMemSize; i++)
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
				for (int j = 0; j < 2; j++)
				{
					iIndex = Convert.ToInt32(Math.Truncate((fAtomComposition[i, 2 * j]) / fHistogramResolution));
					iHistogram[iIndex, j]++;
					iHistogram[1001, j]++;
				}
			}

			Console.WriteLine("Calculate Atom composition : OK  /   Nb filtered atom = {0}", iNbFilteredAtom);

			// Display Composition histogramm
			// -------------------------------
			Vector3[] dataRenderExp = new Vector3[Convert.ToInt32(100 / fHistogramResolution)];
			Vector3[] dataRenderRnd = new Vector3[Convert.ToInt32(100 / fHistogramResolution)];

			for (int i = 0; i < dataRenderExp.Length; i++)
			{
				dataRenderExp[i] = new Vector3(i * fHistogramResolution + fHistogramResolution / 2, 0, iHistogram[i, 0] * 100f / (float)iHistogram[1001, 0]);
				dataRenderRnd[i] = new Vector3(i * fHistogramResolution + fHistogramResolution / 2, 0, iHistogram[i, 1] * 100f / (float)iHistogram[1001, 1]);
			}

			ExampleChartData.Add(_renderDataFactory.CreateLine(dataRenderExp, Colors.Red, thickness: 2, name: "Experimental", isVisible: true));
			ExampleChartData.Add(_renderDataFactory.CreateLine(dataRenderRnd, Colors.Blue, thickness: 2, name: "Randomized", isVisible: true));


			// Save data in atom file
			// -------------------------
			//Atom.bAtomFileAccess(true, "D:\\Documents\\Desktop\\Travail\\Labo\\APsuite\\Experiment files\\test.ato");


			bFilteringState = true;

			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");
		}

	}

	private void AtomClustering()
	{
		bool[] bShowElt;
		tableContent.Clear();
		if (Atom.bState == false)
		{
			LoadAtomMemory();
		}
		InitMenuParameters();

		if(SelectedClusteringId == 0)
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

			int iStep;
			int iLocalNbCluster, iMinCluId;

			int[] iCluIdTable = new int[iNbParticuleMax + 2];
			int[] iNbAtomCluster = new int[iNbParticuleMax + 2];
			bool[] bClusterIsFound = new bool[iNbParticuleMax + 2];
			int[] iNewCluId = new int[iNbParticuleMax + 2];
			int[] iBlocId = new int[3];

			float fDistance;

			int a, b, c, d;

			// Add other atoms to solute atoms (user option)
			// ------------------------------------------------
			if (AllAtomClustering == true)
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
			for (int i = 0; i < Atom.iMemSize; i++)
			{
				iLocalCluId[i] = Atom.iCluId[i];
				Atom.iCluId[i] = 0;
			}

			// Step 1 : Global cluster identification
			// ----------------------------------------
			Console.WriteLine("Identification - Step1 ...");

			iLocalNbCluster = 0;

			for (int i = 0; i < Atom.iMemSize; i++)
				if (iLocalCluId[i] > 0 && Atom.iCluId[i] == 0)
				{
					for (int j = 0; j < 3; j++)
						iBlocId[j] = (int)((Atom.fPos[i, j] - OptMap.fSubLimit[j]) / OptMap.fNewSampSize[j]);

					for (c = Math.Max(iBlocId[2] - iStep, 0); c <= Math.Min(iBlocId[2] + iStep, OptMap.iNbStep[2] - 1); c++)
						for (b = Math.Max(iBlocId[1] - iStep, 0); b <= Math.Min(iBlocId[1] + iStep, OptMap.iNbStep[1] - 1); b++)
							for (a = Math.Max(iBlocId[0] - iStep, 0); a <= Math.Min(iBlocId[0] + iStep, OptMap.iNbStep[0] - 1); a++)
							{
								for (int j = 0; j < OptMap.iNbAtom[a, b, c]; j++)
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
			bShowElt = bEltSelected;

			for (int i = 0; i < Atom.iMemSize; i++)
				if (iLocalCluId[i] > 0)
					if (Atom.bEltId[i, 0] < Atom.iNbElt && bShowElt[Atom.bEltId[i, 0]] == true)
						iNbAtomCluster[iCluIdTable[Atom.iCluId[i]]]++;

			int m = 0;
			for (int i = 1; i <= iLocalNbCluster; i++)
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
			for (int i = 0; i < Atom.iMemSize; i++)
				if (iLocalCluId[i] > 0)
				{
					int j = Atom.iCluId[i];

					if (bClusterIsFound[j] == true)
						Atom.iCluId[i] = iNewCluId[j];
					else
						Atom.iCluId[i] = 0;
				}

			Console.WriteLine("Cluster Identification : Nb cluster = {0}", iLocalNbCluster);


			// Display cluster information
			// -------------------------------
			int[,] iNbAtomElt = new int[Atom.iNbCluster + 2, 256];

			for (int i = 0; i < Atom.iMemSize; i++)
			{
				iNbAtomElt[Atom.iCluId[i], Atom.bEltId[i, 0]]++;
				if (Atom.bEltId[i, 0] < Atom.iNbElt)
					iNbAtomElt[Atom.iCluId[i], Atom.iNbElt]++;
			}

			int[] iVal = new int[Atom.iNbElt];
			float[] fVal = new float[Atom.iNbElt];

			tableCompo.Rows.Clear();

			for (int i = 0; i <= iLocalNbCluster; i++)
			{
				for (int j = 0; j < Atom.iNbElt; j++)
				{
					iVal[j] = iNbAtomElt[i, j];
					fVal[j] = 100 * (float)iNbAtomElt[i, j] / (float)iNbAtomElt[i, Atom.iNbElt];
				}

				//tableContent.Add(new ClusterTableForDisplay(i, iNbAtomElt[i, Atom.iNbElt], iNbAtomElt[i, 255], iVal, fVal));
				DataRow newRow = tableCompo.NewRow();
				newRow[0] = i;
				newRow[1] = iNbAtomElt[i, Atom.iNbElt];
				newRow[2] = iNbAtomElt[i, 255];
				for (int j = 3, k = 0; j < 3 + Atom.iNbElt * 2; j += 2, k++)
				{
					newRow[j] = iVal[k];
					newRow[j + 1] = fVal[k];
				}
				tableCompo.Rows.Add(newRow);

				Console.WriteLine("Test : i = {0}    NbAtom0 = {1}", i, iNbAtomElt[i, 0]);
			}



			// Save data in atom file
			// -------------------------
			//Atom.bAtomFileAccess(true, "D:\\Documents\\Desktop\\Travail\\Labo\\APsuite\\Experiment files\\test.ato");


			ExecutionTime.Stop();
			Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");
		}
	}

	/*private void AddCheckBox()
	{
		var itemName = $"Item {_counter}";
		var newItem = new CheckBoxItem($"Item {_counter}", Colors.Black, isSelected: true);
		var lineRenderData = CreateRandomLineRenderData(_counter, itemName, newItem.IsSelected);
		_counter++;
		// Associate checkbox with random line visibility to demonstrate some useful work
		newItem.PropertyChanged += (sender, args) =>
		{
			if (args.PropertyName == nameof(CheckBoxItem.IsSelected))
			{
				lineRenderData.IsVisible = newItem.IsSelected;
			}
		};
		ExampleChartData.Add(lineRenderData);
		CheckBoxItemsClu.Add(newItem);
	}*/

	private ILineRenderData CreateRandomLineRenderData(int seed, string name, bool isVisible, float xMax = 10f, float xStep = 1f, float yMax = 10f)
	{
		var r = new Random(seed);
		var steps = (int)((xMax - 0f) / xStep);
		// Create random data
		var lineData = Enumerable.Range(0, steps + 1)
			.Select(x => new Vector3((float)x, 0f, r.NextSingle() * yMax))
			.ToArray();
		var color = Color.FromRgb((byte)r.Next(256), (byte)r.Next(256), (byte)r.Next(256));
		return _renderDataFactory.CreateLine(lineData, color, name: name, isVisible: isVisible);
	}

	private void UpdateCommandOnCanExecuteChanged(object? sender, EventArgs e)
	{
		DisplayUpdateOverlay = UpdateCommand.CanExecute(null);
	}

	// async void not recommended, but sufficient for a quick example
	/*protected override async void OnCreated(ViewModelCreatedEventArgs eventArgs)
	{
		base.OnCreated(eventArgs);
		// Event args can be used to get information
		_ionDisplayInfo = _ionDisplayInfoProvider.Resolve(InstanceId);
		await UpdateChartDataSeries();

		if (Node?.NodeDataState is not null)
		{
			Node.NodeDataState.PropertyChanged += DataStateOnPropertyChanged;
		}
	}*/

	/*private void DataStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		UpdateCommand.NotifyCanExecuteChanged();
	}*/

	private bool CanExecuteUpdate() => !(Node?.NodeDataState?.IsValid ?? false);

	private async Task UpdateChartDataSeries()
	{
		DataSeries.Clear();

		if (Node is null) return;
		var ionCounts = await Node.GetIonTypeCounts();
		
		foreach (var (ionTypeInfo, count) in ionCounts)
		{
			var seriesItem = new PieSeries
			{
				Title = ionTypeInfo.Name,
				Values = new ChartValues<ObservableValue> { new ObservableValue(count) },
				DataLabels = false,
			};
			// Try to retried ion color and set chart slice to color if possible
			if (_ionDisplayInfo?.GetColor(ionTypeInfo.Formula) is { } color)
			{
				seriesItem.Fill = new SolidColorBrush(color);
			}
			DataSeries.Add(seriesItem);
		}
	}
}


