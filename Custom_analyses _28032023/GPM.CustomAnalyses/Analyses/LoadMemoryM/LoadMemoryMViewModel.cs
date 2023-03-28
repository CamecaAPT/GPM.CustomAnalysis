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

namespace GPM.CustomAnalyses.Analyses.LoadMemoryM;

internal class LoadMemoryMViewModel : AnalysisViewModelBase<LoadMemoryMNode>
{	
	public const string UniqueId = "GPM.CustomAnalyses.Analyses.LoadMemoryM.LoadMemoryMViewModel";

	private readonly IIonDisplayInfoProvider _ionDisplayInfoProvider;
	private readonly IIonDataProvider _ionDataProvider;
	private readonly IRenderDataFactory _renderDataFactory;

	private IIonDisplayInfo? _ionDisplayInfo = null;

	public DataTable tableInfos { get; set; }
	public int ProgressBarValue { get; set; } = 0;
	public string NbAto { get; set; } = "0";
	public string NbElt { get; set; } = "10";
	public string NbClu { get; set; } = "0";

	public ICommand LoadCommand { get; }

	CAtom Atom = CustomAnalysesModule.Atom;

	Stopwatch ExecutionTime = new Stopwatch();

	private bool _displayUpdateOverlay;
	public bool DisplayUpdateOverlay
	{
		get => _displayUpdateOverlay;
		set => SetProperty(ref _displayUpdateOverlay, value);
	}

	public LoadMemoryMViewModel(
		IAnalysisViewModelBaseServices services,
		IIonDisplayInfoProvider ionDisplayInfoProvider,
		IRenderDataFactory renderDataFactory)
		: base(services)
	{
		_ionDisplayInfoProvider = ionDisplayInfoProvider;
		_renderDataFactory = renderDataFactory;

		LoadCommand = new DelegateCommand(LoadAtomMemory);
		CreateTableColumns();
	}

	public async void LoadAtomMemory()
	{
		Task<IIonData> IonDataTask = Node.GetIonData1();
		IIonData IonDataMemory = await IonDataTask;
		IIonDisplayInfo IonDisplayInfoMemory = Node.GetIonDisplayInfo();
		Console.WriteLine("Create Atom data memory ...");
		Atom.bInitMemory2(IonDataMemory, IonDisplayInfoMemory);

		DataRow newRow = tableInfos.NewRow();
		newRow[0] = Atom.iMemSize;
		newRow[1] = Atom.iNbElt;
		newRow[2] = Atom.iNbCluster;
		tableInfos.Rows.Add(newRow);

		Console.WriteLine("Atom memory size = {0}    NbElement = {1}    NbCluster = {2}\n", Atom.iMemSize, Atom.iNbElt, Atom.iNbCluster);

	}

	private void CreateTableColumns()
	{
		// Here we create a DataTable 
		tableInfos = new DataTable();

		tableInfos.Columns.Add("Nb Atoms", typeof(int));
		tableInfos.Columns.Add("Nb Elements", typeof(int));
		tableInfos.Columns.Add("Nb Cluster", typeof(int));
	}
}


