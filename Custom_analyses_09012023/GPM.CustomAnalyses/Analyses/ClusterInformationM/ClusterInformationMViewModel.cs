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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

namespace GPM.CustomAnalyses.Analyses.ClusterInformationM;

internal class ClusterInformationMViewModel : AnalysisViewModelBase<ClusterInformationMNode>
{	
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.ClusterInformationM.ClusterInformationMViewModel";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IRenderDataFactory _renderDataFactory;

	private IIonDisplayInfo? _ionDisplayInfo = null;

	public SeriesCollection DataSeries { get; } = new();

	public DataTable tableCompo { get; set; }
	public ObservableCollection<IRenderData> HistoChartData { get; } = new();
	public ObservableCollection<IRenderData> SizeOrderingChartData { get; } = new();
	public ObservableCollection<IRenderData> RadialCompositionChartData { get; } = new();
	public ObservableCollection<IRenderData> ErorionProfilChartData { get; } = new();
	public ObservableCollection<CheckBoxItem> CheckBoxItemsClu { get; } = new();
	public ObservableCollection<ClusterTableForDisplay> tableContent { get; } = new();
	public ICommand SelectAllCluCommand { get; }
	public ICommand DeselectAllCluCommand { get; }
	public ICommand LoadAtomMemoryCommand { get; }
	public ICommand UpdateRepCommand { get; }
	public ICommand CalculationCommand { get; }

	public AsyncRelayCommand UpdateCommand { get; }

	// Parameters 
	// -------------
	public int TabSelectedIndex { get; set; } = 0;          // Display Id
	public string MinNbAtomPerCluster { get; set; } = "5";       // Min nb atom per cluster
	public string ClassSize { get; set; } = "0.1";            // Class size for profiles (nm)
	public string DistanceMax  { get; set; } = "2.0";          // Maximum distance for profiles (nm)

	// Variable declaration
	// ----------------------
	List<float> mass = new List<float>();
	List<Vector3> pos_init = new List<Vector3>();
	List<byte> name = new List<byte>();

	private int iMinNbAtomPerCluster  = 5;       // Min nb atom per cluster
	private float fClassSize  = 0.1f;            // Class size for profiles (nm)
	private float fDistanceMax  = 2.0f;          // Maximum distance for profiles (nm)

	bool[] bShowCluster;
	float fHistogramResolution = (float)0.1;
	int[,] iHistogram;

	CMapping Map3d = new CMapping();

	CAtom Atom = CustomAnalysesModule.Atom;

	Stopwatch ExecutionTime = new Stopwatch();

	private bool _displayUpdateOverlay;
	public bool DisplayUpdateOverlay
	{
		get => _displayUpdateOverlay;
		set => SetProperty(ref _displayUpdateOverlay, value);
	}

	public ClusterInformationMViewModel(
		IAnalysisViewModelBaseServices services,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
		IRenderDataFactory renderDataFactory)
		: base(services)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
		_renderDataFactory = renderDataFactory;
		UpdateCommand = new AsyncRelayCommand(UpdateChartDataSeries, CanExecuteUpdate);
		UpdateCommand.CanExecuteChanged += UpdateCommandOnCanExecuteChanged;

		UpdateRepCommand = new DelegateCommand(UpdateRep);
		LoadAtomMemoryCommand = new DelegateCommand(LoadAtomMemory);
		SelectAllCluCommand = new DelegateCommand(SelectAllClu);
		DeselectAllCluCommand = new DelegateCommand(DeselectAllClu);
		CalculationCommand = new DelegateCommand(Calculation);

		CreateTableColumns();

		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

