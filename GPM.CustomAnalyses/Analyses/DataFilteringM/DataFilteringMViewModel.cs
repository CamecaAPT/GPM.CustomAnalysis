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
using Prism.Commands;
using Color = System.Windows.Media.Color;

namespace GPM.CustomAnalyses.Analyses.DataFilteringM;

internal class DataFilteringMViewModel : AnalysisViewModelBase<DataFilteringMNode>
{
	public struct AtomStruct
	{
		public int id;
		public Vector3 pos;
		public float mass;
		public Vector2 pos_det;
		public float vdc;
		public int multiplicity;
		public int eltId;
		public int cluId;
	}
	public AtomStruct[] atoms;
	public int iNumberAtoms;

	public struct EltStruct
	{
		public int id;
		public string name;
		public Color color;
	}
	public EltStruct[] elt;
	public int iNumberElt;

	public int iNumberCluster;

	public const string UniqueId = "GPM.CustomAnalyses.Analyses.DataFilteringM.DataFilteringMViewModel";
	public static List<ulong> outIndices = new List<ulong>();

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IIonDataProvider _ionDataProvider;
	private readonly IRenderDataFactory _renderDataFactory;

	private IIonDisplayInfo? _ionDisplayInfo = null;

	public string MinLimit { get; set; } = "0";
	public string MaxLimit { get; set; } = "10";
	public int TabSelectedIndex { get; set; } = 0;

	public ObservableCollection<IRenderData> MassHistogramData { get; } = new();
	public ObservableCollection<IRenderData> MultiplicityHistogramData { get; } = new();
	public ICommand LoadCommand { get; }
	public ICommand FilterCommand { get; }

	CAtom Atom = CustomAnalysesModule.Atom;

	Stopwatch ExecutionTime = new Stopwatch();

	private bool _displayUpdateOverlay;
	public bool DisplayUpdateOverlay
	{
		get => _displayUpdateOverlay;
		set => SetProperty(ref _displayUpdateOverlay, value);
	}

	public DataFilteringMViewModel(
		IAnalysisViewModelBaseServices services,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
		IRenderDataFactory renderDataFactory)
		: base(services)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
		_renderDataFactory = renderDataFactory;

		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

