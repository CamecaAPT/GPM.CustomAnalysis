using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Windows.Media;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using CommunityToolkit.HighPerformance;

namespace GPM.CustomAnalyses.Analyses.SaxeyDiagram;

internal class GpmSaxeyDiagram : ICustomAnalysis<SaxeyDiagramOptions>
{
	private readonly IColorMapFactory _colorMapFactory;

	// Class definition
	private CSaxeyDiagram saxey;

	public GpmSaxeyDiagram(IColorMapFactory colorMapFactory)
	{
		_colorMapFactory = colorMapFactory;
	}

	/// <inheritdoc/>
	public void Run(IIonData ionData, SaxeyDiagramOptions options, IViewBuilder viewBuilder)
	{
		// Check for proper APT sections.  TODO - generate them if needed.
		var sectionInfo = ionData.Sections;

		if (!sectionInfo.ContainsKey(IonDataSectionName.Mass) || !sectionInfo.ContainsKey("Multiplicity") && !sectionInfo.ContainsKey("pulse"))
		{
			viewBuilder.AddText("Error", "Missing section(s) in the APT file.  \"Mass\" and \"Multiplicity\" are required.");
			return;
		}

		bool hasMultiplicity = sectionInfo.ContainsKey("Multiplicity");

		// Init Saxey diagram
		saxey = new CSaxeyDiagram();

		// Build Saxey diagram
		saxey.Build(options, ionData, hasMultiplicity);
		
		var map2D = new ReadOnlyMemory2D<float>(saxey.Map, options.EdgeSize, options.EdgeSize);
		var colorMap = _colorMapFactory.GetPresetColorMap(ColorMapPreset.Bright);
		colorMap.OutOfRangeBottom = Colors.White;

		var histogram2DContext = new Histogram2DContext(
			map2D,
			new Vector2(options.Resolution, options.Resolution),
			colorMap,
			new Vector2(options.XMin, options.YMin),
			options.PlotZeroAsWhite ? 0.0001f : null);
		viewBuilder.AddHistogram2D("Saxey Diagram", "M/n (da)", "M/n (da)", histogram2DContext);

		if (options.ExportToCsv)
		{
			var csvName = ionData.Filename + ".SP.csv";
			saxey.ExportToCsvTable(csvName, out string err);
		}
	}

	private class CSaxeyDiagram
	{
		private SaxeyDiagramOptions options;
		private int pixels;

		public float[] Map { get; private set; }

		public void Build(SaxeyDiagramOptions optionsIn, IIonData ionData, bool hasMultiplicity)
		{
			options = optionsIn;

			pixels = (options.EdgeSize + 2) * (options.EdgeSize + 2);
			Map = new float[pixels];

			if (hasMultiplicity)
			{
				BuildFromMultiplicitySection(ionData);
			}
			else
			{
				BuildFromPulseSection(ionData);
			}

			NormalizeMap();
		}

		private void BuildFromMultiplicitySection(IIonData ionData)
		{
			string[] sections = { IonDataSectionName.Mass, "Multiplicity" };
			var multiEventMasses = new List<float>();

			foreach (var chunk in ionData.CreateSectionDataEnumerable(sections))
			{
				var masses = chunk.ReadSectionData<float>(IonDataSectionName.Mass).Span;
				var multiplicities = chunk.ReadSectionData<int>("Multiplicity").Span;

				for (int i = 0; i < chunk.Length; ++i)
				{
					int multiplicity = multiplicities[i];

					if (multiplicity == 1 && multiEventMasses.Count == 0)
					{
						continue;
					}

					if (multiEventMasses.Count == 0 || multiplicity == 0)
					{
						float mass = masses[i];

						multiEventMasses.Add(mass);
						continue;
					}

					ProcessEvent(multiEventMasses);

					if (multiplicity != 1)
					{
						float mass = masses[i];

						multiEventMasses.Add(mass);
					}
				}
			}

			// Perhaps catch the very last multi event
			if (multiEventMasses.Count != 0)
			{
				ProcessEvent(multiEventMasses);
			}
		}

