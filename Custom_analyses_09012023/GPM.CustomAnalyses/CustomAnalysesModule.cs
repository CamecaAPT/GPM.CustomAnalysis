using System;
using System.IO;
using System.Text;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Cameca.CustomAnalysis.Utilities.Legacy;
using Microsoft.Win32.SafeHandles;
using Prism.Ioc;
using Prism.Modularity;
using GPM.CustomAnalyses.Analyses.ClusteringM;
using GPM.CustomAnalyses.Analyses.LoadMemoryM;
using GPM.CustomAnalyses.Analyses.ClusterPositionM;
using GPM.CustomAnalyses.Analyses.ClusterInformationM;

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

		// Custom analysis extensions will typically need to register:
		// * Node implementation (object)
		// * Display information (INodeDisplayInfo) -- Recommend not using Node instance as display info needs to be instantiated to display menu, etc, often before we want to create full node instance. Try to keep to just interface.
		// * Menu factory (IAnalysisMenuFactory) -- Resolved each time menu is opened so that available menu items/actions can be contextually aware of what is being clicked. Try to keep light and separate from other classes.
		// * 1+ View models (object)
		// In OnInitialized method, resolve the IExtensionRegistry instance and RegisterAnalysisView to link views with corresponding view models, and define the display location of the view (top or bottom panel of IVAS)

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

	}

	public void OnInitialized(IContainerProvider containerProvider)
	{
		var extensionRegistry = containerProvider.Resolve<IExtensionRegistry>();
		extensionRegistry.RegisterAnalysisView<ClusterPositionMView, ClusterPositionMViewModel>(AnalysisViewLocation.Top);
		extensionRegistry.RegisterAnalysisView<ClusteringMView, ClusteringMViewModel>(AnalysisViewLocation.Top);
		extensionRegistry.RegisterAnalysisView<ClusterInformationMView, ClusterInformationMViewModel>(AnalysisViewLocation.Top);
		extensionRegistry.RegisterAnalysisView<LoadMemoryMView, LoadMemoryMViewModel>(AnalysisViewLocation.Top);

		// Examples
		// NOTE: AnalysisViewLocation.Bottom will display analysis view in the bottom tab. This can be especially useful if the
		// analysis will add data to the main 3D chart and it is undesirable to cover the main chart by default when opening the view.
		//extensionRegistry.RegisterAnalysisView<ClusterPositionMPreviewView, ClusterPositionMPreviewViewModel>();  // Only used for preview, so view location (Default) will never need to be considered
		// Preview view must be registered separately, and only respects a single registration per node.
		// No default preview is provided: if not configured, not previews will be available for the custom analysis node
		// Previews can often reuse th main view model and view, possibly with conditional logic depending on the ViewModelPreviewLoaded event,
		// but in this case we want to demonstrate the full possibility to define an entirely different preview. In this case we only show one  part of the full default view with a custom layout.
		//extensionRegistry.RegisterPreview(ClusterPositionMNode.UniqueId, ClusterPositionMPreviewViewModel.UniqueId);
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
