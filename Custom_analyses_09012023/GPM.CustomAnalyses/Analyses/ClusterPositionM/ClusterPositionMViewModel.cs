using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Cameca.Extensions.Controls;
using CommunityToolkit.Mvvm.Input;
using Prism.Commands;
using Color = System.Windows.Media.Color;

namespace GPM.CustomAnalyses.Analyses.ClusterPositionM;

internal class ClusterPositionMViewModel : AnalysisViewModelBase<ClusterPositionMNode>
{	
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.ClusterPositionM.ClusterPositionMViewModel";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IRenderDataFactory _renderDataFactory;

	private IIonDisplayInfo? _ionDisplayInfo = null;

	public ObservableCollection<IRenderData> ExampleChartData { get; } = new();
	public ObservableCollection<CheckBoxItem> CheckBoxItemsClu { get; } = new();
	public ObservableCollection<CheckBoxItem> CheckBoxItemsElt { get; } = new();
	public ObservableCollection<CheckBoxItem> CheckBoxItemsFamily { get; } = new();
	public ICommand UpdateRepCommand { get; }
	public ICommand UpdateColorCommand { get; }
	public ICommand DeselectAllCluCommand { get; }
	public ICommand SelectAllCluCommand { get; }
	public ICommand GrowingCommand { get; }
	public ICommand ErosionCommand { get; }
	public ICommand UndoCommand { get; }
	public ICommand NewFamilyCommand { get; }
	public ICommand DeleteFamilyCommand { get; }
	public ICommand AddToFamilyCommand { get; }
	public ICommand RemoveFromFamilyCommand { get; }

	public AsyncRelayCommand UpdateCommand { get; }

	//Parameters
	public string DistThreshold { get; set; } = "0.3";
	public bool ColorRep { get; set; } = true; //flase cluster color, true elt color

	//Variables
	CAtom Atom = CustomAnalysesModule.Atom;
	List<Representation>[,] tabCluster;
	public float[] fSubLimit = new float[3];
	int[] iOldCluId;
	int[] iNewCluId;
	bool isRepUpload = false;

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
		UpdateColorCommand = new DelegateCommand(AddCheckBoxData);
		SelectAllCluCommand = new DelegateCommand(SelectAllClu);
		DeselectAllCluCommand = new DelegateCommand(DeselectAllClu);
		GrowingCommand = new DelegateCommand(Growing);
		ErosionCommand = new DelegateCommand(Erosion);
		UndoCommand = new DelegateCommand(Undo);
		NewFamilyCommand = new DelegateCommand(NewFamily);
		DeleteFamilyCommand = new DelegateCommand(DeleteFamily);
		AddToFamilyCommand = new DelegateCommand(AddToFamily);
		RemoveFromFamilyCommand = new DelegateCommand(RemoveFromFamily);

		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

		UpdateRep();
	}

	public async void LoadAtomMemory()
	{
		if (Atom.bState == false)
		{
			Task<IIonData> IonDataTask = Node.GetIonData1();
			IIonData IonDataMemory = await IonDataTask;
			IIonDisplayInfo IonDisplayInfoMemory = Node.GetIonDisplayInfo();
			Console.WriteLine("Create Atom data memory ...");
			Atom.bInitMemory2(IonDataMemory, IonDisplayInfoMemory);
		}
		Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

		if (!isRepUpload && Atom.bState)
		{
			CreateClusterStruct();
		}
	}

	public bool CreateClusterStruct()
	{
		Representation rep = new Representation();

		tabCluster = new List<Representation>[Atom.iNbCluster + 1, Atom.iNbElt + 1];
		for (int i = 0; i <= Atom.iNbCluster; i++)
		{
			for (int j = 0; j <=Atom.iNbElt; j++)
			{
				tabCluster[i,j] = new List<Representation>();
			}
		}

		for (int i = 0; i < Atom.iMemSize; i++)
		{
			int idElt = (Atom.bEltId[i, 0] == 255 ? Atom.iNbElt : Atom.bEltId[i, 0]);
			rep.id = i;
			rep.X = Atom.fPos[i, 0];
			rep.Y = Atom.fPos[i, 1];
			rep.Z = Atom.fPos[i, 2];
			rep.idElmt = idElt;
			//rep.iBlockId = new int[3] {(int)((Atom.fPos[i, 0] - fSubLimit[0])/fSampling), (int)((Atom.fPos[i, 1] - fSubLimit[1]) / fSampling), (int)((Atom.fPos[i, 2] - fSubLimit[2]) / fSampling) };
			tabCluster[Atom.iCluId[i], idElt].Add(rep);
		}
		isRepUpload = true;
		return true;
	}

	public void UpdateRep()
	{
		if (Atom.bState == false)
		{
			LoadAtomMemory();
		}
		else if(!isRepUpload && Atom.bState)
		{
			CreateClusterStruct();
		}

		AddCheckBox();
		AddCheckBoxData();	
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
			Color color = (i == 0 ? Colors.Gray : Atom.tabColor[i % 10]);
			bool isRep = (i == 0 ? false : true);
			CheckBoxItemsClu.Add(new CheckBoxItem(name, color, isRep, "Cluster"));
		}
		for (int i = 0; i <= Atom.iNbElt; i++)
		{
			string name = (i == Atom.iNbElt ? "Noise" : Atom.EltName[i]);
			Color color = (i == Atom.iNbElt ? Colors.Gray : Atom.EltColor[i]);
			bool isRep = (i == Atom.iNbElt ? false : true);
			CheckBoxItemsElt.Add(new CheckBoxItem(name, color, isRep, "Element"));
		}
		for (int i = 0; i <6; i++)
		{
			string name = "Family_" + (char)(familyChar+i);
			Color color = Atom.tabColor[i % 10];
			bool isRep = true;
			CheckBoxItemsFamily.Add(new CheckBoxItem(name, color, isRep, "Family"));
		}
	}

	private void AddCheckBoxData()
	{
		ExampleChartData.Clear();
		for (int i = 0; i < Atom.iMemSize; i++)
		{
			
		}

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
	}

	int idFamily = 0;
	public void NewFamily()
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

	}

	private void Growing()
	{
		//Init Memory Atom
		if (Atom.bState == false)
		{
			LoadAtomMemory();
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
			for(int j=0; j<= Atom.iNbElt; j++)
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
		for (int i= 0; i<= Atom.iNbElt; i++)
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
		//Init Memory Atom
		if (Atom.bState == false)
		{
			LoadAtomMemory();
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
			for (int j = 0; j <= Atom.iNbElt; j++)
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
		for (int i = 0; i <= Atom.iNbElt; i++)
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
						for (int e = 0; e <= Atom.iNbElt; e++) //Loop on elt
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
				for (int e = 0; e <= Atom.iNbElt; e++)
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
		Atom.iCluId = iOldCluId.ToArray();

		//Init Clurter struct to calculation
		CreateClusterStruct();

		//Update 3D view
		AddCheckBoxData();
	}
	
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
    }
}
