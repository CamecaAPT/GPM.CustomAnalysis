using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO.Packaging;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
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
using static GPM.CustomAnalyses.varGlob;
using static GPM.CustomAnalyses.fctGlob;
using Color = System.Windows.Media.Color;
using CommunityToolkit.HighPerformance;

namespace GPM.CustomAnalyses.Analyses.ClusterPositionM;

internal class ClusterPositionMViewModel : AnalysisViewModelBase<ClusterPositionMNode>
{	
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.ClusterPositionM.ClusterPositionMViewModel";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IRenderDataFactory _renderDataFactory;

	private IIonDisplayInfo? _ionDisplayInfo = null;

	public SeriesCollection DataSeries { get; } = new();

	public ObservableCollection<IRenderData> ExampleChartData { get; } = new();
	public ObservableCollection<CheckBoxItem> CheckBoxItemsClu { get; } = new();
	public ObservableCollection<CheckBoxItem> CheckBoxItemsElt { get; } = new();
	public ObservableCollection<CheckBoxItem> CheckBoxItemsFamily { get; } = new();
	public ICommand UpdateRepCommand { get; }
	public ICommand LoadmMemoryCommand { get; }
	public ICommand UpdateColorCommand { get; }
	public ICommand DeselectAllCluCommand { get; }
	public ICommand SelectAllCluCommand { get; }
	public ICommand GrowingCommand { get; }
	public ICommand ErosionCommand { get; }
	public ICommand UndoCommand { get; }
	public ICommand MoveFamilyACommand { get; }
	public ICommand MoveFamilyBCommand { get; }
	public ICommand MoveFamilyCCommand { get; }
	public ICommand MoveFamilyDCommand { get; }
	public ICommand MoveFamilyECommand { get; }
	public ICommand MoveFamilyFCommand { get; }
	public ICommand KmeansCommand { get; }
	public ICommand UndoKmeansCommand { get; }


	public event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public AsyncRelayCommand UpdateCommand { get; }

	//Parameters
	public string DistThreshold { get; set; } = "0.3";
	public string kmax { get; set; } = "3";
	public string scoreThres { get; set; } = "0.6";
	public string SelectedKMeansId { get; set; } = "0";
	public string silhThres { get; set; } = "0.2";
	public bool ColorClu { get; set; } = false;
	public bool ColorFam { get; set; } = false;
	public string test { get; set; }

	//Variables
	List<float> mass = new List<float>();
	List<Vector3> pos_init = new List<Vector3>();
	List<byte> ionType = new List<byte>();
	List<int> cluId = new List<int>();
	List<int> familyId = new List<int>();
	Elt elt = new Elt();
	SAtom Atom = new SAtom();
	List<Representation>[,] tabCluster;
	public float[] fSubLimit = new float[3];
	int[] iOldCluId;
	int[] iNewCluId;
	bool isRepUpload = false;
	bool[] isRepFamily = new bool[6];
	bool[] isRepCluster;
	bool[] isRepElt;
	bool allInFamily = true;
	
	public struct Representation
	{
		public int id;
		public float X;
		public float Y;
		public float Z;
		public int idElmt;
		public float fMass;
		public int idClus;
		public int[] iBlockId;
	}

	private bool _displayUpdateOverlay;
	public bool DisplayUpdateOverlay
	{
		get => _displayUpdateOverlay;
		set => SetProperty(ref _displayUpdateOverlay, value);
	}

	public ClusterPositionMViewModel(
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
		LoadmMemoryCommand = new DelegateCommand(LoadMemory);
		UpdateColorCommand = new DelegateCommand(AddCheckBoxData);
		SelectAllCluCommand = new DelegateCommand(SelectAllClu);
		DeselectAllCluCommand = new DelegateCommand(DeselectAllClu);
		GrowingCommand = new DelegateCommand(Growing);
		ErosionCommand = new DelegateCommand(Erosion);
		UndoCommand = new DelegateCommand(Undo);
		MoveFamilyACommand = new DelegateCommand(MoveFamilyA);
		MoveFamilyBCommand = new DelegateCommand(MoveFamilyB);
		MoveFamilyCCommand = new DelegateCommand(MoveFamilyC);
		MoveFamilyDCommand = new DelegateCommand(MoveFamilyD);
		MoveFamilyECommand = new DelegateCommand(MoveFamilyE);
		MoveFamilyFCommand = new DelegateCommand(MoveFamilyF);
		KmeansCommand = new DelegateCommand(KMeansClick);
		UndoKmeansCommand = new DelegateCommand(KMeansUndo);

		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

		ExampleChartData.Clear();

	}

