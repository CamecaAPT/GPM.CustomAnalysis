using System;
using System.Windows.Media;
using Cameca.CustomAnalysis.Interface;

namespace GPM.CustomAnalyses;

internal class RandomColorIonDisplayInfo : IIonDisplayInfo
{
	private static readonly Lazy<RandomColorIonDisplayInfo> Lazy = new(() => new RandomColorIonDisplayInfo());
	public static RandomColorIonDisplayInfo Instance => Lazy.Value;
	private RandomColorIonDisplayInfo() { }

	public Color GetColor(IonFormula formula)
	{
		// Seed random color by hash code to maintain same random color for any given IonFormula
		var r = new Random(formula.GetHashCode());
		return Color.FromRgb((byte)r.Next(256), (byte)r.Next(256), (byte)r.Next(256));
	}
}
