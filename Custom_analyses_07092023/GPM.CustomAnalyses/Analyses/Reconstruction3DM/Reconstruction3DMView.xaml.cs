using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Windows.Controls;
using Cameca.CustomAnalysis.Interface;
using LiveCharts;
using LiveCharts.Wpf;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Data;

namespace GPM.CustomAnalyses.Analyses.Reconstruction3DM;
/// <summary>
/// Interaction logic for Reconstruction3DMView.xaml
/// </summary>
public partial class Reconstruction3DMView : UserControl
{
	CAtom Atom = CustomAnalysesModule.Atom;
	public Reconstruction3DMView()
	{
		InitializeComponent();
		ComboboxRec.Items.Add("Cone angle");
		ComboboxRec.Items.Add("eBeta");
		ComboboxRec.Items.Add("SEM Image");
	}

	public Func<ChartPoint, string> PointLabel { get; set; }

	private void Chart_OnDataClick(object sender, ChartPoint chartpoint)
	{
		var chart = (LiveCharts.Wpf.PieChart)chartpoint.ChartView;

		//clear selected slice.
		foreach (PieSeries series in chart.Series)
			series.PushOut = 0;

		var selectedSeries = (PieSeries)chartpoint.SeriesView;
		selectedSeries.PushOut = 8;
	}



	private void combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{

	}

	private void CreateEltList()
	{

	}

}
