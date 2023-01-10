using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Windows.Controls;
using Cameca.CustomAnalysis.Interface;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Data;

namespace GPM.CustomAnalyses.Analyses.DataFilteringM;
/// <summary>
/// Interaction logic for DataFilteringMView.xaml
/// </summary>
public partial class DataFilteringMView : UserControl
{
	CAtom Atom = CustomAnalysesModule.Atom;
	public DataFilteringMView()
	{
		InitializeComponent();
	}

	private void combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{

	}

	private void CreateEltList()
	{

	}

}