		AddCheckBox();

	}

	private void InitMenuParameters()
	{
		iMinNbAtomPerCluster = int.Parse(MinNbAtomPerCluster);       
		fClassSize = float.Parse(ClassSize);           
		fDistanceMax = float.Parse(DistanceMax);

		iHistogram = new int[Convert.ToInt32(102 / fHistogramResolution), 2];
		bShowCluster = new bool[CheckBoxItemsClu.Count];
		for (int i=0; i<CheckBoxItemsClu.Count; i++)
		{
			bShowCluster[i] = CheckBoxItemsClu[i].IsSelected;
		}
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

	private void AddCheckBox()
	{
		CheckBoxItemsClu.Clear();
		for (int i = 0; i <= Atom.iNbCluster; i++)
		{
			string name = (i == 0 ? "Mat" : "Cl_" + i);
			Color color = (i == 0 ? Colors.Gray : Atom.tabColor[i % 10]);
			bool isRep = (i == 0 ? false : true);
			CheckBoxItemsClu.Add(new CheckBoxItem(name, color, isRep, "Cluster"));
		}
	}

	public void UpdateRep()
	{
		AddCheckBox();
	}

	public void Calculation()
	{
		InitMenuParameters();
		if (TabSelectedIndex == 0)
		{
			ClusterInformation();
		}
		else if (TabSelectedIndex == 1)
		{
			Histogram();
		}
		else if (TabSelectedIndex == 2)
		{
			SizeOrdering();
		}
		else if (TabSelectedIndex == 3)
		{
			RadialComposition();
		}
		else if (TabSelectedIndex == 4)
		{
			ErosionProfil();
		}
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
		tableCompo.Columns.Add("LgX", typeof(float));
		tableCompo.Columns.Add("LgY", typeof(float));
		tableCompo.Columns.Add("LgZ", typeof(float));
		tableCompo.Columns.Add("LgTot", typeof(float));
		tableCompo.Columns.Add("Rg", typeof(float));
		tableCompo.Columns.Add("X0", typeof(float));
		tableCompo.Columns.Add("Y0", typeof(float));
		tableCompo.Columns.Add("Z0", typeof(float));
	}

	private void ClusterInformation()
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
		for (int i = 0; i < Atom.iMemSize; i++)
		{
			iBlocId = Atom.iCluId[i];

			iNbAtomElt[iBlocId, Atom.bEltId[i, 0]]++;
			if (Atom.bEltId[i, 0] < Atom.iNbElt)
				iNbAtomElt[iBlocId, Atom.iNbElt]++;

			for (int j = 0; j < 3; j++)
				fVolumeCenter[iBlocId, j] += Atom.fPos[i, j];
			fVolumeCenter[iBlocId, 3]++;
		}

		for (int i = 0; i <= Atom.iNbCluster; i++)
			for (int j = 0; j < 3; j++)
				fVolumeCenter[i, j] /= (float)Math.Max(fVolumeCenter[i, 3], 1);


		// Cluster information
		// -----------------------
		for (int i = 0; i < Atom.iMemSize; i++)
			if (Atom.bEltId[i, 0] < Atom.iNbElt)
			{
				iElementId = Atom.bEltId[i, 0];
				iBlocId = Atom.iCluId[i];
				int j = 0;
				for (j = 0, fDstValue[3] = 0; j < 3; j++)
				{
					fDstValue[j] = Atom.fPos[i, j] - fVolumeCenter[iBlocId, j];
					fDstValue[j] *= fDstValue[j];
					fDstValue[3] += fDstValue[j];
				}
				j = 0;
				for (j = 0; j < 4; j++)
				{
					fDstValue[j] = (float)Math.Sqrt(fDstValue[j]);
					fLgValue[iBlocId, iElementId, j] += fDstValue[j];
					fLgValue[iBlocId, Atom.iNbElt, j] += fDstValue[j];
				}
			}


		for (int i = 0; i <= Atom.iNbCluster; i++)
			for (int k = 0; k <= Atom.iNbElt; k++)
				for (int j = 0; j < 4; j++)
					fLgValue[i, k, j] /= (float)Math.Max(iNbAtomElt[i, k], 1);


		// Display cluster information
		// -------------------------------
		int[] iVal = new int[10];
		float[] fVal1 = new float[10];
		float[] fVal2 = new float[10];
		tableCompo.Rows.Clear();

		for (int i = 0; i <= Atom.iNbCluster; i++)
		{
			for (int j = 0; j < Atom.iNbElt; j++)
			{
				iVal[j] = iNbAtomElt[i, j];
				fVal1[j] = 100 * (float)iNbAtomElt[i, j] / (float)iNbAtomElt[i, Atom.iNbElt];
			}

			for (int j = 0; j < 4; j++)
				fVal2[j] = fLgValue[i, Atom.iNbElt, j];

			fVal2[4] = (float)Math.Sqrt(5.0 / 3.0) * fLgValue[i, Atom.iNbElt, 3];

			for (int j = 5; j < 8; j++)
				fVal2[j] = fVolumeCenter[i, j - 5];

			
			if (bShowCluster[i])
			{
				//tableContent.Add(new ClusterTableForDisplay(i, iNbAtomElt[i, Atom.iNbElt], iNbAtomElt[i, 255], iVal, fVal1, fVal2));
				DataRow newRow = tableCompo.NewRow();
				newRow[0] = i;
				newRow[1] = iNbAtomElt[i, Atom.iNbElt];
				newRow[2] = iNbAtomElt[i, 255];
				for (int j = 3, k=0; j < 3 + Atom.iNbElt * 2; j+=2,k++)
				{
					newRow[j] = iVal[k];
					newRow[j+1] = fVal1[k];
				}
				for(int j= 3+Atom.iNbElt*2, k=0; j < 3+ Atom.iNbElt * 2 + 8; j++, k++)
				{
					newRow[j] = fVal2[k];
				}

				tableCompo.Rows.Add(newRow);
			}
		}

		TabSelectedIndex = 0;
		ExecutionTime.Stop();
		Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");
	}

	private void Histogram()
	{
		Console.WriteLine("GPM  -  Cluster histogram");
		Console.WriteLine("Nb cluster = {0}", Atom.iNbCluster);
		ExecutionTime.Restart();
		HistoChartData.Clear();

		int iLocalNbCluster, iBlocId, iElementId;

		iLocalNbCluster = Atom.iNbCluster + 2;
		int[,] iNbAtomElt = new int[iLocalNbCluster, 256];


		// Cluster : Nb atom per cluster
		// --------------------------------
		for (int i = 0; i < Atom.iMemSize; i++)
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
		Vector2[] histo = new Vector2[Convert.ToInt32(iLocalNbCluster / fHistogramResolution)];

		for (int i = 0; i < iLocalNbCluster / fHistogramResolution; i++)
			xVals[i] = (float)i * fHistogramResolution;

		

		// Applies color under histogram
		List<IChart2DSlice> slices;
		for (int i = 0; i < Atom.iNbElt; i++)
		{
			Array.Clear(exp_dist, 0, exp_dist.Length);

			for (int j = 1; j <= Atom.iNbCluster; j++)
			{
				if (CheckBoxItemsClu[j].IsSelected)
				{
					iBlocId = (int)(j / fHistogramResolution) - 1 + 4 * (i + 1);
					exp_dist[iBlocId] = exp_dist[iBlocId + 1] = exp_dist[iBlocId + 2] = (float)iNbAtomElt[j, i];
				}
			}

			for (int j= 0; j<xVals.Length; j++)
			{
				Vector2 vec = new Vector2(xVals[j], exp_dist[j]);
				histo[j] =vec;
			}
			
			slices = new List<IChart2DSlice> { new Chart2DSlice(xVals[0], xVals[^1], Atom.EltColor[i]) };

			HistoChartData.Add(_renderDataFactory.CreateHistogram(histo, Atom.EltColor[i], 1 , null, slices));
		}

		Array.Clear(exp_dist, 0, exp_dist.Length);
		for (int j = 1; j <= Atom.iNbCluster; j++)
		{
			if (CheckBoxItemsClu[j].IsSelected)
			{
				iBlocId = (int)(j / fHistogramResolution) - 1;
				exp_dist[iBlocId] = exp_dist[iBlocId + 1] = exp_dist[iBlocId + 2] = (float)iNbAtomElt[j, Atom.iNbElt];
			}
		}

		for (int j = 0; j < xVals.Length; j++)
		{
			Vector2 vec = new Vector2(xVals[j], exp_dist[j]);
			histo[j] = vec;
		}


		slices = new List<IChart2DSlice> { new Chart2DSlice(xVals[0], xVals[^1], Colors.Black) };
		HistoChartData.Add(_renderDataFactory.CreateHistogram(histo, Colors.Black, 1, null, slices));


		ExecutionTime.Stop();
		Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");
	}

	private void SizeOrdering()
	{
		Console.WriteLine("GPM  -  Cluster size ordering");
		Console.WriteLine("Nb cluster = {0}    Min Nb atom / cluster = {1}", Atom.iNbCluster, iMinNbAtomPerCluster);
		ExecutionTime.Restart();
		SizeOrderingChartData.Clear();

		int iLocalNbCluster, iBlocId, iNbClusterFound;

		iLocalNbCluster = Atom.iNbCluster + 2;
		int[,] iNbAtomElt = new int[iLocalNbCluster, 256];
		int[] iNbAtomPerCluster = new int[iLocalNbCluster];
		int[] iClusterId = new int[iLocalNbCluster];
		bool[] bState = new bool[iLocalNbCluster];
		int k = 0;


		// Nb atom per cluster
		// --------------------------------
		for (int i = 0; i < Atom.iMemSize; i++)
			iNbAtomPerCluster[Atom.iCluId[i]]++;

		// Sort clusters size
		// --------------------------------
		for (int i = 1; i <= Atom.iNbCluster; i++)
			if (iNbAtomPerCluster[i] >= iMinNbAtomPerCluster)
				bState[i] = true;

		iNbClusterFound = 1;

		for (int i = 1; i <= Atom.iNbCluster; i++)
		{
			k = 0;

			for (int j = 1; j <= Atom.iNbCluster; j++)
				if (bState[j] == true && iNbAtomPerCluster[j] < iNbAtomPerCluster[k])
					k = j;

			if (k > 0)
			{
				iClusterId[k] = iNbClusterFound;
				bState[k] = false;
				iNbClusterFound++;
			}
		}


		// Update Cluster Id and histogram
		// ----------------------------------
		for (int i = 0; i < Atom.iMemSize; i++)
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
		Vector2[] histo = new Vector2[Convert.ToInt32(iLocalNbCluster / fHistogramResolution)];

		for (int i = 0; i < iLocalNbCluster / fHistogramResolution; i++)
			xVals[i] = (float)i * fHistogramResolution;


		// Applies color under histogram
		List<IChart2DSlice> slices;
		for (int i = 0; i < Atom.iNbElt; i++)
		{
			Array.Clear(exp_dist, 0, exp_dist.Length);

			for (int j = 1; j <= Atom.iNbCluster; j++)
			{
				if (CheckBoxItemsClu[j].IsSelected)
				{
					iBlocId = (int)(j / fHistogramResolution) - 1 + 4 * (i + 1);
					exp_dist[iBlocId] = exp_dist[iBlocId + 1] = exp_dist[iBlocId + 2] = (float)iNbAtomElt[j, i];
				}
			}

			for (int j = 0; j < xVals.Length; j++)
			{
				Vector2 vec = new Vector2(xVals[j], exp_dist[j]);
				histo[j] = vec;
			}

			slices = new List<IChart2DSlice> { new Chart2DSlice(xVals[0], xVals[^1], Atom.EltColor[i]) };
			SizeOrderingChartData.Add(_renderDataFactory.CreateHistogram(histo, Atom.EltColor[i], 1, null, slices));
		}

		Array.Clear(exp_dist, 0, exp_dist.Length);
		for (int j = 1; j <= Atom.iNbCluster; j++)
		{
			if (CheckBoxItemsClu[j].IsSelected)
			{
				iBlocId = (int)(j / fHistogramResolution) - 1;
				exp_dist[iBlocId] = exp_dist[iBlocId + 1] = exp_dist[iBlocId + 2] = (float)iNbAtomElt[j, Atom.iNbElt];
			}
		}

		for (int j = 0; j < xVals.Length; j++)
		{
			Vector2 vec = new Vector2(xVals[j], exp_dist[j]);
			histo[j] = vec;
		}

		slices = new List<IChart2DSlice> { new Chart2DSlice(xVals[0], xVals[^1], Colors.Black) };
		SizeOrderingChartData.Add(_renderDataFactory.CreateHistogram(histo, Colors.Black, 1, null, slices));


		ExecutionTime.Stop();
		Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");
	}

	private void RadialComposition()
	{
		Console.WriteLine("GPM  -  Cluster Radial composition");
		/*if (iSelectedClusterId == 0)
			Console.WriteLine("Nb cluster = {0}    All clusters are selected", Atom.iNbCluster);
		else
			Console.WriteLine("Nb cluster = {0}    Selected cluster = {1}", Atom.iNbCluster, iSelectedClusterId);*/
		Console.WriteLine("Class size = {0} nm    Maximum distance = {1} nm", fClassSize, fDistanceMax);
		ExecutionTime.Restart();
		RadialCompositionChartData.Clear();

		int iLocalNbCluster, iBlocId, iElementId;
		int iIndex;

		iLocalNbCluster = Atom.iNbCluster + 2;
		int[,] iNbAtomElt = new int[iLocalNbCluster, 256];
		float[,] fCluCenter = new float[iLocalNbCluster, 4];
		float[,,] fCluLimit = new float[iLocalNbCluster, 3, 2];
		float[] fDstValue = new float[4];


		// Cluster center and limits
		// ----------------------------
		for (int i = 0; i < iLocalNbCluster; i++)
			for (int j = 0; j < 3; j++)
			{
				fCluLimit[i, j, 0] = 10000;
				fCluLimit[i, j, 1] = -10000;
			}

		for (int i = 0; i < Atom.iMemSize; i++)
			if (Atom.bEltId[i, 0] < Atom.iNbElt)
			{
				iBlocId = Atom.iCluId[i];

				for (int j = 0; j < 3; j++)
				{
					fCluLimit[iBlocId, j, 0] = Math.Min(Atom.fPos[i, j], fCluLimit[iBlocId, j, 0]);
					fCluLimit[iBlocId, j, 1] = Math.Max(Atom.fPos[i, j], fCluLimit[iBlocId, j, 1]);
					fCluCenter[iBlocId, j] += Atom.fPos[i, j];
				}

				fCluCenter[iBlocId, 3]++;
			}

		for (int k = 0; k < iLocalNbCluster; k++)
			for (int j = 0; j < 3; j++)
			{
				fCluLimit[k, j, 0] -= fDistanceMax;
				fCluLimit[k, j, 1] += fDistanceMax;
			}

		for (int i = 0; i <= Atom.iNbCluster; i++)
			for (int j = 0; j < 3; j++)
				fCluCenter[i, j] /= (float)Math.Max(fCluCenter[i, 3], 1);


		// Composition Profile
		// -----------------------
		int iProfileSize = 2 + (int)(fDistanceMax / fClassSize);
		float[,] fProfile = new float[Atom.iNbElt + 2, iProfileSize];

		for (int i = 0; i < Atom.iMemSize; i++)
			if (Atom.bEltId[i, 0] < Atom.iNbElt)
				for (int k = 1; k <= Atom.iNbCluster; k++)
					if (CheckBoxItemsClu[k].IsSelected)
						if (Atom.fPos[i, 0] >= fCluLimit[k, 0, 0] && Atom.fPos[i, 0] <= fCluLimit[k, 0, 1])
							if (Atom.fPos[i, 1] >= fCluLimit[k, 1, 0] && Atom.fPos[i, 1] <= fCluLimit[k, 1, 1])
								if (Atom.fPos[i, 2] >= fCluLimit[k, 2, 0] && Atom.fPos[i, 2] <= fCluLimit[k, 2, 1])
								{
									int j = 0;
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
		Vector3[] data = new Vector3[iProfileSize];

		for (int i = 0; i < iProfileSize; i++)
			xVals[i] = (float)(i * fClassSize);


		for (int i = 0; i < Atom.iNbElt; i++)
		{
			float[] exp_dist = new float[iProfileSize];
			for (int j = 0; j < iProfileSize; j++)
			{
				exp_dist[j] = 100 * fProfile[i, j] / Math.Max(fProfile[Atom.iNbElt, j], 1);
				data[j] = new Vector3(xVals[j], 0, exp_dist[j]);
			}

			RadialCompositionChartData.Add(_renderDataFactory.CreateLine(data, Atom.EltColor[i]));
		}

		ExecutionTime.Stop();
		Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");
	}

	private void ErosionProfil()
	{

		Console.WriteLine("GPM  -  Cluster Erosion composition");
		/*if (iSelectedClusterId == -1)
			Console.WriteLine("Nb cluster = {0}    All clusters are selected", Atom.iNbCluster);
		else
			Console.WriteLine("Nb cluster = {0}    Selected cluster = {1}", Atom.iNbCluster, iSelectedClusterId);*/
		Console.WriteLine("Class size = {0} nm    Maximum distance = {1} nm", fClassSize, fDistanceMax);
		ExecutionTime.Restart();
		ErorionProfilChartData.Clear();

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
		Array.Fill(bShowCluster, false);


		for (int i = 1; i <= Atom.iNbCluster; i++)
		{
			bShowCluster[i] = CheckBoxItemsClu[i].IsSelected;
		}
			



		// Cluster centers and limits
		// ----------------------------
		Console.WriteLine("Cluster centers and limits ...");
		for (int i = 0; i <= Atom.iNbCluster; i++)
			for (int j = 0; j < 3; j++)
			{
				fCluLimit[i, j, 0] = 10000;
				fCluLimit[i, j, 1] = -10000;
			}

		for (int i = 0; i < Atom.iMemSize; i++)
		{
			iBlocId = Atom.iCluId[i];

			for (int j = 0; j < 3; j++)
			{
				fCluLimit[iBlocId, j, 0] = Math.Min(Atom.fPos[i, j], fCluLimit[iBlocId, j, 0]);
				fCluLimit[iBlocId, j, 1] = Math.Max(Atom.fPos[i, j], fCluLimit[iBlocId, j, 1]);
				fCluCenter[iBlocId, j] += Atom.fPos[i, j];
			}

			fCluCenter[iBlocId, 3]++;
		}

		fMaxDistanceFound = 0;
		for (int k = 1; k <= Atom.iNbCluster; k++)
		{
			for (int j = 0; j < 3; j++)
			{
				fCluLimit[k, j, 0] = Math.Max(fCluLimit[k, j, 0] - fDistanceMax, Atom.fLimit[j, 0]);
				fCluLimit[k, j, 1] = Math.Min(fCluLimit[k, j, 1] + fDistanceMax, Atom.fLimit[j, 1]);
				fMaxDistanceFound = Math.Max(Math.Abs(fCluLimit[k, j, 1] - fCluLimit[k, j, 0]), fMaxDistanceFound);

				//						if ( k==1)
				//							Console.WriteLine("fCluLimit[k, j, 0] = {0}    fCluLimit[k, j, 0] = {1}    Diff = {2}", fCluLimit[k, j, 0], fCluLimit[k, j, 1], fCluLimit[k, j, 1]- fCluLimit[k, j, 0]);
			}
		}

		for (int i = 0; i <= Atom.iNbCluster; i++)
			for (int j = 0; j < 3; j++)
				fCluCenter[i, j] /= (float)Math.Max(fCluCenter[i, 3], 1);

		//				Console.WriteLine("fMaxDistanceFound = {0}", fMaxDistanceFound);


		// Prepare memory for cluster and matrix fast calculation
		// --------------------------------------------------------
		Console.WriteLine("Prepare memories for calculation ...");
		iMaxNbAtomId = 0;
		for (int i = 0; i < Atom.iMemSize; i++)
		{
			if (Atom.iCluId[i] > 0)
			{
				iNbAtomId[Atom.iCluId[i], 1]++;
				iMaxNbAtomId = Math.Max(iNbAtomId[Atom.iCluId[i], 1], iMaxNbAtomId);
			}

			else
				for (int j = 1; j <= Atom.iNbCluster; j++)
					if (bShowCluster[j] == true)
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

		for (int i = 0; i < Atom.iMemSize; i++)
		{
			if (Atom.iCluId[i] > 0)
			{
				iIndexC[Atom.iCluId[i], iNbAtomId[Atom.iCluId[i], 1]] = i;
				iNbAtomId[Atom.iCluId[i], 1]++;
			}

			else
				for (int j = 1; j <= Atom.iNbCluster; j++)
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



		for (int i = 1; i <= Atom.iNbCluster; i++)
		{
			if (bShowCluster[i] == true)
			{
				for (int j = 0; j < iMaxNbAtomId; j++)
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
				for (int j = 0; j < iNbAtomId[i, 1]; j++)
					if (fDstMin[j, 1] != 500000)
						if (Atom.bEltId[iIndexC[i, j], 0] < Atom.iNbElt)
						{
							iBlocId = (int)((fDstMin[j, 1]) / fClassSize);
							iMaxBlocId[1] = Math.Max(iMaxBlocId[1], iBlocId);

							iCountAtom[iBlocId, Atom.bEltId[iIndexC[i, j], 0], 1]++;
							iCountAtom[iBlocId, Atom.iNbElt, 1]++;
						}

				for (int j = 0; j < iNbAtomId[i, 0]; j++)
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
		int iCountAverage;
		int[] iNbClassForDst = new int[2];
		float[] fDeltaDst = new float[2];


		int jj = 0;
		int kk = 0;

		for (int p = 0; p < 2; p++)
			for (int i = 0; i <= iMaxBlocId[p]; i++)
				if (iCountAtom[i, Atom.iNbElt, p] > 0)
				{
					jj += iCountAtom[i, Atom.iNbElt, p];
					kk++;
				}

		iCountAverage = jj / kk;


		for (int p = 0; p < 2; p++)
		{
			iNbClassForDst[p] = 1;
			fDeltaDst[p] = fClassSize;

			for (int i = 0; i <= iMaxBlocId[p] / 2; i++)
			{
				if (iCountAtom[i, Atom.iNbElt, p] < iCountAverage / 2)
				{
					fDeltaDst[p] += (float)(i + 2) * fClassSize;
					iNbClassForDst[p]++;

					for (int j = 0; j < Atom.iNbElt + 1; j++)
					{
						iCountAtom[i + 1, j, p] += iCountAtom[i, j, p];
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

		for (int i = 0; i <= iMaxBlocId[1]; i++)
			if (iCountAtom[i, Atom.iNbElt, 1] > 0)
				iProfileSize++;

		iMiddleBloc = iProfileSize;

		for (int i = 0; i <= iMaxBlocId[0]; i++)
			if (iCountAtom[i, Atom.iNbElt, 0] > 0)
				iProfileSize++;

		iProfileSize += 2;
		float[,] fProfile = new float[iProfileSize, Atom.iNbElt + 2];

		iNbBloc = 0;

		for (int i = 0, k = iMiddleBloc - 1; i <= iMaxBlocId[1] && k >= 0; i++)
			if (iCountAtom[i, Atom.iNbElt, 1] > 0)
			{
				if (k == iMiddleBloc - 1)
					fProfile[k, Atom.iNbElt + 1] = -fDeltaDst[0] / (float)iNbClassForDst[0];
				else
					fProfile[k, Atom.iNbElt + 1] = -(float)(i) * fClassSize;

				for (int j = 0; j <= Atom.iNbElt; j++)
					fProfile[k, j] = (float)iCountAtom[i, j, 1];

				k--;
				iNbBloc++;
			}


		for (int i = 0, k = iMiddleBloc; i <= iMaxBlocId[0] && k < iProfileSize; i++)
			if (iCountAtom[i, Atom.iNbElt, 0] > 0)
			{
				fProfile[k, Atom.iNbElt + 1] = (float)(k - iMiddleBloc + 1) * fClassSize;
				for (int j = 0; j <= Atom.iNbElt; j++)
					fProfile[k, j] = (float)iCountAtom[i, j, 0];

				k++;
				iNbBloc++;
			}


		// Display Composition profile
		// --------------------------------
		iProfileSize = iNbBloc;

		float[] xVals = new float[iProfileSize];
		Vector3[] data = new Vector3[iProfileSize];

		for (int j = 0; j < iProfileSize; j++)
			xVals[j] = fProfile[j, Atom.iNbElt + 1];

		for (int i = 0; i < Atom.iNbElt; i++)
		{
			float[] exp_dist = new float[iProfileSize];
			for (int j = 0; j < iProfileSize; j++)
			{
				if (fProfile[j, Atom.iNbElt] > 0)
					exp_dist[j] = 100 * fProfile[j, i] / Math.Max(fProfile[j, Atom.iNbElt], 1);
				else if (j > 0)
					exp_dist[j] = exp_dist[j - 1];

				data[j] = new Vector3(xVals[j], 0, exp_dist[j]);
			}

			ErorionProfilChartData.Add(_renderDataFactory.CreateLine(data, Atom.EltColor[i]));
		}


		ExecutionTime.Stop();
		Console.WriteLine("Execution time : " + ExecutionTime.Elapsed.TotalSeconds + " s\n");
	}

		/*private void AddCheckBoxData()
		{
			ExampleChartData.Clear();
			for (int i = 0; i <= Atom.iNbCluster; i++)
			{
				for (int j = 0; j <= Atom.iNbElt; j++)
				{
					List<Vector3> pos = new List<Vector3>();
					Color color;
					bool isRep;
					string name = "data" + i + j;
					foreach (Representation atom in tabCluster[i, j])
					{
						pos.Add(new Vector3(atom.X, atom.Y, atom.Z));
					}
					color = (ColorRep ? CheckBoxItemsElt[j].Color : CheckBoxItemsClu[i].Color);
					isRep = (CheckBoxItemsElt[j].IsSelected && CheckBoxItemsClu[i].IsSelected ? true : false);
					var pointRenerData = _renderDataFactory.CreatePoints(pos.ToArray(), color, name, isRep);
					AddCheckBoxProperties(i, j, pointRenerData, CheckBoxItemsElt[j]);
					AddCheckBoxProperties(i, j, pointRenerData, CheckBoxItemsClu[i]);
					ExampleChartData.Add(pointRenerData);
				}
			}
		}

		private void AddCheckBoxProperties(int idClu, int idElt, IPointsRenderData pointRenerData, CheckBoxItem item)
		{	
			item.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == nameof(CheckBoxItem.IsSelected))
				{
					if (item.IsSelected)
					{
						if ((item.Type == "Element" && CheckBoxItemsClu[idClu].IsSelected) || (item.Type == "Cluster" && CheckBoxItemsElt[idElt].IsSelected))
						{
							pointRenerData.IsVisible = item.IsSelected;
						}
					}
					else
					{
						pointRenerData.IsVisible = item.IsSelected;
					}
				}
			};
		}*/

		private void SelectAllClu()
	{
		for (int i = 1; i <= Atom.iNbCluster; i++)
		{
			CheckBoxItemsClu[i].IsSelected = true;
		}
	}

	private void DeselectAllClu()
	{
		foreach (var item in CheckBoxItemsClu)
		{
			item.IsSelected = false;
		}
	}

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
	protected override async void OnCreated(ViewModelCreatedEventArgs eventArgs)
	{
		base.OnCreated(eventArgs);
		// Event args can be used to get information
		_ionDisplayInfo = _ionDisplayInfoProvider.Resolve(InstanceId);
		await UpdateChartDataSeries();

		if (Node?.NodeDataState is not null)
		{
			Node.NodeDataState.PropertyChanged += DataStateOnPropertyChanged;
		}
	}

	private void DataStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		UpdateCommand.NotifyCanExecuteChanged();
	}

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
