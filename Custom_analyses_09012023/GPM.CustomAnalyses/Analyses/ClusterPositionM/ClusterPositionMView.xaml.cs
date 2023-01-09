using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Windows.Controls;
using Cameca.CustomAnalysis.Interface;
using LiveCharts;
using LiveCharts.Wpf;

namespace GPM.CustomAnalyses.Analyses.ClusterPositionM;
/// <summary>
/// Interaction logic for ClusterPositionMView.xaml
/// </summary>
public partial class ClusterPositionMView : UserControl
{

	public ClusterPositionMView()
	{

		InitializeComponent();
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

	private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
	{
	}

	private void combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{

	}

	private void CreateEltList()
	{

	}
}
