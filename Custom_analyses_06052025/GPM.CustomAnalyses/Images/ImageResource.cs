using System;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GPM.CustomAnalyses.Images;

internal static class ImageResource
{
	private const string PieChartIconPath = $"Images/PieChartIcon.png";

	public static ImageSource PieChartIcon { get; } = CreateFromResourcePath(PieChartIconPath);

	/// <summary>
	/// A small helper to create an <see cref="ImageSource"/> from an assembly resource
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private static ImageSource CreateFromResourcePath(string path)
	{
		var assembly = Assembly.GetCallingAssembly();
		var uri = new Uri($"pack://application:,,,/{assembly.GetName().Name};component/{path}");
		var image = new BitmapImage(uri);
		image.Freeze();
		return image;
	}
}
