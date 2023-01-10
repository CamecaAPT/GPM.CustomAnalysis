using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Windows.Controls;
using Cameca.CustomAnalysis.Interface;

namespace GPM.CustomAnalyses.Analyses.ClusterPositionM;
/// <summary>
/// Interaction logic for ClusterPositionMView.xaml
/// </summary>
public partial class ClusterPositionMView : UserControl
{

	public ClusterPositionMView()
	{
		InitializeComponent();
		ComboBoxKMeans.Items.Add("Manual");
		ComboBoxKMeans.Items.Add("Auto");
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
