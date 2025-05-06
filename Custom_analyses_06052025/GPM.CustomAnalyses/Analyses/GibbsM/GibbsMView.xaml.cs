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

namespace GPM.CustomAnalyses.Analyses.GibbsM;
/// <summary>
/// Interaction logic for GibbsMView.xaml
/// </summary>
public partial class GibbsMView : UserControl
{
	CAtom Atom = CustomAnalysesModule.Atom;
	public GibbsMView()
	{
		InitializeComponent();
		ProfilType.Items.Add("Nb Atoms");
		ProfilType.Items.Add("Percent");
		ProfilType.SelectedIndex = 1;
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
