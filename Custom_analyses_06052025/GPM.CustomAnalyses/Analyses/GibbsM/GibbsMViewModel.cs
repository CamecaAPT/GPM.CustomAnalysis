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
using static GPM.CustomAnalyses.varGlob;
using static GPM.CustomAnalyses.fctGlob;
using System.Security.Claims;

namespace GPM.CustomAnalyses.Analyses.GibbsM;

internal class GibbsMViewModel : AnalysisViewModelBase<GibbsMNode>
{
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.GibbsM.GibbsMViewModel";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IIonDataProvider _ionDataProvider;
	private readonly IRenderDataFactory _renderDataFactory;

	private IIonDisplayInfo? _ionDisplayInfo = null;

	public ObservableCollection<CheckBoxItem> CheckBoxItemsElt { get; } = new();
	public ObservableCollection<IRenderData> dataIntProfil { get; } = new();

	private readonly IColorMapFactory _colorMapFactory;

	public int ProfilTypeIndex { get; set; } = 0;
	public string ClassSize { get; set; } = "0.25";
	public string MovingStep { get; set; } = "0.25";
	public string LowLimit { get; set; } = "0";
	public string HighLimit { get; set; } = "0";
	public string ExcessAto { get; set; } = "0";
	public string GibbsExcess { get; set; } = "0";
	public string Surface { get; set; } = "0";
	public string Efficiency { get; set; } = "0.52";

	float fClassSize = 0.25f;
	float fMouvingStep = 0.25f;
	float fLowLimit = 0;
	float fHighLimit = 0;
	int iExcessAto = 0;
	float fGibbsExcess = 0;
	int iProfilTypeId = 0;
	float fSurface = 0;
	float fEfficiency = 0.52f;
	bool[] bUseElt;

	double fA1 = 0;
	double fB1 = 0;
	double fA2 = 0;
	double fB2 = 0;
	double fA3 = 0;
	double fB3 = 0;
	double[] fPx = new double[7];
	double[] fPy = new double[7];

	public ICommand LoadAtomMemoryCommand { get; }
	public ICommand UpdateRepCommand { get; }
	public ICommand PlotProfilCommand { get; }
	public ICommand DeselectAllCluCommand { get; }
	public ICommand SelectAllCluCommand { get; }
	public ICommand AutoGibbsCommand { get; }

	List<double> resultFT3D = new List<double>();
	float fThreshold = 0;
	List<Vector3> gridFT3D = new List<Vector3>();

	SAtom Atom = new SAtom();
	Elt elt = new Elt();

	List<float> mass = new List<float>();
	List<Vector3> pos_init = new List<Vector3>();
	List<byte> ionType = new List<byte>();

	Stopwatch ExecutionTime = new Stopwatch();

	private bool _displayUpdateOverlay;
	public bool DisplayUpdateOverlay
	{
		get => _displayUpdateOverlay;
		set => SetProperty(ref _displayUpdateOverlay, value);
	}

	public GibbsMViewModel(
		IAnalysisViewModelBaseServices services,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
		IRenderDataFactory renderDataFactory)
		: base(services)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
		_renderDataFactory = renderDataFactory;

		//UpdateRepCommand = new DelegateCommand(UpdateRep);
		LoadAtomMemoryCommand = new DelegateCommand(LoadAtomMemory);
		PlotProfilCommand = new DelegateCommand(CalculProfil);
		SelectAllCluCommand = new DelegateCommand(SelectAllElt);
		DeselectAllCluCommand = new DelegateCommand(DeselectAllElt);
		AutoGibbsCommand = new DelegateCommand(AutoGibbs);

		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

