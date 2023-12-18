using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;
using Prism.Mvvm;

namespace GPM.CustomAnalyses.Analyses.SaxeyDiagram
{
#pragma warning disable 1591
	[Serializable]
	public class EventSelections : BindableBase
	{
		private bool doubles = true;
		private bool triples = true;
		private bool quads = true;
		private bool fivePlus = true;
		private bool all;
		
		/// <inheritdoc />
		public EventSelections()
		{
			UpdateAll();
		}

		private void UpdateAll()
		{
			all = doubles && triples && quads && fivePlus;
			RaisePropertyChanged(nameof(All));
		}

		public bool Doubles
		{
			get => doubles;
			set => SetProperty(ref doubles, value, UpdateAll);
		}

		public bool Triples
		{
			get => triples;
			set => SetProperty(ref triples, value, UpdateAll);
		}

		public bool Quads
		{
			get => quads;
			set => SetProperty(ref quads, value, UpdateAll);
		}

		[Display(Name = ">= 5")]
		public bool FivePlus
		{
			get => fivePlus;
			set => SetProperty(ref fivePlus, value, UpdateAll);
		}

		[XmlIgnore]
		public bool All
		{
			get => all;
			set => SetProperty(ref all, value, () =>
			{
				doubles = true;
				triples = value;
				quads = value;
				fivePlus = value;
				RaisePropertyChanged(nameof(Doubles));
				RaisePropertyChanged(nameof(Triples));
				RaisePropertyChanged(nameof(Quads));
				RaisePropertyChanged(nameof(FivePlus));
			});
		}

		public bool Plot(int eventCount)
		{
			if (all) return true;
			if (eventCount == 2) return doubles;
			if (eventCount == 3) return triples;
			if (eventCount == 4) return quads;
			if (eventCount >= 5) return fivePlus;
			throw new ArgumentException($"Invalid value {eventCount}", nameof(eventCount));
		}
	}

	[Serializable]
	public class SaxeyDiagramOptions : BindableBase
	{
		private float massExtent = 50;
		private int edgeSize = 1000;
		private float xMin = 0;
		private float yMin = 0;
		private bool plotZeroAsWhite = true;
		private bool exportToCsv = false;
		private float resolution;

		/// <inheritdoc />
		public SaxeyDiagramOptions()
		{
			UpdateResolution();
		}

		private void UpdateResolution() => Resolution = MassExtent / EdgeSize;

		[Display(Name = "Mass Extent (Da)", Description = "Extent of plot, max mass is min + extent")]
		public float MassExtent
		{
			get => massExtent;
			set => SetProperty(ref massExtent, value, UpdateResolution);
		}

		[Display(Name = "Edge Size", Description = "Number of pixels in width and height")]
		public int EdgeSize
		{
			get => edgeSize;
			set => SetProperty(ref edgeSize, value, UpdateResolution);
		}

		[Display(Name = "X Minimum (Da)", Description = "Minimum mass on X axis")]
		public float XMin
		{
			get => xMin;
			set => SetProperty(ref xMin, value);
		}

		[Display(Name = "Y Minimum (Da)", Description = "Minimum mass on Y axis")]
		public float YMin
		{
			get => yMin;
			set => SetProperty(ref yMin, value);
		}

		[XmlIgnore]
		[Display(Name = "Resolution (Da)", Description = "Resolution of diagram")]
		public float Resolution
		{
			get => resolution;
			private set => SetProperty(ref resolution, value);
		}

		[Display(Name = "Events to show", Description = "Multiplicities of events to include in the diagram.")]
		public EventSelections EventSelections { get; set; } = new EventSelections();

		[Display(Name = "Plot zero as white")]
		public bool PlotZeroAsWhite
		{
			get => plotZeroAsWhite;
			set => SetProperty(ref plotZeroAsWhite, value);
		}

		[Display(Name = "Export CSV", Description = "Creates a CSV file of plot data.")]
		public bool ExportToCsv
		{
			get => exportToCsv;
			set => SetProperty(ref exportToCsv, value);
		}
	}
#pragma warning restore 1591
}
