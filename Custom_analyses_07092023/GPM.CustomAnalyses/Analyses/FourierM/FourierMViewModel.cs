using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Printing;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Cameca.Extensions.Controls;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.Mvvm.Input;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using LiveCharts.Wpf.Charts.Base;
using Prism.Commands;
using Color = System.Windows.Media.Color;

namespace GPM.CustomAnalyses.Analyses.FourierM;

internal class FourierMViewModel : AnalysisViewModelBase<FourierMNode>
{	
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.FourierM.FourierMViewModel";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IIonDataProvider _ionDataProvider;
	private readonly IRenderDataFactory _renderDataFactory;

	private IIonDisplayInfo? _ionDisplayInfo = null;

	public ObservableCollection<CheckBoxItem> CheckBoxItemsElt { get; } = new();
	public ObservableCollection<IRenderData> data1DFT { get; } = new();
	public ObservableCollection<IRenderData> data3DFT { get; } = new();
	public ObservableCollection<IRenderData> dataSFT { get; } = new();

	private readonly IColorMapFactory _colorMapFactory;

	public string sSampling { get; set; } = "20";
	public string sSpaceX { get; set; } = "10";
	public string sSpaceY { get; set; } = "10";
	public string sSpaceZ { get; set; } = "10";
	public bool radioX { get; set; } = true;
	public bool radioY { get; set; } = false;
	public bool radioZ { get; set; } = false;
	public bool radio3D { get; set; } = false;
	public bool radioSFT { get; set; } = false;
	public string sThreshold { get; set; } = "0";
	public float fProgressBarValue { get; set; } = 0;

	int iSampling = 20;
	int iSpaceX = 10;
	int iSpaceY = 10;
	int iSpaceZ = 10;
	bool[] bUseElt;

	public ICommand CalculationCommand { get; }
	public ICommand FilterCommand { get; }
	public ICommand DeselectAllCluCommand { get; }
	public ICommand SelectAllCluCommand { get; }

	List<double> resultFT3D =new List<double>();
	float fThreshold = 0;
	List <Vector3> gridFT3D = new List<Vector3>();

	CAtom Atom = CustomAnalysesModule.Atom;

	Stopwatch ExecutionTime = new Stopwatch();

	private bool _displayUpdateOverlay;
	public bool DisplayUpdateOverlay
	{
		get => _displayUpdateOverlay;
		set => SetProperty(ref _displayUpdateOverlay, value);
	}

	public FourierMViewModel(
		IAnalysisViewModelBaseServices services,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
		IRenderDataFactory renderDataFactory)
		: base(services)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
		_renderDataFactory = renderDataFactory;

