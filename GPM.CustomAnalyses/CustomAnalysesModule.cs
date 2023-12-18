using System;
using System.IO;
using System.Text;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using GPM.CustomAnalyses.Analyses.ClusterPositionM;
using GPM.CustomAnalyses.Analyses.ClusterInformationM;
//using GPM.CustomAnalyses.Examples.ExampleDataFilter;
using Microsoft.Win32.SafeHandles;
using Prism.Ioc;
using Prism.Modularity;
using GPM.CustomAnalyses.Analyses.ClusteringM;
using GPM.CustomAnalyses.Analyses.LoadMemoryM;
using GPM.CustomAnalyses.Analyses.FourierM;
using GPM.CustomAnalyses.Analyses.DataFilteringM;

namespace GPM.CustomAnalyses;

public class CustomAnalysesModule : IModule
{
	public static CAtom Atom = new CAtom();

	public void RegisterTypes(IContainerRegistry containerRegistry)
	{
		ConsoleHelper.AllocConsole();
		vPrepareConsole();

		// Register services required to use base classes from Utilities library
		containerRegistry.AddCustomAnalysisUtilities(options =>
		{
			options.UseBaseClasses = true;
			options.UseStandardBaseClasses = true;
			options.UseLegacy = true;
		});

		// Load Memory Menu
		containerRegistry.Register<object, LoadMemoryMNode>(LoadMemoryMNode.UniqueId);
		containerRegistry.RegisterInstance(LoadMemoryMNode.DisplayInfo, LoadMemoryMNode.UniqueId);
		containerRegistry.Register<IAnalysisMenuFactory, LoadMemoryMMenuFactory>(nameof(LoadMemoryMMenuFactory));
		containerRegistry.Register<object, LoadMemoryMViewModel>(LoadMemoryMViewModel.UniqueId);

		// Cluster Position Menu
		containerRegistry.Register<object, ClusterPositionMNode>(ClusterPositionMNode.UniqueId);
		containerRegistry.RegisterInstance(ClusterPositionMNode.DisplayInfo, ClusterPositionMNode.UniqueId);
		containerRegistry.Register<IAnalysisMenuFactory, ClusterPositionMMenuFactory>(nameof(ClusterPositionMMenuFactory));
		containerRegistry.Register<object, ClusterPositionMViewModel>(ClusterPositionMViewModel.UniqueId);

		// Clustering Menu
		containerRegistry.Register<object, ClusteringMNode>(ClusteringMNode.UniqueId);
		containerRegistry.RegisterInstance(ClusteringMNode.DisplayInfo, ClusteringMNode.UniqueId);
		containerRegistry.Register<IAnalysisMenuFactory, ClusteringMMenuFactory>(nameof(ClusteringMMenuFactory));
		containerRegistry.Register<object, ClusteringMViewModel>(ClusteringMViewModel.UniqueId);

		// Clustering Information Menu
		containerRegistry.Register<object, ClusterInformationMNode>(ClusterInformationMNode.UniqueId);
		containerRegistry.RegisterInstance(ClusterInformationMNode.DisplayInfo, ClusterInformationMNode.UniqueId);
		containerRegistry.Register<IAnalysisMenuFactory, ClusterInformationMMenuFactory>(nameof(ClusterInformationMMenuFactory));
		containerRegistry.Register<object, ClusterInformationMViewModel>(ClusterInformationMViewModel.UniqueId);

		// Fourier Transform Menu
		containerRegistry.Register<object, FourierMNode>(FourierMNode.UniqueId);
		containerRegistry.RegisterInstance(FourierMNode.DisplayInfo, FourierMNode.UniqueId);
		containerRegistry.Register<IAnalysisMenuFactory, FourierMMenuFactory>(nameof(FourierMMenuFactory));
		containerRegistry.Register<object, FourierMViewModel>(FourierMViewModel.UniqueId);

		// Reconstruction 3d Menu
		containerRegistry.Register<object, DataFilteringMNode>(DataFilteringMNode.UniqueId);
		containerRegistry.RegisterInstance(DataFilteringMNode.DisplayInfo, DataFilteringMNode.UniqueId);
		containerRegistry.Register<IAnalysisMenuFactory, DataFilteringMMenuFactory>(nameof(DataFilteringMMenuFactory));
		containerRegistry.Register<object, DataFilteringMViewModel>(DataFilteringMViewModel.UniqueId);
	}

	public void OnInitialized(IContainerProvider containerProvider)
	{
		var extensionRegistry = containerProvider.Resolve<IExtensionRegistry>();
		extensionRegistry.RegisterAnalysisView<ClusterPositionMView, ClusterPositionMViewModel>(AnalysisViewLocation.Top);
		extensionRegistry.RegisterAnalysisView<ClusteringMView, ClusteringMViewModel>(AnalysisViewLocation.Top);
		extensionRegistry.RegisterAnalysisView<ClusterInformationMView, ClusterInformationMViewModel>(AnalysisViewLocation.Top);
		extensionRegistry.RegisterAnalysisView<LoadMemoryMView, LoadMemoryMViewModel>(AnalysisViewLocation.Top);
		extensionRegistry.RegisterAnalysisView<FourierMView, FourierMViewModel>(AnalysisViewLocation.Top);
		extensionRegistry.RegisterAnalysisView<DataFilteringMView, DataFilteringMViewModel>(AnalysisViewLocation.Top);
	}

	// Console properties
	private static void vPrepareConsole()
	{
		IntPtr stdHandle = ConsoleHelper.GetStdHandle(ConsoleHelper.STD_OUTPUT_HANDLE);
		SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
		FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
		Encoding encoding = System.Text.Encoding.GetEncoding(ConsoleHelper.MY_CODE_PAGE);
		StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
		standardOutput.AutoFlush = true;
		Console.SetOut(standardOutput);
	}
}