	IIonData IonDataMemory;
	public async void LoadMemory()
	{
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		if (Atom.bState == false)
		{
			//Task<IIonData> IonDataTask = Node.GetIonData1();
			IonDataMemory = Node.GetIonData();//await IonDataTask;
			IIonDisplayInfo IonDisplayInfoMemory = Node.GetIonDisplayInfo();
			Console.WriteLine("Create Atom data memory ...");

			mass = await LoadAtoMemory<float>(IonDataMemory, IonDataSectionName.Mass);
			pos_init = await LoadAtoMemory<Vector3>(IonDataMemory, IonDataSectionName.Position);
			ionType = await LoadAtoMemory<byte>(IonDataMemory, IonDataSectionName.IonType);
			Atom.iMemSize = mass.Count;
			cluId = await LoadAtoMemory<int>(IonDataMemory, "ClusterID", Atom.iMemSize);
			Atom.iNbCluster = cluId.Max();
			familyId = await LoadAtoMemory<int>(IonDataMemory, "FamilyID", Atom.iMemSize);
			for (int i = 0; i < 6; i++)
			{
				if (familyId.Any(x => x == i))
					Console.WriteLine(" true " + i);
			}

			elt = LoadEltMemory(IonDataMemory, IonDisplayInfoMemory);

			Atom.fLimit = new float[3, 2];

			AllocateMemory(ref Atom);

			FillField<float>(ref Atom.fMass, mass, 0);
			RandomizeMass(ref Atom.fMass, Atom.iMemSize);
			mass.Clear();

			Vector3[] temp = new Vector3[Atom.iMemSize];
			FillField<Vector3>(ref temp, pos_init);
			Atom.fPos = Vec3toArray(temp, Atom.iMemSize);
			Atom.fLimit = CalculLimit(pos_init);

			pos_init.Clear();
			ClearArray<Vector3>(ref temp);
			CenterVolume(ref Atom);

			FillField<byte>(ref Atom.bEltId, ionType, 0);
			RandomizeElt(ref Atom.bEltId, ionType, Atom.iMemSize);
			ionType.Clear();

			FillField<int>(ref Atom.iCluId, cluId);
			cluId.Clear();

			FillField<int>(ref Atom.iFamilyId, familyId);
			familyId.Clear();

			Atom.bState = true;


			for (int i = 0; i < 6; i++)
			{
				isRepFamily[i] = (Atom.iFamilyId.Any(x=> x==i) ? true : false);
			}

			isRepElt = new bool[elt.iNbElt + 1];
			for (int i = 0; i <= elt.iNbElt; i++)
			{
				isRepElt[i] = (i == elt.iNbElt ? false : true);
			}

			isRepCluster = new bool[Atom.iNbCluster + 1];
			for (int i = 0; i <= Atom.iNbCluster; i++)
			{
				isRepCluster[i] = (i == 0 ? false : true);
			}
		}
		Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, elt.iNbElt, Atom.iNbCluster);

