using Prism.Mvvm;
using System;
using System.Windows.Media;

namespace GPM.CustomAnalyses.Analyses.ClusterInformationM;

internal class CheckBoxItem : BindableBase
{
	public string Caption { get; }
	public Brush BackColor{ get; }
	public Color Color{ get; }
	public string Type { get; }
	
	private bool _isSelected;
	public bool IsSelected
	{
		get => _isSelected;
		set => SetProperty(ref _isSelected, value);
	}

	public CheckBoxItem(string caption, Color color, bool isSelected = false, string type = null)
	{
		this.Caption = caption;
		this.IsSelected = isSelected;
		this.BackColor = new SolidColorBrush(color);
		this.Color = color;
		this.Type = type;
	}
}
