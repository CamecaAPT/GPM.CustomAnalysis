using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
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
using System.Windows.Markup;
using System.Windows.Media;
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
using Microsoft.Win32;
using Prism.Commands;
using Prism.Services.Dialogs;
using Color = System.Windows.Media.Color;

namespace GPM.CustomAnalyses.Analyses.Reconstruction3DM;

internal class Reconstruction3DMViewModel : AnalysisViewModelBase<Reconstruction3DMNode>
{	
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.Reconstruction3DM.Reconstruction3DMViewModel";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IIonDataProvider _ionDataProvider;
	private readonly IRenderDataFactory _renderDataFactory;

	private IIonDisplayInfo? _ionDisplayInfo = null;

	public ObservableCollection<IRenderData> VoltageData { get; } = new();
	public ObservableCollection<IRenderData> data3D{ get; } = new();

	public string sM1 { get; set; } = "1.8";
	public string sEBeta { get; set; } = "28";
	public string sCurvFactor { get; set; } = "1";
	public string sRotangle { get; set; } = "0";
	public string sDtcEff { get; set; } = "0.5";
	public string sTiltAngle { get; set; } = "0";
	public int iSelectedRecId { get; set; } = 0;

	private float fM1 = 1.8f;
	private float fEBeta  = 28;
	private float fCurvFactor  = 1;
	private float fRotangle  = 0;
	private float fDtcEff = 0.5f;
	private float fTiltAngle = 0;

	public ICommand UpdateCommand { get; }
	public ICommand CalculationCommand { get; }

	CAtom Atom = CustomAnalysesModule.Atom;

	Stopwatch ExecutionTime = new Stopwatch();

	private bool _displayUpdateOverlay;
	public bool DisplayUpdateOverlay
	{
		get => _displayUpdateOverlay;
		set => SetProperty(ref _displayUpdateOverlay, value);
	}

	public Reconstruction3DMViewModel(
		IAnalysisViewModelBaseServices services,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
		IRenderDataFactory renderDataFactory)
		: base(services)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
		_renderDataFactory = renderDataFactory;

		UpdateCommand = new DelegateCommand(LoadAtomMemory);
		CalculationCommand = new DelegateCommand(Calculation);

		Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
		Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

		InitVoltage();
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

	private void InitParamMenu()
	{
		fM1 = float.Parse(sM1);
		fEBeta = float.Parse(sEBeta);
		fCurvFactor = float.Parse(sCurvFactor);
		fRotangle = float.Parse(sRotangle);
		fDtcEff = float.Parse(sDtcEff);
		fTiltAngle = float.Parse(sTiltAngle);
	}

	private void InitVoltage()
	{
		VoltageData.Clear();
		Vector3[] data = new Vector3[Atom.iMemSize];
		for (int i = 0; i < Atom.iMemSize; i++)
		{
			data[i] = new Vector3(i, 0, Atom.fVDC[i]);
			//Console.WriteLine(Atom.fDPos[i, 0] + " " +  Atom.fDPos[i, 1]);
		}
		VoltageData.Add(_renderDataFactory.CreateLine(data, Colors.Black));
	}
		
		

	private void Calculation()
	{
		InitParamMenu();
		Console.WriteLine(iSelectedRecId);
		data3D.Clear();

		double R = 0;
		double vdc = 0;
		double theta = 0;
		double thetaPrime = 0;
		double Sa = 0;
		double dz = 0;
		double phi = 0;
		double D = 10e-2;

		double X; 
		double Y;

		double Xr = 0;
		double Yr = 0;
		double Zr = 0;
		double z = 0;

		List<Vector3> pos = new List<Vector3>();
		List<Vector2> posD = new List<Vector2>();
		List<double> test = new List<double>();
		for (int i=0; i<Atom.iMemSize;i++)
		{
			if (Atom.bEltId[i,0] != 255) 
			{
				X = Atom.fDPos[i, 0] * 1e-3;
				Y = Atom.fDPos[i, 1] * 1e-3;
				R = Atom.fVDC[i]/ (fEBeta* 1e9);

				thetaPrime = Math.Atan(Math.Sqrt((X * X + Y * Y) / (D)));
				theta = thetaPrime +  Math.Asin(fM1  * Math.Sin(thetaPrime));
				Sa = Math.PI * Math.Pow((R * Math.Sin(fM1 * Math.Asin(0.4 / D))), 2);
				dz = 11.6e-30 / (fDtcEff * Sa);
				phi = Math.Atan2(Y,X);
				z = z + dz;

				Xr = R * Math.Sin(theta) * Math.Cos(phi) * 1e9;
				Yr = R * Math.Sin(theta) * Math.Sin(phi) * 1e9;
				Zr = (R * (Math.Cos(theta)-1) - z) * 1e9;
				pos.Add(new Vector3((float)Xr, (float)Yr, (float)Zr));
				posD.Add(new Vector2((float)X, (float)Y));
				test.Add(thetaPrime);
			}
		}
		data3D.Add(_renderDataFactory.CreatePoints(pos.ToArray(), Colors.Red, "FT3D", true));
		UInt32 ideb1 = 0;
		UInt32 ideb2 = 3;
		int l = 0;
		try
		{
			// Écrire le contenu du fichier texte
			using (StreamWriter writer = new StreamWriter("D:\\Documents\\Desktop\\test2.txt"))
			{
				for (int i = 0; i < test.Count; i+=100)
				{
					writer.WriteLine(test[i]);
				}
			}
			Console.WriteLine("Le fichier a été enregistré avec succès.");
		}
		catch 
		{
			Console.WriteLine("Une erreur s'est produite lors de l'enregistrement du fichier");
		}

		using (BinaryWriter writer = new BinaryWriter(File.Open("D:\\Documents\\Desktop\\test.ato", FileMode.Create)))
		{
			writer.Write(ideb1);
			writer.Write(ideb2);

			for (int i=0; i<pos.Count;i++)
			{
				writer.Write((float)(pos[i].X)*10);//x
				writer.Write((float)(pos[i].Y)*10);//y
				writer.Write((float)(pos[i].Z));//z
				writer.Write((float)1);
				writer.Write((float)1); //atom.id);//clusterid
				writer.Write((float)l); //nbPulse
				writer.Write((float)test[i]);//vdc
				writer.Write((float)1); //tof
				writer.Write((float)(posD[i].X)*100);// + ((tDet * 1e2) / 2)))/3); //Px
				writer.Write((float)(posD[i].Y)*100);// + ((tDet * 1e2) / 2)))/3); //Py
				writer.Write((float)1); //ampli
				writer.Write((float)0);
				writer.Write((float)0);
				writer.Write((float)0);
				l++;
			}
			writer.Close();
		}
	}
}