		CalculationCommand = new DelegateCommand(Calculation);
		FilterCommand = new DelegateCommand(FilterFT3D);
		SelectAllCluCommand = new DelegateCommand(SelectAllElt);
		DeselectAllCluCommand = new DelegateCommand(DeselectAllElt);

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
	}

	private void AddCheckBox()
	{
		CheckBoxItemsElt.Clear();
		for (int i = 0; i <= Atom.iNbElt; i++)
		{
			string name = (i == Atom.iNbElt ? "Noise" : Atom.EltName[i]);
			Color color = (i == Atom.iNbElt ? Colors.Gray : Atom.EltColor[i]);
			bool isRep = (i == Atom.iNbElt ? false : true);
			CheckBoxItemsElt.Add(new CheckBoxItem(name, color, isRep, "Elt"));
		}
	}

	private void InitMenuParameters()
	{
		iSampling = int.Parse(sSampling);
		iSpaceX = int.Parse(sSpaceX);
		iSpaceY = int.Parse(sSpaceY);
		iSpaceZ = int.Parse(sSpaceZ);

		bUseElt = new bool[CheckBoxItemsElt.Count];
		for (int i = 0; i < CheckBoxItemsElt.Count; i++)
		{
			bUseElt[i] = CheckBoxItemsElt[i].IsSelected;
		}
	}

	private void SelectAllElt()
	{
		for (int i = 0; i < Atom.iNbElt; i++)
		{
			CheckBoxItemsElt[i].IsSelected = true;
		}
	}

	private void DeselectAllElt()
	{
		foreach (var item in CheckBoxItemsElt)
		{
			item.IsSelected = false;
		}
	}

	public void Calculation()
	{
		InitMenuParameters();
		if (radioX)
		{
			data1DFT.Clear();
			ExecutionTime.Restart();
			Console.WriteLine("FT X");
			List<Vector3> grid = new List<Vector3>();
			List<Vector3> rep = new List<Vector3>();
			decimal stepX = iSpaceX / (decimal)iSampling;
			decimal a = (decimal)(iSpaceX / 2f);

			Console.Write("Grid... ");
			for (int i = 0; i < iSampling; i++)
			{
				grid.Add(new Vector3((float)a, 0, 0));
				a -= stepX;
			}
			Console.WriteLine("OK " + grid.Count);
			Console.Write("Calcul FT ... ");
			List<double> FTX = FFT3D(grid, 0, grid.Count);
			Console.WriteLine("OK");
			Console.WriteLine("FT X " + ExecutionTime.Elapsed.TotalSeconds);
			for (int j = 0; j < grid.Count; j++)
			{
				rep.Add( new Vector3(grid[j].X, 0, (float)FTX[j]));
				Console.WriteLine(rep[j].X + " "+ rep[j].Z );
			}
			data1DFT.Add(_renderDataFactory.CreateLine(rep.ToArray(), Colors.Blue));
			Console.WriteLine("filter rep  " + rep.Count);
		}
		else if (radioY)
		{
			data1DFT.Clear();
			ExecutionTime.Restart();
			Console.WriteLine("FT Y");
			List<Vector3> grid = new List<Vector3>();
			List<Vector3> rep = new List<Vector3>();
			decimal stepY = iSpaceY / (decimal)iSampling;
			decimal b = (decimal)(iSpaceY / 2f);

			Console.Write("Grid... ");
			for (int i = 0; i < iSampling; i++)
			{
				grid.Add(new Vector3(0, (float)b, 0));
				b -= stepY;
			}
			Console.WriteLine("OK " + grid.Count);
			Console.Write("Calcul FT ... ");
			List<double> FTY = FFT3D(grid,0,grid.Count);
			Console.WriteLine("OK");
			Console.WriteLine("FT Y " + ExecutionTime.Elapsed.TotalSeconds);
			for (int j = 0; j < grid.Count; j++)
			{
				rep.Add(new Vector3(grid[j].Y, 0, (float)FTY[j]));
				Console.WriteLine(rep[j].X + " " + rep[j].Z);
			}
			data1DFT.Add(_renderDataFactory.CreateLine(rep.ToArray(), Colors.Red));
			Console.WriteLine("filter rep  " + rep.Count);
		}
		else if (radioZ)
		{
			data1DFT.Clear();
			ExecutionTime.Restart();
			Console.WriteLine("FT Z");
			List<Vector3> grid = new List<Vector3>();
			List<Vector3> rep = new List<Vector3>();
			decimal stepZ = iSpaceZ / (decimal)iSampling;
			decimal c = (decimal)(iSpaceZ / 2f);

			Console.Write("Grid... ");
			for (int i = 0; i < iSampling; i++)
			{
				grid.Add(new Vector3(0, 0, (float)c));
				c -= stepZ;
			}
			Console.WriteLine("OK " + grid.Count);
			Console.Write("Calcul FT ... ");
			List<double> FTZ = FFT3D(grid,0,grid.Count);
			Console.WriteLine("OK");
			Console.WriteLine("FT Z  " + ExecutionTime.Elapsed.TotalSeconds);
			for (int j = 0; j < grid.Count; j++)
			{
				rep.Add(new Vector3(grid[j].Z, 0, (float)FTZ[j]));
				Console.WriteLine(rep[j].X + " " + rep[j].Z);
			}
			data1DFT.Add(_renderDataFactory.CreateLine(rep.ToArray(), Colors.Black));
			Console.WriteLine("filter rep  " + rep.Count);
		}
		else if (radio3D)
		{
			data3DFT.Clear();
			resultFT3D.Clear();
			ExecutionTime.Restart();
			Console.WriteLine("FT 3D");
			List<Vector3> grid = new List<Vector3>();
			List<Vector3> rep = new List<Vector3>();
			float stepX = iSpaceX / (float)iSampling;
			float a = iSpaceX / 2f;
			float  stepY = iSpaceY /(float)iSampling;
			float b = iSpaceY / 2f;
			float stepZ = iSpaceZ/(float)iSampling;
			float c = iSpaceZ / 2f;

			Console.Write("Grid... ");
			for (int i = 0; i < iSampling; i++)
			{
				b = iSpaceY / 2f;
				for (int k = 0; k < iSampling; k++)
				{
					c = iSpaceZ / 2f;
					for (int m = 0; m < iSampling; m++)
					{
						grid.Add(new Vector3(a, b, c));
						c -= stepZ;
					}
					b -= stepY;
				}
				a -= stepX;
			}
			gridFT3D = grid;
			Console.WriteLine("OK " + grid.Count);
			Console.Write("Calcul FT ... ");

			//No paralell
			//List<double> FT3D = FFT3D(grid,0,grid.Count);
			//resultFT3D = FT3D;

			//Paralell
			int iNumberOfThread = Environment.ProcessorCount * 2;
			int iThreadStep = iThreadStep = gridFT3D.Count / iNumberOfThread;
			Dictionary<int, List<double>> resultPerThread = new Dictionary<int, List<double>>();
			List<double> resultThreads = new List<double>();
			Parallel.For(0, iNumberOfThread, i =>
			{
				int iStart = i * iThreadStep;
				int iStop = (i + 1) * iThreadStep;
				iStop = (i == iNumberOfThread - 1) ? gridFT3D.Count : iStop;
				Console.WriteLine("id " + i + " start " + iStart + "    stop " + iStop);

				resultPerThread.Add(i, FFT3D(grid, iStart, iStop));
			});
			
			for (int i = 0; i < resultPerThread.Count; i++)
			{
				resultFT3D.AddRange(resultPerThread[i]);
			}
			//resultFT3D = resultThreads.ToArray();
			Console.WriteLine("Thread finish OK");

			double max = resultFT3D.Max();
			for (int i = 0; i < resultFT3D.Count; i++)
			{
				resultFT3D[i] = Math.Abs(resultFT3D[i] / max);
			}

			Console.WriteLine("OK");
			Console.WriteLine("FT 3D  " + ExecutionTime.Elapsed.TotalSeconds) ;
			for (int i=0; i < resultFT3D.Count; i++)
			{
				rep.Add(grid[i]);
			}
			data3DFT.Add(_renderDataFactory.CreatePoints(rep.ToArray(), Colors.Red, "FT3D",true));
			Console.WriteLine("filter rep  " + rep.Count);
		}
		else if (radioSFT)
		{
			dataSFT.Clear();
			resultFT3D.Clear();
			ExecutionTime.Restart();
			Console.WriteLine("FT 3D");
			List<Vector3> grid = new List<Vector3>();
			List<Vector3> rep = new List<Vector3>();
			float stepX = iSpaceX / (float)iSampling;
			float a = iSpaceX / 2f;
			float stepY = iSpaceY / (float)iSampling;
			float b = iSpaceY / 2f;
			float stepZ = iSpaceZ / (float)iSampling;
			float c = iSpaceZ / 2f;

			Console.Write("Grid... ");
			/*List<Vector6> RadialSphere = Sphere(10, iSampling);
			for (int i = 0; i < RadialSphere.Count; i++)
			{
				gridFT3D.Add(new Vector3(RadialSphere[i].X, RadialSphere[i].Y, RadialSphere[i].Z));
			}*/

			List<Vector3> points = new List<Vector3>();
			Dictionary<int,List<Vector3>> map = new Dictionary<int,List<Vector3>>();
			int id = 0;
			for (float theta = 0; theta <= Math.PI; theta += (float)(Math.PI / iSampling))
			{
				for (float phi = 0; phi <= 2 * Math.PI; phi += (float)(Math.PI / iSampling))
				{
					points.Clear();
					for(float i = 2; i < iSpaceX; i+= (float)(iSpaceX *1f/ iSampling))
					{
						float x = (float)(i * Math.Sin(theta) * Math.Cos(phi));
						float y = (float)(i * Math.Sin(theta) * Math.Sin(phi));
						float z = (float)(i * Math.Cos(theta));
						points.Add(new Vector3(x, y, z));
					}
					map[id] = new List<Vector3>();
					map[id].AddRange(points);
					id++;
				}
			}

			Console.WriteLine("OK " + gridFT3D.Count);
			Console.Write("Calcul FT ... ");
			//Paralell
			int iNumberOfThread = Environment.ProcessorCount * 2;
			int iThreadStep = iThreadStep = map.Count / iNumberOfThread;
			List<Vector3> maxThetaPhiPos = new List<Vector3>();
			List<double> maxThetaPhi = new List<double>();
			Parallel.For(0, iNumberOfThread, i =>
			{
				int iStart = i * iThreadStep;
				int iStop = (i + 1) * iThreadStep;
				iStop = (i == iNumberOfThread - 1) ? map.Count : iStop;
				Console.WriteLine("id " + i + " start " + iStart + "    stop " + iStop);
				for(int j = iStart; j < iStop; j++)
				{
					List<double> FTThethaPhi = new List<double>();
					FTThethaPhi.AddRange(FFT3D(map[j], 0, map[j].Count));
					int index = FTThethaPhi.FindIndex(x =>x ==FTThethaPhi.Max());
					//Console.WriteLine(j + "   " +index + " " + FTThethaPhi.Max() + " " + map[j][index]);
					maxThetaPhi.Add(FTThethaPhi.Max());
					maxThetaPhiPos.Add(map[j][index]);
				}
			});
			Console.WriteLine(maxThetaPhi.Count);
			Console.WriteLine(maxThetaPhiPos.Count);

			Console.WriteLine("Thread finish OK");
			double max = maxThetaPhi.Max();
			for (int i = 0; i < maxThetaPhi.Count; i++)
			{
				resultFT3D.Add(Math.Abs(maxThetaPhi[i] / max));
			}
		
			for (int i = 0; i < maxThetaPhiPos.Count; i++)
			{
				rep.Add(maxThetaPhiPos[i]);
			}
			gridFT3D = rep;
			data3DFT.Add(_renderDataFactory.CreatePoints(rep.ToArray(), Colors.Red, "FT3D", true));
			Console.WriteLine("OK");
			Console.WriteLine("FT 3D  " + ExecutionTime.Elapsed.TotalSeconds);

			List<Vector3> repSFT = new List<Vector3>();
			float thetaProj = 0;
			for (int i = 0; i < rep.Count-1; i++)
			{
				thetaProj = (float)Math.Atan2(Math.Sqrt(rep[i].X * rep[i].X + rep[i].Y * rep[i].Y), Math.Abs(rep[i].Z));
				repSFT.Add(new Vector3(2f * thetaProj * rep[i].X, 2f * thetaProj * rep[i].Y, 0));
			}

			float[] histo2D = new float[120 * 120];
			List<int> inGrid = new List<int>();
			int cpt = 0;
			for(int i = -60; i < 60; i++)
			{
				for(int j = -60; j <60; j++)
				{
					inGrid = Enumerable.Range(0, repSFT.Count).Where(v => repSFT[v].X >= i && repSFT[v].X < i+1 && repSFT[v].Y >= j && repSFT[v].Y < j+1).ToList();
					double mean = 0;
					foreach (int index in inGrid)
					{
						mean += resultFT3D[index];
					}
					mean = mean / inGrid.Count;
					histo2D[cpt] = (float)mean;
					cpt++;
					//Console.WriteLine(inGrid.Count);
				}			
			}
			//var colorMap = _colorMapFactory.GetPresetColorMap(ColorMapPreset.Bright);
			ReadOnlyMemory2D<float> test = new ReadOnlyMemory2D<float>(histo2D, 120,120);
			dataSFT.Add(_renderDataFactory.CreateHistogram2D(test,new Vector2(1,1)));

			//dataSFT.Add(_renderDataFactory.CreatePoints(repSFT.ToArray(), Colors.Blue, "FT3D", true));
		}
		else
		{
			Console.WriteLine("Error");
		}
	}

	public List<double> FFT3D(List<Vector3> grid, int start, int stop)
	{
		//Complex j = new Complex(0, 1);
		Complex FF;
		List<double> val = new List<double>();
		double angle;
		for(int i = start; i<stop; i++)
		{
			FF = 0;
			for (int jj = 0; jj < Atom.iMemSize; jj++)
			{
				int idElt = (Atom.bEltId[jj, 0] == 255 ? Atom.iNbElt : Atom.bEltId[jj, 0]);
				if (bUseElt[idElt])
				{
					angle = -2 * 3.14159f * (grid[i].X * (Atom.fPos[jj, 0]) + grid[i].Y * (Atom.fPos[jj, 1]) + grid[i].Z * (Atom.fPos[jj, 2]));
					FF += Atom.fMass[jj, 0] * new Complex(Math.Cos(angle), Math.Sin(angle));
					//FF += Atom.fMass[jj, 0] * (Complex.Exp(2 * 3.14159 * j * (pos.X * (Atom.fPos[jj, 0]) + pos.Y * (Atom.fPos[jj, 1]) + pos.Z * (Atom.fPos[jj, 2]))));
					//FF +=  (Complex.Exp(2 * 3.14159 * j * (pos.X * (grid[jj].X+1) + pos.Y * (grid[jj].Y + 1) + pos.Z * (grid[jj].Z + 1))));
				}
			}
			val.Add(FF.Real);
		}
		return val;
	}

	public void FilterFT3D()
	{
		data3DFT.Clear();
		List<Vector3> rep = new List<Vector3>();
		fThreshold = float.Parse(sThreshold) / 100f;
		for (int i = 0; i < resultFT3D.Count; i++)
		{
			if (resultFT3D[i] > fThreshold)
			{
				if (radio3D || radioSFT)
				{
					rep.Add(gridFT3D[i]);
				}
			}
		}
		data3DFT.Add(_renderDataFactory.CreatePoints(rep.ToArray(), Colors.Red, "FT3D", true));
	}

	public float[] GetColumn(float[,] matrix, int columnNumber)
	{
		return Enumerable.Range(0, matrix.GetLength(0))
				.Select(x => matrix[x, columnNumber])
				.ToArray();
	}

}