		UpdateRep();
	}

	public bool CreateClusterStruct()
	{
		Representation rep = new Representation();
		Console.WriteLine("in" + Atom.iNbCluster + "  ");
		tabCluster = new List<Representation>[Atom.iNbCluster + 1, elt.iNbElt + 1];
		for (int i = 0; i <= Atom.iNbCluster; i++)
		{
			for (int j = 0; j <=elt.iNbElt; j++)
			{
				tabCluster[i,j] = new List<Representation>();
			}
		}

		for (int i = 0; i < Atom.iMemSize; i++)
		{
			int idElt = (Atom.bEltId[i, 0] == 255 ? elt.iNbElt : Atom.bEltId[i, 0]);
			rep.id = i;
			rep.X = Atom.fPos[i, 0];
			rep.Y = Atom.fPos[i, 1];
			rep.Z = Atom.fPos[i, 2];
			rep.idElmt = idElt;
			//rep.iBlockId = new int[3] {(int)((Atom.fPos[i, 0] - fSubLimit[0])/fSampling), (int)((Atom.fPos[i, 1] - fSubLimit[1]) / fSampling), (int)((Atom.fPos[i, 2] - fSubLimit[2]) / fSampling) };
			//Console.WriteLine(Atom.iNbCluster + "  " + Atom.iNbElt + "  " + Atom.iCluId[i] + "  " + idElt);
			tabCluster[Atom.iCluId[i], idElt].Add(rep);
		}
		isRepUpload = true;

		//Save cluster id in IonData
		IonDataMemory.WriteSection("ClusterID", Atom.iCluId);
		return true;
	}

	public void UpdateRep()
	{
		if (Atom.bState == false)
		{
			LoadMemory();
		}
		IIonDisplayInfo IonDisplayInfoMemory = Node.GetIonDisplayInfo();
		elt = LoadEltMemory(IonDataMemory, IonDisplayInfoMemory);

		if (isRepCluster.Length < Atom.iNbCluster + 1)
		{
			isRepCluster = ExtendArray(isRepCluster, Atom.iNbCluster - isRepCluster.Length + 1);
			isRepUpload = false;
		}

		if (isRepElt.Length < elt.iNbElt + 1)
		{
			isRepElt = ExtendArray(isRepElt, elt.iNbElt - isRepElt.Length + 1);
		}

		else if(!isRepUpload && Atom.bState)
		{
			CreateClusterStruct();
			isRepUpload = true;
		}
		AddCheckBox();
		AddCheckBoxData();
	}

	public  bool[] ExtendArray(bool[] inArray, int lengthExt)
	{
		bool[] newTab = new bool[inArray.Length + lengthExt];
		inArray.CopyTo(newTab, 0);
		for(int i=inArray.Length; i < newTab.Length; i++)
		{
			newTab[i] = true;
		}
		return newTab;
	}

	Char familyChar = 'A';
	private void AddCheckBox()
	{
		CheckBoxItemsClu.Clear();
		CheckBoxItemsElt.Clear();
		CheckBoxItemsFamily.Clear();
		for (int i = 0; i <= Atom.iNbCluster; i++)
		{
			string name = (i == 0 ? "Mat" : "Cl_" + i);
			Color color = (i == 0 ? Colors.Gray : varGlob.tabColor[i % 10]);
			bool isRep = isRepCluster[i];
			CheckBoxItemsClu.Add(new CheckBoxItem(i,name, color, isRep, "Cluster"));;
		}
		for (int i = 0; i <= elt.iNbElt; i++)
		{
			string name = (i == elt.iNbElt ? "Noise" : elt.Name[i]);
			Color color = (i == elt.iNbElt ? Colors.Gray : elt.Color[i]);
			bool isRep = isRepElt[i];
			CheckBoxItemsElt.Add(new CheckBoxItem(i,name, color, isRep, "Element"));
		}
		for (int i = 0; i <6; i++)
		{
			string name = "Family_" + (char)(familyChar+i);
			Color color = varGlob.tabColor[i % 10];
			bool isRep = isRepFamily[i] ;
			CheckBoxItemsFamily.Add(new CheckBoxItem(i,name, color, isRep, "Family", i));
			AddCheckBoxProperties2(CheckBoxItemsFamily[i]);
		}
	}

	private void AddCheckBoxData()
	{
		ExampleChartData.Clear();
		for (int i = 0; i <= Atom.iNbCluster; i++)
		{
			for (int j = 0; j <= elt.iNbElt; j++)
			{
				List<Vector3> pos = new List<Vector3>();
				Color color;
				bool isRep;
				string name = "data" + i + j;
				int iFamily = -1;
				foreach (Representation atom in tabCluster[i, j])
				{
					pos.Add(new Vector3(atom.X, atom.Y, atom.Z));
					for (int k = 0; k < 6; k++)
					{
						if (Atom.iFamilyId[atom.id] == k)
						{
							iFamily = k;
						}
					}
				}
				CheckBoxItemsClu[i].Family = iFamily;
				color = (ColorClu ? CheckBoxItemsClu[i].Color : ColorFam ? (iFamily == -1 ? Colors.Gray : CheckBoxItemsFamily[iFamily].Color) : CheckBoxItemsElt[j].Color );
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
				if(item.Type == "Element")
				{
					isRepElt[item.Id] = item.IsSelected;
				}
				else if(item.Type == "Cluster")
				{
					isRepCluster[item.Id] = item.IsSelected;
					if (item.IsSelected == true && item.IsSelected != CheckBoxItemsFamily[item.Family].IsSelected)
					{
						allInFamily = false;
						CheckBoxItemsFamily[item.Family].IsSelected = item.IsSelected;
						allInFamily = true;
					}
					else if(item.IsSelected == false && !CheckBoxItemsClu.Any(i => i.Family == item.Family && i.IsSelected == true) )
					{
						allInFamily = false;
						CheckBoxItemsFamily[item.Family].IsSelected = item.IsSelected;
						allInFamily = true;
					}
				}
			}
		};
	}

	private void AddCheckBoxProperties2(CheckBoxItem item)
	{
		item.PropertyChanged += (sender, args) =>
		{
			if (args.PropertyName == nameof(CheckBoxItem.IsSelected))
			{
				if(allInFamily)
				{
					for (int i = 0; i < CheckBoxItemsClu.Count; i++)
					{
						if (CheckBoxItemsClu[i].Family == item.Family)
						{
							CheckBoxItemsClu[i].IsSelected = item.IsSelected;
						}
					}
				}
				isRepFamily[item.Id] = item.IsSelected;
			}
		};
	}

	public void MoveFamilyA()
	{
		int idFamily = 0;
		isRepFamily[idFamily] = true;
		MoveFamily(idFamily);
	}
	public void MoveFamilyB()
	{
		int idFamily = 1;
		isRepFamily[idFamily] = true;
		MoveFamily(idFamily);
	}
	public void MoveFamilyC()
	{
		int idFamily = 2;
		isRepFamily[idFamily] = true;
		MoveFamily(idFamily);
	}
	public void MoveFamilyD()
	{
		int idFamily = 3;
		isRepFamily[idFamily] = true;
		MoveFamily(idFamily);
	}
	public void MoveFamilyE()
	{
		int idFamily = 4;
		isRepFamily[idFamily] = true;
		MoveFamily(idFamily);
	}
	public void MoveFamilyF()
	{
		int idFamily = 5;
		isRepFamily[idFamily] = true;
		MoveFamily(idFamily);
	}

	public async void MoveFamily(int family)
	{
		for (int i = 0; i <= Atom.iNbCluster; i++)
		{
			if (CheckBoxItemsClu[i].IsSelected)
			{
				for (int j = 0; j <= elt.iNbElt; j++)
				{
					foreach (Representation atom in tabCluster[i, j])
					{
						Atom.iFamilyId[atom.id] = family;
					}
				}
			}
		}
		for(int i = 0; i < 6; i++)
		{
			isRepFamily[i] = (i == family ? true : false);
		}

		//Save idFamily
		IonDataMemory.WriteSection("FamilyID", Atom.iFamilyId);
		for (int i = 0; i < 6; i++)
		{
			if (Atom.iFamilyId.Any(x => x == i))
				Console.WriteLine("mmmmmmmmmmm" + i);
		}
		familyId = await LoadAtoMemory<int>(IonDataMemory, "FamilyID", Atom.iMemSize);
		for (int i = 0; i < 6; i++)
		{
			if (familyId.Any(x => x == i))
				Console.WriteLine("ffffffffffffff" + i);
		}
		UpdateRep();
	}

	/*public void NewFamily()
	{
		string name = "Family_" + idFamily;
		Color color =  Atom.tabColor[idFamily % 10];
		bool isRep = false;
		CheckBoxItemsFamily.Add(new CheckBoxItem(name, color, isRep, "Family"));
		idFamily++;
	}

	public void DeleteFamily()
	{
		for (int i=0; i<CheckBoxItemsFamily.Count; i++)
		{
			if (CheckBoxItemsFamily[i].IsSelected)
			{
				CheckBoxItemsFamily.RemoveAt(i);
			}
		}
	}

	public void AddToFamily()
	{

	}

	public void RemoveFromFamily()
	{

	}*/

	private void Growing()
	{
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		//Init Memory Atom
		if (Atom.bState == false)
		{
			LoadMemory();
		}

		//Find Cluster Limits
		float[,,] fLimits = new float[Atom.iNbCluster + 1, 3, 2]; //[clu, space, min/max]
		for(int i = 0; i < Atom.iNbCluster + 1; i++)
		{
			for(int j = 0; j < 3; j++)
			{
				fLimits[i, j, 0] = 900000;
				fLimits[i, j, 1] = -900000;
			}
		}

		float fDistanceThres = float.Parse(DistThreshold);
		for (int i = 1; i <= Atom.iNbCluster; i++)
		{
			for(int j=0; j<= elt.iNbElt; j++)
			{
				if (tabCluster[i, j].Count != 0)
				{
					fLimits[i, 0, 0] = Math.Min(fLimits[i, 0, 0], tabCluster[i, j].Min(x => x.X));
					fLimits[i, 0, 1] = Math.Max(fLimits[i, 0, 1], tabCluster[i, j].Max(x => x.X));
					fLimits[i, 1, 0] = Math.Min(fLimits[i, 1, 0], tabCluster[i, j].Min(x => x.Y));
					fLimits[i, 1, 1] = Math.Max(fLimits[i, 1, 1], tabCluster[i, j].Max(x => x.Y));
					fLimits[i, 2, 0] = Math.Min(fLimits[i, 2, 0], tabCluster[i, j].Min(x => x.Z));
					fLimits[i, 2, 1] = Math.Max(fLimits[i, 2, 1], tabCluster[i, j].Max(x => x.Z));
				}
			}
			fLimits[i, 0, 0] -= fDistanceThres;
			fLimits[i, 0, 1] += fDistanceThres;
			fLimits[i, 1, 0] -= fDistanceThres;
			fLimits[i, 1, 1] += fDistanceThres;
			fLimits[i, 2, 0] -= fDistanceThres;
			fLimits[i, 2, 1] += fDistanceThres;
		}
		Console.WriteLine("Limits OK");

		//Growing Calcul
		iNewCluId = Atom.iCluId.ToArray();
		iOldCluId = Atom.iCluId.ToArray();

		int iNumberOfThread = Environment.ProcessorCount * 2;
		int iThreadStep = 0;
		List<Representation> Matrix = new List<Representation>();
		for (int i= 0; i<= elt.iNbElt; i++)
		{
			Matrix.AddRange(tabCluster[0, i]);
		}
		iThreadStep = Matrix.Count / iNumberOfThread;
		Console.WriteLine("Init thread OK");

		Parallel.For(0, iNumberOfThread, i =>
		{
			int iStart = i * iThreadStep;
			int iStop = (i + 1) * iThreadStep;
			iStop = (i == iNumberOfThread - 1) ? Matrix.Count : iStop;
			Console.WriteLine("id " + i + " start " + iStart + "    stop " + iStop);

			ThreadGrowingLoop(iStart, iStop, fLimits, tabCluster, Matrix);
		});
		Console.WriteLine("Thread finish OK");

		//Replace Atom Cluster ID by new Cluster Id
		Atom.iCluId = iNewCluId.ToArray();

		//Recalculated ClusterStructure with new Cluster Id
		CreateClusterStruct();
		Console.WriteLine("Cluster update OK");

		//update 3D view
		AddCheckBoxData();
		Console.WriteLine("View update OK");
	}

	private void Erosion()
	{
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		//Init Memory Atom
		if (Atom.bState == false)
		{
			LoadMemory();
		}

		//Find Cluster Limits
		float[,,] fLimits = new float[Atom.iNbCluster + 1, 3, 2]; //[clu, space, min/max]
		for (int i = 0; i < Atom.iNbCluster + 1; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				fLimits[i, j, 0] = 900000;
				fLimits[i, j, 1] = -900000;
			}
		}

		float fDistanceThres = float.Parse(DistThreshold);
		for (int i = 1; i <= Atom.iNbCluster; i++)
		{
			for (int j = 0; j <= elt.iNbElt; j++)
			{
				if (tabCluster[i, j].Count != 0)
				{
					fLimits[i, 0, 0] = Math.Min(fLimits[i, 0, 0], tabCluster[i, j].Min(x => x.X));
					fLimits[i, 0, 1] = Math.Max(fLimits[i, 0, 1], tabCluster[i, j].Max(x => x.X));
					fLimits[i, 1, 0] = Math.Min(fLimits[i, 1, 0], tabCluster[i, j].Min(x => x.Y));
					fLimits[i, 1, 1] = Math.Max(fLimits[i, 1, 1], tabCluster[i, j].Max(x => x.Y));
					fLimits[i, 2, 0] = Math.Min(fLimits[i, 2, 0], tabCluster[i, j].Min(x => x.Z));
					fLimits[i, 2, 1] = Math.Max(fLimits[i, 2, 1], tabCluster[i, j].Max(x => x.Z));
				}
			}
			fLimits[i, 0, 0] -= 0.15f;
			fLimits[i, 0, 1] += 0.15f;
			fLimits[i, 1, 0] -= 0.15f;
			fLimits[i, 1, 1] += 0.15f;
			fLimits[i, 2, 0] -= 0.15f;
			fLimits[i, 2, 1] += 0.15f;
		}
		Console.WriteLine("Limits OK");

		//Erosion Calcul
		iNewCluId = Atom.iCluId.ToArray();
		iOldCluId = Atom.iCluId.ToArray();

		int iNumberOfThread = Environment.ProcessorCount * 2;
		int iThreadStep = Atom.iNbCluster / iNumberOfThread;
		List<Representation> Matrix = new List<Representation>();
		for (int i = 0; i <= elt.iNbElt; i++)
		{
			Matrix.AddRange(tabCluster[0, i]);
		}
		Parallel.For(0, iNumberOfThread, i =>
		{
			int iStart = i * iThreadStep + 1;
			int iStop = (i + 1) * iThreadStep;
			iStop = (i == iNumberOfThread - 1) ? Atom.iNbCluster : iStop;
			Console.WriteLine(" id : " + i + " start " + iStart + "    stop " + iStop);

			ThreadErosionLoop(iStart, iStop, fLimits, tabCluster, Matrix);
		});
		Console.WriteLine("Thread finish OK");

		//Replace Atom Cluster ID by new Cluster Id
		Atom.iCluId = iNewCluId.ToArray();

		//Recalculated ClusterStructure with new Cluster Id
		CreateClusterStruct();
		Console.WriteLine("Cluster update OK");

		//Update 3D view
		AddCheckBoxData();
		Console.WriteLine("View update OK");
	}

	public void ThreadGrowingLoop(int start, int stop, float[,,] fCluLimits, List<Representation>[,] atom, List<Representation> matrix)
	{
		float[] fDelta = new float[3];
		float dist = 0;
		float squareThreshold = float.Parse(DistThreshold) * float.Parse(DistThreshold);
		for (int k = start; k < stop; k++) //Loop on Matrix atoms
		{
			for (int c = 1; c <= Atom.iNbCluster; c++) //Loop on Cluster
			{
				if (CheckBoxItemsClu[c].IsSelected)
				{
					if (matrix[k].X > fCluLimits[c, 0, 0] && matrix[k].X < fCluLimits[c, 0, 1]
					&& matrix[k].Y > fCluLimits[c, 1, 0] && matrix[k].Y < fCluLimits[c, 1, 1]
					&& matrix[k].Z > fCluLimits[c, 2, 0] && matrix[k].Z < fCluLimits[c, 2, 1])
					{
						for (int e = 0; e <= elt.iNbElt; e++) //Loop on elt
						{
							//Console.WriteLine(" ato Id : " + ato.id);
							for (int j = 0; j < atom[c, e].Count; j++) //Loop on Atoms in cluster
							{
								fDelta[0] = (atom[c, e][j].X - matrix[k].X); fDelta[0] *= fDelta[0];
								fDelta[1] = (atom[c, e][j].Y - matrix[k].Y); fDelta[1] *= fDelta[1];
								fDelta[2] = (atom[c, e][j].Z - matrix[k].Z); fDelta[2] *= fDelta[2];
								dist = fDelta[0] + fDelta[1] + fDelta[2];
								if (dist < squareThreshold)
								{
									iNewCluId[matrix[k].id] = c;
									//Console.WriteLine ("Cluster id : " + i + " ato Id : " + ato.id + " old Cul id : " + Atom.iCluId[ato.id] + " new clu id " + iNewCluId[ato.id]);
									break;
								}
							}
						}
					}
				}
			}
		}
	}

	public void ThreadErosionLoop(int start, int stop, float[,,] fCluLimits, List<Representation>[,] atom, List<Representation> matrix)
	{
		List<Representation> slectMatrixCluster = new List<Representation>();
		float[] fDelta = new float[3];
		float dist = 0;
		float squareThreshold = float.Parse(DistThreshold) * float.Parse(DistThreshold);
		int iNbAtoSelcted = 0;
		for (int c = start; c <= stop; c++) // Loop on clusters
		{
			if (CheckBoxItemsClu[c].IsSelected)
			{
				//Select Matrix atoms arround cluster
				slectMatrixCluster.Clear();
				slectMatrixCluster.AddRange(matrix.Where(o => o.X > fCluLimits[c, 0, 0] && o.X < fCluLimits[c, 0, 1]
					&& o.Y > fCluLimits[c, 1, 0] && o.Y < fCluLimits[c, 1, 1]
					&& o.Z > fCluLimits[c, 2, 0] && o.Z < fCluLimits[c, 2, 1]));
				iNbAtoSelcted = slectMatrixCluster.Count;
				for (int e = 0; e <= elt.iNbElt; e++)
				{
					for (int j = 0; j < atom[c, e].Count; j++) //Loop on Atoms in cluster
					{
						for (int k = 0; k < iNbAtoSelcted; k++) // Loop on matrix selected Atoms
						{
							fDelta[0] = (atom[c, e][j].X - slectMatrixCluster[k].X); fDelta[0] *= fDelta[0];
							fDelta[1] = (atom[c, e][j].Y - slectMatrixCluster[k].Y); fDelta[1] *= fDelta[1];
							fDelta[2] = (atom[c, e][j].Z - slectMatrixCluster[k].Z); fDelta[2] *= fDelta[2];
							dist = fDelta[0] + fDelta[1] + fDelta[2];
							if (dist < squareThreshold)
							{
								iNewCluId[atom[c, e][j].id] = 0;
								//Console.WriteLine ("Cluster id : " + i + " ato Id : " + ato.id + " old Cul id : " + Atom.iCluId[ato.id] + " new clu id " + iNewCluId[ato.id]);
								break;
							}
						}
					}
				}
			}
			
		}
	}

	public void Undo()
	{
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

		Atom.iCluId = iOldCluId.ToArray();

		//Init Clurter struct to calculation
		CreateClusterStruct();

		//Update 3D view
		AddCheckBoxData();
	}
	
	private void SelectAllClu()
	{
		foreach (var item in CheckBoxItemsClu)
		{
			if(item.Id != 0)
			{
				item.IsSelected = true;
				CheckBoxItemsFamily[item.Family].IsSelected = true;
			}
		}
	}

	private void DeselectAllClu()
	{
		foreach (var item in CheckBoxItemsClu)
		{
			item.IsSelected = false;
			isRepCluster[item.Id] = false;
		}
		foreach(var item in CheckBoxItemsFamily)
		{
			item.IsSelected=false;
			isRepFamily[item.Id] = false;
		}
	}

	//KMEANS
	float[,] dataSource;
	float[,] centroids;
	int iNbData = 0;
	int iKmax = 0;
	List<Result> lstBestCalcul = new List<Result>();
	List<List<float>> lstSilhouette  = new List<List<float>>();
	private struct Result
	{
		public float[,] centroidsStart;
		public float[,] centroidsEnd;
		public double sumDist;
		public float[,] data;
	}

	private struct SaveCluster
	{
		public int[] clusterId;
		public int nbCluster;
		public bool[] isRep;
	}
	SaveCluster saveCluster = new SaveCluster();

	public void KMeansClick()
	{
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		iKmax = int.Parse(kmax);
		int iKmeansIteration = 10;
		int iKmeansGroup = 5;
		int iSelectedKMeansId = int.Parse(SelectedKMeansId);
		float iSilhThres = float.Parse(silhThres);
		float esp = 0.01f;
		float scoreMin = float.Parse(scoreThres);
		int iNbAddCluster = 0;
		List<int> atomSelec = new List<int>();
		lstSilhouette.Clear();
		saveCluster.clusterId = new int[Atom.iCluId.Length];
		saveCluster.isRep = new bool[isRepCluster.Length];
		Array.Copy(isRepCluster, saveCluster.isRep, isRepCluster.Length);
		Array.Copy(Atom.iCluId, saveCluster.clusterId, Atom.iCluId.Length);
		saveCluster.nbCluster = Atom.iNbCluster;
		int iShiftSilList = 0;

		for (int c = 1; c <= Atom.iNbCluster; c++) // Loop on clusters
		{
			if (CheckBoxItemsClu[c].IsSelected) // Selected Clusters
			{
				lstBestCalcul.Clear();
				iKmax = int.Parse(kmax);
				//Number atom in cluster n
				atomSelec.Clear();
				for(int i=0; i< Atom.iMemSize; i++)
				{
					if (Atom.iCluId[i] == c)
					{
						atomSelec.Add(i);
					}
				}
				iNbData = atomSelec.Count;

				//Init data for cluster n
				dataSource = new float[iNbData,4];
				for(int i = 0; i< atomSelec.Count; i++)
				{
					dataSource[i, 0] = Atom.fPos[atomSelec[i], 0];
					dataSource[i, 1] = Atom.fPos[atomSelec[i], 1];
					dataSource[i, 2] = Atom.fPos[atomSelec[i], 2];
					dataSource[i, 3] = 0;
				}

				if (iSelectedKMeansId == 0)
				{
					Console.WriteLine("Manual : cluster ID = " + c + ", nb atom = " + iNbData);
					Console.WriteLine("!!!!!!!!!!!!!!!!!! K = " + iKmax + " !!!!!!!!!!!!!!!!!!!!");
					float newScore = 0;
					Result bestCalcul = KMeans(dataSource, iKmax, iNbData, iKmeansIteration, iKmeansGroup, esp);
					if (iKmax > 1)
					{
						newScore = Silhouette(bestCalcul.data, bestCalcul.centroidsEnd);
					}
					Console.WriteLine("Silhouette score k=" + iKmax + " : " + newScore);

					//update cluster id
					if(scoreMin <= newScore)
					{
						for (int i = 0; i < atomSelec.Count; i++)
						{
							//si id = 0 rester dans clu filtre sinon ajouter clu a la fin de la list
							if ((int)bestCalcul.data[i, 3] != 0)
							{
								//sup seuil dans ato dans new clu sinon matrice
								if (lstSilhouette[iShiftSilList][i] >= iSilhThres)
								{
									Atom.iCluId[atomSelec[i]] = Atom.iNbCluster + iNbAddCluster + (int)bestCalcul.data[i, 3];
								}
								else
								{
									Atom.iCluId[atomSelec[i]] = 0;
								}
							}
							else
							{
								//sup seuil dans ato dans new clu sinon matrice
								if(lstSilhouette[iShiftSilList][i] >= iSilhThres)
								{
									Atom.iCluId[atomSelec[i]] = Atom.iCluId[atomSelec[i]];
								}
								else
								{
									Atom.iCluId[atomSelec[i]] = 0;
								}
							}
						}
						iNbAddCluster += (iKmax - 1);
						Console.WriteLine("cluster ID = " + c + ", score = " + newScore + ", OK nb new clusters : " + iKmax);
					}
					else
					{
						Console.WriteLine("cluster ID = " + c + ", score = " + newScore + " NO UPDATE ID");
					}
					iShiftSilList++;
				}
				else if (iSelectedKMeansId == 1)
				{
					Console.WriteLine("Auto : cluster ID = " + c + ", nb atom = " + iNbData);
					int bestK = 1;
					float oldScore = 0;
					float newScore = 0;
					for (int k = 1; k <= iKmax; k++)
					{
						Console.WriteLine("!!!!!!!!!!!!!!!!!! K = " + k + " !!!!!!!!!!!!!!!!!!!!");
						lstBestCalcul.Add(KMeans(dataSource, k, iNbData, iKmeansIteration, iKmeansGroup, esp));
						if (k > 1)
						{
							newScore = Silhouette(lstBestCalcul[k - 1].data, lstBestCalcul[k - 1].centroidsEnd);
							Console.WriteLine("---"+k);
							if (newScore > oldScore)
							{
								oldScore = newScore;
								bestK = k;
							}
						}
					}
					Console.WriteLine("Best result for k=" + bestK + " , silhouette score : " + oldScore);
					Result bestCalcul = lstBestCalcul[bestK - 1];

					//update cluster id
					if (scoreMin <= oldScore)
					{
						for (int i = 0; i < atomSelec.Count; i++)
						{
							//si id = 0 rester dans clu filtre sinon ajouter clu a la fin de la list
							if ((int)bestCalcul.data[i, 3] != 0)
							{
								//sup seuil dans ato dans new clu sinon matrice
								if (lstSilhouette[iShiftSilList + bestK-2][i] >= iSilhThres)
								{
									Atom.iCluId[atomSelec[i]] = Atom.iNbCluster + iNbAddCluster + (int)bestCalcul.data[i, 3];
								}
								else
								{
									Atom.iCluId[atomSelec[i]] = 0;
								}
							}
							else
							{
								//sup seuil dans ato dans new clu sinon matrice
								if (lstSilhouette[iShiftSilList + bestK -2][i] >= iSilhThres)
								{
									Atom.iCluId[atomSelec[i]] = Atom.iCluId[atomSelec[i]];
								}
								else
								{
									Atom.iCluId[atomSelec[i]] = 0;
								}
							}
						}
						iNbAddCluster += (bestK - 1);
						Console.WriteLine("cluster ID = " + c + ", score = " + oldScore + ", OK nb new clusters : " + bestK);
					}
					else
					{
						Console.WriteLine("cluster ID = " + c + ", score = " + oldScore + " NO UPDATE ID");
					}
					iShiftSilList = lstSilhouette.Count;
				}
				else
				{
					Console.WriteLine("Error id KMeans selection");
				}
			}
		}
		Console.WriteLine("before" + Atom.iNbCluster+ "  "+iNbAddCluster);
		Atom.iNbCluster += iNbAddCluster;
		Console.WriteLine("after" + Atom.iNbCluster + "  " );
		UpdateRep();
	}

	public void KMeansUndo()
	{
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		isRepUpload = false;
		Array.Copy(saveCluster.clusterId, Atom.iCluId, saveCluster.clusterId.Length);
		isRepCluster = new bool[saveCluster.isRep.Length];
		Array.Copy(saveCluster.isRep, isRepCluster, saveCluster.isRep.Length);
		Array.Copy(isRepCluster, saveCluster.isRep, isRepCluster.Length);
		Atom.iNbCluster = saveCluster.nbCluster;
		UpdateRep();
	}

	Random rnd = new Random();
	float[,] RandomCentroid(int nb, float min, float max)
	{
		float[,] centroid = new float[nb, 3];
		for (int i = 0; i < nb; i++)
		{
			centroid[i, 0] = min + (max - min) * (float)rnd.NextDouble();
			centroid[i, 1] = min + (max - min) * (float)rnd.NextDouble();
			centroid[i, 2] = min + (max - min) * (float)rnd.NextDouble();
		}
		return centroid;
	}

	float dX, dY, dZ;
	float SquareDistance(float X1, float Y1, float Z1, float X2, float Y2, float Z2)
	{
		dX = X1 - X2; dX *= dX;
		dY = Y1 - Y2; dY *= dY;
		dZ = Z1 - Z2; dZ *= dZ;

		return dX + dY + dZ;
	}

	float[] UpdateCentroid(int id, float[,] data)
	{
		float[] newCentroid = new float[3];
		int nb = 0;
		for (int i = 0; i < iNbData; i++)
		{
			if ((int)data[i, 3] == id)
			{
				newCentroid[0] += data[i, 0];
				newCentroid[1] += data[i, 1];
				newCentroid[2] += data[i, 2];
				nb++;
			}
		}
		newCentroid[0] = newCentroid[0] / nb;
		newCentroid[1] = newCentroid[1] / nb;
		newCentroid[2] = newCentroid[2] / nb;

		return newCentroid;
	}

	Result KMeans(float[,] data, int K, int nbData, int nbIter, int nbGroup, float eps)
	{
		Result bestCalcul;
		float[,] centroidsStart;

		bestCalcul.data = new float[nbData, 4];
		bestCalcul.centroidsEnd = new float[K, 3];
		bestCalcul.centroidsStart = new float[K, 3];
		bestCalcul.sumDist = double.MaxValue;

		centroids = new float[K, 3];
		centroidsStart = new float[K, 3];

		for (int g = 0; g < nbGroup; g++)
		{
			Console.WriteLine("-----Group " + g);

			centroids = RandomCentroid(K, 0, 9);
			Array.Copy(centroids, centroidsStart, centroids.Length);

			float err = float.MaxValue;
			int test = 0;
			for (int itr = 0; (itr < nbIter) && (err > eps); itr++)
			{
				int id = 0;
				float minDist = 0;
				float calDist = 0;

				for (int i = 0; i < nbData; i++)
				{
					minDist = float.MaxValue;
					id = 0;
					for (int j = 0; j < K; j++)
					{
						calDist = SquareDistance(data[i, 0], data[i, 1], data[i, 2], centroids[j, 0], centroids[j, 1], centroids[j, 2]);
						if (calDist < minDist)
						{
							minDist = calDist;
							id = j;
						}
					}
					data[i, 3] = id;
				}

				err = 0;
				for (int i = 0; i < K; i++)
				{
					float[] newCentroid = UpdateCentroid(i, data);
					err += (float)Math.Sqrt(SquareDistance(centroids[i, 0], centroids[i, 1], centroids[i, 2], newCentroid[0], newCentroid[1], newCentroid[2]));
					centroids[i, 0] = newCentroid[0];
					centroids[i, 1] = newCentroid[1];
					centroids[i, 2] = newCentroid[2];
				}
				if (float.IsNaN(err))
				{
					//Console.WriteLine("Error NaN restart");
					centroids = RandomCentroid(K, 0, 9);
					Array.Copy(centroids, centroidsStart, centroids.Length);
					itr = -1;
					err = float.MaxValue;
					test++;
					if (test > 100)
					{
						iKmax = K - 1;
						break;
					}
				}
				else
				{
					Console.WriteLine("Itr n°" + itr + "  Error : " + err);
					test = 0;
				}
			}

			double sumDist = 0;
			for (int i = 0; i < nbData; i++)
			{
				sumDist += SquareDistance(data[i, 0], data[i, 1], data[i, 2], centroids[(int)data[i, 3], 0], centroids[(int)data[i, 3], 1], centroids[(int)data[i, 3], 2]);
			}
			Console.WriteLine("Sum Dist : " + sumDist);
			if (sumDist < bestCalcul.sumDist)
			{
				Console.WriteLine("New best");
				Array.Copy(data, bestCalcul.data, data.Length);
				Array.Copy(centroidsStart, bestCalcul.centroidsStart, centroidsStart.Length);
				Array.Copy(centroids, bestCalcul.centroidsEnd, centroids.Length);
				bestCalcul.sumDist = sumDist;
			}
		}
		return bestCalcul;
	}

	float Silhouette(float[,] data, float[,] centroids)
	{
		float score = 0;
		float[] silhouette = new float[data.Length];
		float dist;
		float minDist;
		int nearCentroidId;
		int nbIn = 0;
		int nbOut = 0;
		double sumIn;
		double sumOut;


		for (int i = 0; i < data.GetLength(0); i++)
		{
			//Find nearest centroid
			minDist = float.MaxValue;
			nbIn = 0;
			nbOut = 0;
			sumIn = 0;
			sumOut = 0;
			nearCentroidId = 0;
			for (int j = 0; j < centroids.GetLength(0); j++)
			{
				if (j != data[i, 3])
				{
					dist = SquareDistance(data[i, 0], data[i, 1], data[i, 2], centroids[j, 0], centroids[j, 1], centroids[j, 2]);
					if (dist < minDist)
					{
						minDist = dist;
						nearCentroidId = j;
					}
				}
			}

			//Average distance calculation
			for (int j = 0; j < data.GetLength(0); j++)
			{
				if (data[j, 3] == data[i, 3])
				{
					sumIn += (SquareDistance(data[i, 0], data[i, 1], data[i, 2], data[j, 0], data[j, 1], data[j, 2]));
					nbIn++;
				}
				else if (data[j, 3] == nearCentroidId)
				{
					sumOut += (SquareDistance(data[i, 0], data[i, 1], data[i, 2], data[j, 0], data[j, 1], data[j, 2]));
					nbOut++;
				}
				else
				{

				}
			}
			sumIn = sumIn / nbIn;
			sumOut = sumOut / nbOut;
			silhouette[i] = (float)((sumOut - sumIn) / Math.Max(sumIn, sumOut));
			score += silhouette[i];
		}
		Console.WriteLine("appel");
		lstSilhouette.Add(new List<float>(silhouette.ToList()));
		score = score / data.GetLength(0);
		Console.WriteLine("Silhouette score : " + score);

		return score;
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

