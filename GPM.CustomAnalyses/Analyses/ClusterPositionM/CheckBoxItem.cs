using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace GPM.CustomAnalyses.Analyses.ClusterPositionM;

internal class CheckBoxItem : BindableBase
{
	public int Id { get; set; }
	public string Caption { get; set; }
	public Brush BackColor{ get; }
	public Color Color{ get; }
	public string Type { get; }
	public int Family { get; set; }

	private bool _isSelected;
	public bool IsSelected
	{
		get => _isSelected;
		set => SetProperty(ref _isSelected, value);
	}

	public CheckBoxItem(int id , string caption , Color color , bool isSelected = false, string type = null, int family = 0)
	{
		this.Id = id;
		this.Caption = caption;
		this.IsSelected = isSelected;
		this.BackColor = new SolidColorBrush(color);
		this.Color = color;
		this.Type = type;
		this.Family = family;
	}


}