		LoadCommand = new DelegateCommand(LoadAtomMemory);
		FilterCommand = new DelegateCommand(FilterAtoms);
		//PlotHisto(histoMass(),MassHistogramData,0);
		//PlotHisto(histoMulti(), MultiplicityHistogramData,1);
	}

	private List<int> id = new List<int>();
	private List<float> mass = new List<float>();
	private List<Vector3> pos_init = new List<Vector3>();
	private List<byte> name = new List<byte>();
	private List<string> impact_name;
	private List<Int32> multiplicity = new List<Int32>();
	private List<float> vdc = new List<float>();
	private List<Vector2> pos_det = new List<Vector2>();

	public async void LoadAtomMemory()
	{
		Task<IIonData> IonDataTask = Node.GetParentIonData();
		IIonData IonDataMemory = await IonDataTask;
		IIonDisplayInfo IonDisplayInfoMemory = _ionDisplayInfoProvider.Resolve(Node.GetParentId());
		Console.WriteLine("Create Atom data memory ...");
		mass.Clear();
		pos_init.Clear();
		name.Clear();
		multiplicity.Clear();
		vdc.Clear();
		pos_det.Clear();
		int idFin =0;

		// Extract ApSuite data
		foreach (var chunk in IonDataMemory.CreateSectionDataEnumerable(IonDataSectionName.Mass, IonDataSectionName.Position, IonDataSectionName.IonType, IonDataSectionName.Multiplicity, IonDataSectionName.Voltage, IonDataSectionName.DetectorCoordinates))
		{
			var masses = chunk.ReadSectionData<float>(IonDataSectionName.Mass);
			var positions = chunk.ReadSectionData<Vector3>(IonDataSectionName.Position);
			var ionTypes = chunk.ReadSectionData<byte>(IonDataSectionName.IonType);
			var detector_position = chunk.ReadSectionData<Vector2>(IonDataSectionName.DetectorCoordinates);
			var voltage = chunk.ReadSectionData<float>(IonDataSectionName.Voltage);
			var multipleEvent = chunk.ReadSectionData<Int32>(IonDataSectionName.Multiplicity);
			for (int index = 0; index < chunk.Length; index++)
			{
				id.Add(idFin+index);
				mass.Add(masses.Span[index]);
				pos_init.Add(positions.Span[index]);
				name.Add(ionTypes.Span[index]);
				pos_det.Add(detector_position.Span[index]);
				vdc.Add(voltage.Span[index]);
				multiplicity.Add(multipleEvent.Span[index]);
			}
			idFin = id.Count();
		}
		iNumberAtoms = pos_init.Count;
		atoms = new AtomStruct[iNumberAtoms];
		
		for (int i=0;i<iNumberAtoms; i++) 
		{
			atoms[i].id = id[i];
			atoms[i].pos = pos_init[i];
			atoms[i].mass = mass[i];
			atoms[i].pos_det = pos_det[i];
			atoms[i].vdc = vdc[i];
			atoms[i].multiplicity = multiplicity[i];
			atoms[i].eltId = name[i];
		}

		string[] EltName = IonDataMemory.Ions.Select(o => o.Name).ToArray();
		Color[] EltColor = IonDataMemory.Ions.Select(x => IonDisplayInfoMemory.GetColor(x)).ToArray();
		iNumberElt = EltName.Length;

		elt = new EltStruct[iNumberElt];
		for(int i=0;i<iNumberElt;i++)
		{
			elt[i].id = i;
			elt[i].name = EltName[i];
			elt[i].color = EltColor[i];
		}

		Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", iNumberAtoms, iNumberElt, iNumberCluster);

		PlotHisto(histoMass(), MassHistogramData , 0);
		PlotHisto(histoMulti(), MultiplicityHistogramData, 1);
	}

	public void FilterAtoms()
	{
		outIndices.Clear();
		float min = float.Parse(MinLimit);
		float max = float.Parse(MaxLimit);
		if(TabSelectedIndex == 0 || TabSelectedIndex == 1)
		{
			for (int i = 0; i < iNumberAtoms; i++)
			{
				if (atoms[i].mass >= min && atoms[i].mass <= max)
				{
					outIndices.Add((ulong)atoms[i].id);
				}
			}
		}
		else if (TabSelectedIndex == 2)
		{
			for (int i = 0; i < iNumberAtoms; i++)
			{
				if (atoms[i].multiplicity >= min && atoms[i].multiplicity <= max)
				{
					outIndices.Add((ulong)atoms[i].id);
				}
			}
		}
	}

	public float[,] histoMass()
	{
		float fSampling = 0.01f;
		//float fMax = GetColumn(Atom.fMass, 0).Max()+10;
		float fMax = atoms.Max(x => x.mass) + 10;
		int idElt;
		float[,] hist;

		hist = new float[(int)Math.Ceiling((fMax / fSampling)), iNumberElt+2];
		//Init Mass spectrum
		for (int i = 0; i < fMax / fSampling; i++)
		{
			hist[i, 0] = (float)Decimal.Truncate((decimal)i * (decimal)fSampling * 100)/100;
		}

		for (int i = 0; i < iNumberAtoms; i++)
		{
			idElt = (atoms[i].eltId == 255 ? iNumberElt + 1 : atoms[i].eltId + 1);
			hist[(int)(atoms[i].mass / fSampling), idElt]++;
		}
		return hist;
	}

	public float[,] histoMulti()
	{
		float[,] hist;
		hist = new float[16,  2];
		//Init Multiplicity histo
		for (int i = 0; i < 16; i++)
		{
			hist[i, 0] = (float)i;
		}

		for (int i = 0; i < iNumberAtoms; i++)
		{
			hist[(int)(atoms[i].multiplicity), 1]++;	
		}
		return hist;
	}

	public bool PlotHisto(float[,] data, ObservableCollection<IRenderData> chart, int idCal)
	{
		float[] fX = GetColumn(data, 0);
		float[] fY;
		Vector2[] vec = new Vector2[fX.Length];
		Color color;
		List<IChart2DSlice> slices;

		if (idCal == 0)
		{
			for (int i = 1; i <= iNumberElt + 1; i++)
			{
				color = (i == iNumberElt + 1 ? Colors.Gray : elt[i - 1].color);
				fY = GetColumn(data, i);
				slices = new List<IChart2DSlice> { new Chart2DSlice(fX.Min(), fX.Max(), color) };
				for (int j = 0; j < fX.Length; j++)
				{
					vec[j] = new Vector2(fX[j], fY[j]);
				}
				ReadOnlyMemory<Vector2> dataHisto = new ReadOnlyMemory<Vector2>(vec);
				chart.Add(_renderDataFactory.CreateHistogram(dataHisto, color, 1, 0, slices));
			}
		}
		else if (idCal == 1)
		{
			color = Colors.Blue;
			fY = GetColumn(data, 1);
			slices = new List<IChart2DSlice> { new Chart2DSlice(fX.Min()-0.5f, fX.Max(), color) };
			for (int j = 0; j < fX.Length; j++)
			{
				vec[j] = new Vector2(fX[j] - 0.5f, fY[j]);
			}
			ReadOnlyMemory<Vector2> dataHisto = new ReadOnlyMemory<Vector2>(vec);
			chart.Add(_renderDataFactory.CreateHistogram(dataHisto, color, 1, 0, slices));
		}
		return true;
	}

	public float[] GetColumn(float[,] matrix, int columnNumber)
	{
		return Enumerable.Range(0, matrix.GetLength(0)).Select(x => matrix[x, columnNumber]).ToArray();
	}
}


