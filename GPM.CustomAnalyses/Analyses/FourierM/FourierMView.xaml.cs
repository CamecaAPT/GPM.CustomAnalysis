using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Windows.Controls;
using Cameca.CustomAnalysis.Interface;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Data;

namespace GPM.CustomAnalyses.Analyses.FourierM;
/// <summary>
/// Interaction logic for FourierMView.xaml
/// </summary>
public partial class FourierMView : UserControl
{
	CAtom Atom = CustomAnalysesModule.Atom;
	public FourierMView()
	{
		InitializeComponent();
	}

	private void combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{

	}

	private void CreateEltList()
	{

	}

	public void UpdateProgressBar(float value)
	{
		progressBar.Value = value;
		Console.WriteLine(progressBar.Value);
	}
}
