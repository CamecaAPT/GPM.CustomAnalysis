using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Windows.Controls;
using Cameca.CustomAnalysis.Interface;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Data;

namespace GPM.CustomAnalyses.Analyses.ClusteringM;
/// <summary>
/// Interaction logic for ClusteringMView.xaml
/// </summary>
public partial class ClusteringMView : UserControl
{
	CAtom Atom = CustomAnalysesModule.Atom;
	public ClusteringMView()
	{
		InitializeComponent();
		ComboBoxFiltering.Items.Add("Composition Grid");
		ComboBoxFiltering.Items.Add("Local Composition");
		ComboBoxClustering.Items.Add("Atomic Distance");
	}

    private void combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{

	}

	private void CreateEltList()
	{

	}

}