		private void BuildFromPulseSection(IIonData ionData)
		{
			string[] sections = { IonDataSectionName.Mass, "pulse" };
			var multiEventMasses = new List<float>();

			float prevMass = 0;
			float prevPulse = -1;

			foreach (var chunk in ionData.CreateSectionDataEnumerable(sections))
			{
				var masses = chunk.ReadSectionData<float>(IonDataSectionName.Mass).Span;
				var pulses = chunk.ReadSectionData<float>("pulse").Span;
				
				for (int i = 0; i < chunk.Length; ++i)
				{
					float mass = masses[i];
					float pulse = pulses[i];

					if (pulse == prevPulse)
					{
						multiEventMasses.Add(prevMass);
					}
					else if (multiEventMasses.Count != 0)
					{
						multiEventMasses.Add(prevMass);
						ProcessEvent(multiEventMasses);
					}

					prevMass = mass;
					prevPulse = pulse;
				}
			}

			// Perhaps catch the very last multi event
			if (multiEventMasses.Count != 0)
			{
				multiEventMasses.Add(prevMass);
				ProcessEvent(multiEventMasses);
			}
		}

		private void NormalizeMap()
		{
			// Adjust amplitude of Map memory
			float fMaxValue = 0;
			for (int i = 0; i < pixels; i++)
			{
				if (Map[i] > 0)
					Map[i] = (float)Math.Log10(1 + Map[i]);
				fMaxValue = Math.Max(Map[i], fMaxValue);
			}

			for (int i = 0; i < pixels; i++)
			{
				Map[i] *= 100 / fMaxValue;
			}
		}

		private void ProcessEvent(List<float> multiEventMasses)
		{
			// We have the whole event, sort and plot.
			multiEventMasses.Sort();
			int events = multiEventMasses.Count;

			// Treat selected type of events
			if (options.EventSelections.Plot(events))
			{
				for (int j = 0; j < events - 1; j++)
				for (int k = j + 1; k < events; k++)
				{
					if (multiEventMasses[j] >= options.XMin && multiEventMasses[j] < options.XMin + options.MassExtent)
						if (multiEventMasses[k] >= options.YMin && multiEventMasses[k] < options.YMin + options.MassExtent)
						{
							int b = (int) ((multiEventMasses[j] - options.XMin) / options.Resolution);
							int a = (int) ((multiEventMasses[k] - options.YMin) / options.Resolution);

							int index = a * options.EdgeSize + b;
							if (index < pixels)
								Map[index]++;
						}

					if (multiEventMasses[k] >= options.XMin && multiEventMasses[k] < options.XMin + options.MassExtent)
						if (multiEventMasses[j] >= options.YMin && multiEventMasses[j] < options.YMin + options.MassExtent)
						{
							int b = (int) ((multiEventMasses[j] - options.YMin) / options.Resolution);
							int a = (int) ((multiEventMasses[k] - options.XMin) / options.Resolution);

							int index = b * options.EdgeSize + a;
							if (index < pixels)
								Map[index]++;
						}
				}

			}

			multiEventMasses.Clear();
		}
		
		/// <summary>
		/// Export histogram data to a CSV file as a table
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="error">Error text if failure (CSV open, for instance)</param>
		internal bool ExportToCsvTable(string filename, out string error)
		{

			try
			{
				float fResolution = options.MassExtent / (float)options.EdgeSize;
				using (var writer = new StreamWriter(filename))
				{

					Console.WriteLine("a");
					for (int j = 0; j < options.EdgeSize; j++)
					{
						if (j == 0)
						{
							writer.Write(@"y [Da]\x [Da]");
							//continue;
						}

						writer.Write(',');
						double y = options.YMin + j * fResolution;
						writer.Write(y);
					}

					writer.Write(writer.NewLine);

					for (int i = 0; i < options.EdgeSize; i++)
					{
						double x = options.XMin + i * fResolution;
						writer.Write(x);
						for (int j = 0; j < options.EdgeSize; j++)
						{

							writer.Write(',');
							writer.Write(Map[j * options.EdgeSize + i]);

						}

						writer.Write(writer.NewLine);
					}
				}
			}
			catch (Exception e)
			{
				error = e.Message;
				return false;
			}

			error = null;
			return true;
		}
	}
}