		AddCheckBox();
	}

	protected override void OnAdded(ViewModelAddedEventArgs eventArgs)
	{
		// Keep this line for base class add work
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
		base.OnAdded(eventArgs);
		LoadAtomMemory();
	}

	IIonData IonDataMemory;
	public async void LoadAtomMemory()
	{
		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

		Task<IIonData> IonDataTask = Node.GetIonData1();
		IonDataMemory = await IonDataTask;
		IIonDisplayInfo IonDisplayInfoMemory = Node.GetIonDisplayInfo();
		Console.WriteLine("Create Atom data memory ...");

		mass = await LoadAtoMemory<float>(IonDataMemory, IonDataSectionName.Mass);
		pos_init = await LoadAtoMemory<Vector3>(IonDataMemory, IonDataSectionName.Position);
		ionType = await LoadAtoMemory<byte>(IonDataMemory, IonDataSectionName.IonType);
		Atom.iMemSize = mass.Count;

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

		Atom.bState = true;

		Console.WriteLine("Atom memory size = {0}    NbElement = {1}  \n", Atom.iMemSize, elt.iNbElt);

		AddCheckBox();
	}

	private void AddCheckBox()
	{
		CheckBoxItemsElt.Clear();
		for (int i = 0; i <= elt.iNbElt; i++)
		{
			string name = (i == elt.iNbElt ? "Noise" : elt.Name[i]);
			Color color = (i == elt.iNbElt ? Colors.Gray : elt.Color[i]);
			bool isRep = (i == elt.iNbElt ? false : true);
			CheckBoxItemsElt.Add(new CheckBoxItem(name, color, isRep, "Elt"));
		}
	}

	private void InitMenuParameters()
	{
		bUseElt = new bool[CheckBoxItemsElt.Count];
		fClassSize = float.Parse(ClassSize);
		fMouvingStep = float.Parse(MovingStep);
		fLowLimit = float.Parse(LowLimit);
		fHighLimit = float.Parse(HighLimit);
		iExcessAto = int.Parse(ExcessAto);
		fGibbsExcess = float.Parse(GibbsExcess);
		iProfilTypeId = ProfilTypeIndex;
		fSurface = float.Parse(Surface);
		fEfficiency = float.Parse(Efficiency);
		for (int i = 0; i < CheckBoxItemsElt.Count; i++)
		{
			bUseElt[i] = CheckBoxItemsElt[i].IsSelected;
		}
	}

	private void RefreshValue()
	{
		ExcessAto = new string(ExcessAto);
		RaisePropertyChanged(nameof(ExcessAto));
		GibbsExcess = new string(GibbsExcess);
		RaisePropertyChanged(nameof(GibbsExcess));
	}

	private void SelectAllElt()
	{
		for (int i = 0; i < elt.iNbElt; i++)
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

	int idEltSel = 0;
	float[,] integralProfil;
	float[,] compo;
	float[,] compoPercent;
	public void CalculProfil()
	{
		InitMenuParameters();
		compo = CompoProfil();
		compoPercent = CompoProfilPercent(compo);
		List<Vector3> data = new List<Vector3>();
		dataIntProfil.Clear();
		PlotProfil();
	}

	public void AutoGibbs()
	{
		InitMenuParameters();
		FitLine(idEltSel);
		SetPoint();
		dataIntProfil.Clear();
		PlotProfil();
		PlotFit();
		//Console.WriteLine(fPx[0] * fMouvingStep + " " + fPy[0] + "; " + fPx[1] * fMouvingStep + " " + fPy[1] + "; " + fPx[2] * fMouvingStep + " " + fPy[2] + " " + fPx[3] * fMouvingStep + " " + fPy[3] + "; " + fPx[4] * fMouvingStep + " " + fPy[4] + "; " + fPx[5] * fMouvingStep + " " + fPy[5]);
		double xIntersection1 = (fB2 - fB1) / ((fA1 - fA2) / fMouvingStep);
		double yIntersection1 = fA1 / fMouvingStep * xIntersection1 + fB1;

		double xIntersection2 = (fB3 - fB2) / ((fA2 - fA3) / fMouvingStep);
		double yIntersection2 = fA2 / fMouvingStep * xIntersection2 + fB2;

		double midX = (xIntersection2 + xIntersection1) / 2.0;
		double midY1 = fA1 / fMouvingStep * midX + fB1;
		double midY2 = fA3 / fMouvingStep * midX + fB3;

		Console.WriteLine("interrrrrrrrrrr" + xIntersection1 + "  " + xIntersection2 + "  " + midX + "   " + midY1 + "    " + midY2);

		ExcessAto = ((int)((midY2 - midY1) / (fClassSize / fMouvingStep))).ToString();
		GibbsExcess = (int.Parse(ExcessAto) / (fSurface * fEfficiency)).ToString() ;
		RefreshValue();
		Console.WriteLine(ExcessAto + "  " + GibbsExcess);
	}

	//Compo Profil
	public float[,] CompoProfil()
	{
		float minZ = GetColumn(Atom.fPos, 2).Min();
		float maxZ = GetColumn(Atom.fPos, 2).Max();
		int nbStep = (int)((maxZ - minZ) / fMouvingStep);
		int nbPoint = (int)(fClassSize / fMouvingStep);
		float[,] compoTab = new float[elt.iNbElt, nbStep + 1];
		int[] nbAto = new int[elt.iNbElt];
		int idElt;
		Console.WriteLine("min " + minZ + "max " + maxZ + " nbEltTot: " + elt.iNbElt + "   ; nbStepTot: " + nbStep);
		for (int i = 0; i < Atom.iMemSize; i++)
		{
			float minLimit = (Atom.fPos[i, 2] - minZ);
			int idPoint = (int)(minLimit / fMouvingStep);
			idElt = Atom.bEltId[i, 0];
			if (idElt != 255)
			{
				if (idPoint >= 0 && idPoint <= nbStep)
				{
					for (int j = 0; j < nbPoint; j++)
					{
						if (idPoint + j < compoTab.GetLength(1))
						{
							compoTab[idElt, idPoint + j]++;
						}
					}
				}
				nbAto[idElt]++;
			}
		}
		for (int i = 0; i < nbAto.Length; i++)
		{
			Console.WriteLine(elt.Name[i] + "  " + nbAto[i]);
		}
		return compoTab;
	}

	public float[,] CompoProfilPercent(float[,] compoTab)
	{
		//percent
		float minZ = GetColumn(Atom.fPos, 2).Min();
		float maxZ = GetColumn(Atom.fPos, 2).Max();
		int nbStep = (int)((maxZ - minZ) / fMouvingStep);
		float[,] result = new float[elt.iNbElt, nbStep + 1];
		for (int i = 0; i < compoTab.GetLength(1); i++)
		{
			float totAto = 0;
			for (int j = 0; j < compoTab.GetLength(0); j++)
			{
				totAto += compoTab[j, i];
			}

			for (int j = 0; j < compoTab.GetLength(0); j++)
			{
				result[j, i] = compoTab[j, i] * 100f / totAto;
			}
		}
		return result;
	}

	public float[,] IntegralProfil(float[,] compoTab)
	{
		float minZ = GetColumn(Atom.fPos, 2).Min();
		float maxZ = GetColumn(Atom.fPos, 2).Max();
		int nbStep = (int)((maxZ - minZ) / fMouvingStep);
		float[,] result = new float[elt.iNbElt, nbStep + 1];

		for (int elt = 0; elt < compoTab.GetLength(0); elt++)
		{
			for (int clas = 0; clas < compoTab.GetLength(1); clas++)
			{
				if (clas > 0)
				{
					result[elt, clas] = compoTab[elt, clas] + result[elt, clas - 1];
				}
				else
				{
					result[elt, clas] = compoTab[elt, clas];
				}

			}
		}
		return result;
	}

	public void FitLine(int idEltSelect)
	{
		List<double> xTh = new List<double>();
		List<double> yTh = new List<double>();
		List<double> yCal = new List<double>();

		//Line 1
		for (int i = 0; i < (int)(fLowLimit / fMouvingStep) - 5; i++)
		{
			xTh.Add(i);
			yTh.Add(integralProfil[idEltSelect, i]);
		}
		fA1 = (yTh[yTh.Count - 1] - yTh[0]) / (xTh[xTh.Count - 1] - xTh[0]);
		fB1 = yTh[yTh.Count - 1] - fA1 * xTh[xTh.Count - 1];
		yCal = CalculThLine(xTh, fA1, fB1);

		for (int j = 0; j < 200; j++)
		{
			//A
			int nbIter = 5000;
			double epsilon = 0.01;
			double deltaErr = 100000;
			double oldDeltaErr = 0;
			double oldErr = CalcErr(yTh, yCal);
			double newErr = 0;
			int maxDir = 0;
			bool dir = true;
			int i = 0;
			double step = 10;
			while (i < nbIter && deltaErr > epsilon)
			{
				if (dir)
				{
					fA1 = fA1 + step;
				}
				else
				{
					fA1 = fA1 - step;
				}
				yCal = CalculThLine(xTh, fA1, fB1);
				newErr = CalcErr(yTh, yCal);
				deltaErr = Math.Abs(oldErr - newErr);

				if (oldDeltaErr < deltaErr)
				{
					maxDir++;
				}
				oldDeltaErr = deltaErr;
				oldErr = newErr;

				if (maxDir > 2)
				{
					maxDir = 0;
					dir = !dir;
					step = step / 2.0;
				}
				i++;
			}

			//B
			deltaErr = 100000;
			oldDeltaErr = 0;
			newErr = 0;
			i = 0;
			step = 10;
			while (i < nbIter && deltaErr > epsilon)
			{
				if (dir)
				{
					fB1 = fB1 + step;
				}
				else
				{
					fB1 = fB1 - step;
				}
				yCal = CalculThLine(xTh, fA1, fB1);
				newErr = CalcErr(yTh, yCal);
				deltaErr = Math.Abs(oldErr - newErr);

				if (oldDeltaErr < deltaErr)
				{
					maxDir++;
				}
				oldDeltaErr = deltaErr;
				oldErr = newErr;

				if (maxDir > 2)
				{
					maxDir = 0;
					dir = !dir;
					step = step / 2.0;
				}
				i++;
			}
		}

		//LINE 2
		xTh.Clear();
		yTh.Clear();
		yCal.Clear();
		for (int i = (int)(fLowLimit / fMouvingStep); i < (int)(fHighLimit / fMouvingStep); i++)
		{
			xTh.Add(i);
			yTh.Add(integralProfil[idEltSelect, i]);
		}

		fA2 = (yTh[yTh.Count - 1] - yTh[0]) / (xTh[xTh.Count - 1] - xTh[0]);
		fB2 = yTh[yTh.Count - 1] - fA2 * xTh[xTh.Count - 1];
		yCal = CalculThLine(xTh, fA2, fB2);

		for (int j = 0; j < 200; j++)
		{
			//A
			int nbIter = 5000;
			double epsilon = 0.01;
			double deltaErr = 100000;
			double oldDeltaErr = 0;
			double oldErr = CalcErr(yTh, yCal);
			double newErr = 0;
			int maxDir = 0;
			bool dir = true;
			int i = 0;
			double step = 10;

			//B
			deltaErr = 100000;
			oldDeltaErr = 0;
			newErr = 0;
			i = 0;
			step = 100;
			while (i < nbIter && deltaErr > epsilon)
			{
				if (dir)
				{
					fB2 = fB2 + step;
				}
				else
				{
					fB2 = fB2 - step;
				}
				yCal = CalculThLine(xTh, fA2, fB2);
				newErr = CalcErr(yTh, yCal);
				deltaErr = Math.Abs(oldErr - newErr);

				if (oldDeltaErr < deltaErr)
				{
					maxDir++;
				}
				oldDeltaErr = deltaErr;
				oldErr = newErr;

				if (maxDir > 2)
				{
					maxDir = 0;
					dir = !dir;
					step = step / 2.0;
				}
				i++;
			}
		}

		//LINE 3
		xTh.Clear();
		yTh.Clear();
		yCal.Clear();
		for (int i = (int)(fHighLimit / fMouvingStep) + 5; i < integralProfil.GetLength(1) - 3; i++)
		{
			xTh.Add(i);
			yTh.Add(integralProfil[idEltSelect, i]);
		}
		fA3 = (yTh[yTh.Count - 1] - yTh[0]) / (xTh[xTh.Count - 1] - xTh[0]);
		fB3 = yTh[yTh.Count - 1] - fA3 * xTh[xTh.Count - 1];
		yCal = CalculThLine(xTh, fA3, fB3);

		for (int j = 0; j < 200; j++)
		{
			//A
			int nbIter = 5000;
			double epsilon = 0.01;
			double deltaErr = 100000;
			double oldDeltaErr = 0;
			double oldErr = CalcErr(yTh, yCal);
			double newErr = 0;
			int maxDir = 0;
			bool dir = true;
			int i = 0;
			double step = 10;
			while (i < nbIter && deltaErr > epsilon)
			{
				if (dir)
				{
					fA3 = fA3 + step;
				}
				else
				{
					fA3 = fA3 - step;
				}
				yCal = CalculThLine(xTh, fA3, fB3);
				newErr = CalcErr(yTh, yCal);
				deltaErr = Math.Abs(oldErr - newErr);


				if (oldDeltaErr < deltaErr)
				{
					maxDir++;
				}
				oldDeltaErr = deltaErr;
				oldErr = newErr;

				if (maxDir > 2)
				{
					maxDir = 0;
					dir = !dir;
					step = step / 2.0;
				}
				i++;
			}

			//B
			deltaErr = 100000;
			oldDeltaErr = 0;
			newErr = 0;
			i = 0;
			step = 10;
			while (i < nbIter && deltaErr > epsilon)
			{
				if (dir)
				{
					fB3 = fB3 + step;
				}
				else
				{
					fB3 = fB3 - step;
				}
				yCal = CalculThLine(xTh, fA3, fB3);
				newErr = CalcErr(yTh, yCal);
				deltaErr = Math.Abs(oldErr - newErr);

				if (oldDeltaErr < deltaErr)
				{
					maxDir++;
				}
				oldDeltaErr = deltaErr;
				oldErr = newErr;

				if (maxDir > 2)
				{
					maxDir = 0;
					dir = !dir;
					step = step / 2.0;
				}
				i++;
			}
		}
	}

	public void SetPoint()
	{
		fPx[0] = 0;
		fPy[0] = (float)fB1;
		fPx[1] = integralProfil.GetLength(1) - 2;
		fPy[1] = (float)(fA1 * fPx[1] + fB1);

		fPx[2] = (int)(fLowLimit / fMouvingStep) - 5;
		fPy[2] = (float)(fA2 * fPx[2] + fB2);
		fPx[3] = (int)(fHighLimit / fMouvingStep) + 5;
		fPy[3] = (float)(fA2 * fPx[3] + fB2);

		fPx[4] = 0 + 1;
		fPy[4] = (float)fB3;
		fPx[5] = integralProfil.GetLength(1) - 1;
		fPy[5] = (float)(fA3 * fPx[5] + fB3);
	}

	public static List<double> CalculThLine(List<double> x, double A, double B)
	{
		List<double> y = new List<double>();

		for (int i = 0; i < x.Count; i++)
		{
			y.Add(A * x[i] + B);
		}
		return y;
	}

	public static double CalcErr(List<double> yExp, List<double> yTh)
	{
		double err = 0;

		for (int i = 0; i < yExp.Count; i++)
		{
			err += ((yExp[i] - yTh[i]) * (yExp[i] - yTh[i]));
		}

		return err;
	}

	public float[] GetColumn(float[,] matrix, int columnNumber)
	{
		return Enumerable.Range(0, matrix.GetLength(0))
				.Select(x => matrix[x, columnNumber])
				.ToArray();
	}

	public void PlotProfil()
	{
		List<Vector3> data = new List<Vector3>();
		for (int i = 0; i < CheckBoxItemsElt.Count; i++)
		{
			data.Clear();
			if (CheckBoxItemsElt[i].IsSelected)
			{
				idEltSel = i;
				if (iProfilTypeId == 0)
				{
					integralProfil = IntegralProfil(compo);
					for (int j = 0; j < compo.GetLength(1); j++)
					{
						data.Add(new Vector3(j * fMouvingStep, 0, integralProfil[i, j]));
					}
				}
				else if (iProfilTypeId == 1)
				{
					integralProfil = IntegralProfil(compoPercent);
					for (int j = 0; j < compoPercent.GetLength(1); j++)
					{
						data.Add(new Vector3(j * fMouvingStep, 0, integralProfil[i, j]));
					}
				}
				dataIntProfil.Add(_renderDataFactory.CreateLine(data.ToArray(), CheckBoxItemsElt[i].Color, thickness: 2, name: CheckBoxItemsElt[i].Caption, isVisible: true));
			}
		}
	}

	public void PlotFit()
	{
		List<Vector3> data = new List<Vector3>();
		for (int i = 0; i < 3; i++)
		{
			data.Clear();
			data.Add(new Vector3((float)fPx[2 * i] * fMouvingStep, 0, (float)fPy[2 * i]));
			data.Add(new Vector3((float)fPx[2 * i + 1] * fMouvingStep, 0, (float)fPy[2 * i + 1]));
			dataIntProfil.Add(_renderDataFactory.CreateLine(data.ToArray(), Colors.Black, thickness: 2, name: "Line_" + i, isVisible: true));
		}
	}
}


