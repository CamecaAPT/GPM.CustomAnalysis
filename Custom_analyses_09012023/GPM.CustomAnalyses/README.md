# Custom Analysis
#### 2022-06-06

## Introduction
With an initial release in version 6.2.3, AP Suite is adding increased support for developing extensions that can enhance the default functionality of the application. This includes an interface in APS for installing, upgrading, or uninstalling packaged extensions, public libraries that define shared interfaces or common code for developing extensions, and a locations to hook into the default application logic to add additional functionality.

The implementation of this extension infrastructure replaces previous recommendations for custom analysis development. Nearly all functionality that was previously supported by custom analyses in previous versions of AP Suite is still supported in some form, and the flexibility in adding new tools to IVAS has been greatly increased.

The following document offers a high-level overview the design decisions in how extensions are implemented, and instructions and recommended patterns for implementing new custom analyses.

## Extensions
Extensions are packaged assemblies that are loaded into AP Suite at startup. Each extension defines one or more module that appears in the existing AP Suite Module Catalog.

Extensions can be distributed as [NuGet](https://docs.microsoft.com/en-us/nuget/what-is-nuget) packages, compressing and archiving all contents into a single file. Packages can be publicly or privately distributed by setting up a NuGet feed, or can be loading into AP Suite locally after configuring an appropriate discovery path.

AP Suite defines a configurable location for extension installation (Configuration->Options->Extensions->Extensions Directory). By default, this location is "%LOCALAPPDATA%\CAMECA\AP Suite\Extensions", allowing users to manage extensions with user-level permissions. When extensions are installed, the contained assemblies and static content is extracted and copied to a unique folder at this location. Upon startup, AP Suite will seach this directory for any available extensions and load them into the Module Catalog (Configuration->Module Catalog). If using the default module catalog settings, all modules will be loaded when the application starts. If desires, a custom module catalog can be used to only enable some modules at startup.

If developing extensions for internal use with no intention of distributing publicly, the extra overhead of packaging the extension into a NuGet file and managing versions through the application interface may be undesirable. This package process can be bypassed by dropping all necessary assemblies into a folder at the configured extension installation path. After startup, AP Suite will find and load the extension. This can also be convenient for extension development, as the build process can be configured to directly output the compiled assemblies to this location, which then only will require a restart of AP Suite to find changes.

## Default Extensions
In partnership with GPM, CAMECA has developed a number of extensions that server as niche use cases or useful examples. Currently there are four extensions implementing custom analyses that are bundled with the AP Suite installer. Additionally, the same extension are published as NuGet packages to a [public NuGet feed hosted on MyGet.org](https://www.myget.org/feed/Packages/cameca-apsuite-extensions). If users have internet access on their workstation running AP Suite, extensions may be installed from this source. By installing from the public feed, future updates to the extensions can be easily identified and installed when available from inside AP Suite without required distribute of a new installer.

## Public Extension Development Interfaces/Libraries
A number of public projects have been created to serve as common interfaces for extensions to interface with AP Suite or to facilitate development. The source code of these projects (along with the default example extensions) can be found on the [CAMECA Instruments, Inc. GitHub page](https://github.com/CamecaAPT). Further details regarding important projects follow.

### Cameca.CustomAnalysis.Interface
This project serves as the main common library used to interface with AP Suite. It mainly defines numerous C# interfaces and events that AP Suite will implement. By referencing this project as a dependency for a custom extension, many features of IVAS can be accessed, allowing for a substantial amount of flexibility when developing a custom analysis. Any extension that defines a custom analysis will need to depend on this project.
### References
* Source Code: https://github.com/CamecaAPT/cameca-customanalysis-interface
* NuGet Package: https://www.myget.org/feed/cameca-extensions-dev/package/nuget/Cameca.CustomAnalysis.Interface

### Cameca.CustomAnalysis.Utilities
The previously described Cameca.CustomAnalysis.Interface project defines the bare minimum common code required to interace with AP Suite. While it is certainly possible to create a custom analysis while only referencing the interface project, it would require a substantial amount of additional code to create a useful custom analysis.

To streamline the development of custom analyses, we provide this utilities project. It defines a number of useful tools that can be useful when developing a custom analysis, especially in the most common case where the goal is to create a custom analysis that closesly matches the functionality of native AP Suite analyses.

Perhaps most notable are the provided base classes. The AnalysisNodeBase and AnalysisViewModelBase classes implement a substantial amount of default configuration. Using these as base classes for the creation of new custom analyses will dramatically simplify development.

The utilities project also exposes a Legacy namespace that implements code that roughly matches the now removed custom analysis interfaces from previous versions of AP Suite. Use of these classes is not recommended for new development, rather they attempt to make translating old-style custom analysis to the new toolset with as little work as possible.

### Legacy ViewBuilder
The new custom analysis toolkit no longer includes the view builder of previous versions. To simplify conversion of any existing custom analyses made for previous versions, the utitlies project containersa dedicated ViewBuilder that mimics the interface exposed in the old ICustomAnalysis interface.
Use of this object and the view builder pattern is not required, or even recomended, but it allowes easier conversion of old analyses while minimizeing changes to the existing code.

If the utilities package is being uses, ensure that it is registered with the service container in the IModule.RegisterTypes method. This can be configured to register different services depending on how this package is being used.
```csharp
class CustomModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Register services required to use base classes from Utilities library
		containerRegistry.AddCustomAnalysisUtilities(options =>
		{
            // Additional options can be configured in the configuration delegate parameter if using specific features.
            // For example, to use the legacy base classes, ensure that UseLegacy is enabled.
			options.UseLegacy = true;
		});
    }
    // ...
}
```
### References
* Source Code: https://github.com/CamecaAPT/cameca-customanalysis-utilities
* NuGet Package: https://www.myget.org/feed/cameca-extensions-dev/package/nuget/Cameca.CustomAnalysis.Utilities

### Cameca.Extension.Controls
Developers of custom analyses may wish to take advantage of existing AP Suite controls. This allows for quickly displaying data in common plots, and makes the custom analysis interface appear more tightly integrated with the rest of AP Suite. This project defined custom controls that can be used when defining the visual layout in an extension. These controls define regions and data binding information. When loaded by AP Suite, their display templates will be replaced by controls native to the main application. This allows for defining any arbitrary layout while still being able to take advantage of AP Suite charts and controls.

Currently supported controls as of the date of writing this document include:
* Table
* PropertyGrid
* Chart2D
* Chart3D
* Histogram2D (a special instance of Chart2D required to render Histogram2D data)

### References
* Source Code: https://github.com/CamecaAPT/cameca-extensions-controls
* NuGet Package: https://www.myget.org/feed/cameca-extensions-dev/package/nuget/Cameca.Extensions.Controls


# Custom Analysis Development
## Overview
AP Suite is written in C# using  [Windows Presentations Forms](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/overview/?view=netdesktop-6.0) (WPF). The current version of AP Suite targets [.NET 6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0). The application uses a [Model-View-ViewModel](https://docs.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern) (MVVM) design pattern supported by the [Prism Library](https://prismlibrary.com/docs/). An understanding of MVVM and [dependency injection](https://en.wikipedia.org/wiki/Dependency_injection) design patterns can help in understanding the rational behind design decisions of the extension structure. AP Suite currently uses [Unity Container](http://unitycontainer.org/articles/introduction.html) as an [Inversion of Control](https://en.wikipedia.org/wiki/Inversion_of_control) (IoC) container to inject dependencies.

## Requirements
* Visual Studio 2022+ (for .NET 6 support)
* .NET SDK
* .NET 6 Runtime
* Licensed installation of AP Suite (for running the extensions/debugging)

### Setup
Ensure that the following package sources are configured in Visual Studio:
* https://www.myget.org/F/cameca-extensions-dev/api/v3/index.json
* https://www.myget.org/F/cameca-extensions-dev-preview/api/v3/index.json

For most simply development, set the following properties in the project *.csproj file.
```xml
<PropertyGroup>
    <!-- Required for extensions: copies dependency assemblies to output directory. -->
    <EnableDynamicLoading>true</EnableDynamicLoading>
    <!-- Recommened for development: Set output path to the AP Suite confiigured extension installation directory -->
    <AssemblyName>ExtensionAssemblyName</AssemblyName>
    <OutputPath>$(LOCALAPPDATA)\CAMECA\AP Suite\Extensions\$(AssemblyName)</OutputPath>
    <!-- Suppresses output subdirectories so all files are directly placed in OutputPath root -->
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>
</PropertyGroup>
```
With this configuration, building the extension will place it directly in the AP Suite installed extensions folder. Restarting AP Suite will discover the compiled extension.

It is additionally useful to create a custom debug profile in Visual Studio 2022 that points directoy to the installed AP Suite executable. Extensions are loaded into the same process as AP Suite, so launching AP Suite will allow direct debugging of an extension in development.

## Structure
Custom analysis have a number of core components:
### Node
The node is the core component that controls a custom analysis. The analysis node is represented in IVAS by and entry in the Analysis Tree. When an analysis is created or loaded, the node instance is always active, and should control all core functionality of the custom analysis. It is recommended that all required analysis calculations be handled by the node object, as well as state management and data persistance (save/load logic).
### View
A view is the user interface of an active custom analysis. Views can be placed by default in the top or bottom panels on the right side of IVAS. An analysis can have zero or more views, with no strict requirement or limitation. A view is only visible and active up request, generally through interaction with the node in the analysis tree by menu or double-clicking. Because a view is not guarenteed to be present at all times for a given analysis node, it is recommended that no logic that is strictly necessary should be included in the view. The view should only contain code that is strictly required to render and interactic with analysis data. This follows the MVVM design pattern.
Views are defined in code as a WPF UserControl, generally using Extensible Application Markup Language (XAML). Controls from the provided Cameca.Extensions.Controls may be used, but are not required. In addition to standard WPF controls, custom controls could be created, or other libraries of controls could be added as dependencies and used. However, using 3rd-party controls may have their own licensing restrictions if distribution is desired, and the appearence of the controls may not match the AP Suite theme.
### View Model
The view model manages data for the view, and serves as a link between the analysis logic and the view that displays and interacts with the rendered data. Each view model should be registered to control a corresponding view. The deliniation regarding what code the view model should be aware of can be slightly subjective, but remember that the view model is only instantiated when a custom analysis requests that the corresponding view should be displayed. Therefore it is not recommended that any data or state that is required for the analysis to be entirely located in the view model object. Instead, the recommended pattern is for the node instance to contain all required data and state, and the view model should request a reference to the corresponding node. Then the view model can coordinate between the node and view by passing interaction information to the node, requesting updates from the node, and formatting and passing results back to the view for display.

A custom analysis therefore is required to have a single node, and optionally has one or more views. Each view will have a corresponding view model instance to coordinate data between the main node and the associated view.

## Custom Analysis Types
There are two main types of custom analyses that are supported.
### Analysis
The standard version is simply referred to as "Analysis" and is used to explore data in a given state. This can be either the entire data set, or if created under some Region of Interest (ROI) this analysis will apply to only that spatially filtered data.
### Data Filter
A custom data filter provides a method of filtering the ion data set (from current state, either top level or spatially filtered by an ROI) through code. Data filter nodes expect a data filter function that returns a collection of ion indices for the current IonData object. In most other ways this type of node functions as a standard ROI. If the parent data context changes, the filter will be invalidated and require a further update. Analyses can be created as children of this node an only the filtered data will be used for a child analysis.

Importantly, overriding ranges is not supported for custom data filter nodes. The data filter could be conditionally defined using ion type data as provided by the ion data object. If so, allowing overriding ranges would create a state of circular dependency when resolving an ion type for a given mass/charge ratio.

## Shared Interface
As described in the overview above, the Cameca.CustomAnalysis.Interface package is the required common shared interface between AP Suite and all extensions that implement a custom analysis.

Many implementation of the abstract interfaces in this package are dependant on the current state of AP Suite or the scope in which they are requested (e.g. which analysis tree the custom analysis is active in). To loosely couple the extension to AP Suite and handle  conditional resolution based on application state, reference to implementations in AP Suite are generally resolved using a global unique identifier. In C# we use the [System.Guid struct](https://docs.microsoft.com/en-us/dotnet/api/system.guid?view=net-6.0) for a globally unique identifier.

One of the most important resources in this package is the IIdProvider. An instance of IdProvider is managed by AP Suite and can be requested by an extension. It defines a single method `Guid Get(object item);`. When called and passed any object (node, view model, view, service, etc), it returns a unique identifier associated with that specific object *by reference*. In this way, by passing a Guid to many resource providers in the interface package, extensions can request resource for a certain object that is being managed by AP Suite that the extension may not directly have a reference to. This pattern will quickly become apparent if looking at example code, as it is widely used to inform AP Suite of the context under which an extension requests data.

Nearly all of this interface package is exposed by AP Suite to extensions through the dependency injection pattern. AP Suite registers implementations of abstract interfaces with the IoC container. Extensions can set these interfaces as constructor parameters. When the extension instances are created by AP Suite, the assocated implementation will be provided as the arguments fulfilling the corresponding interfaces through the constructor injection pattern. By using this method instead of defining static interface, the interface package can be made more flexible to support backwards compatibility while adding new functionality. Extensions additionally only have to request what is ncessary for that specific implementation.

The Interface Package is generally organized around two main concepts: events and resource.
### Events
Events are global weak events that allow communcation between AP Suite and extensions. Technically, they use the [Prism Library Event Aggregator](https://prismlibrary.com/docs/event-aggregator.html). Events are published to the event aggregator, and subscriptions to events can be registered with the event aggregator. Nearly all events contain an Guid that identifies the object upon with the relevent action that triggered the event occured. Using this ID, a specific custom analysis can filter events to only ID values that match its own so it can handle only events relevant to the analysis in question.

To simplify use, each event has corresponding extension methods targeting the shared IEventAggregator coordinating event that are used to publish and subscribe to event instances.

As of the writing of this document (2022-06-06), there are 15 events:
| Event                       | Description |
|-----------------------------|-------------|
| ActivateMainView            | Request that the Analysis Set (*not a specific analysis*) activate the main view (generally the main 3D chart) |
| CreateNode                  | Requests creation of a new analysis node of a specified type |
| DisplayView                 | Request that a specified view is displayed. Can be configured to match existing views to or always create new instances |
| NodeAdded                   | Fired when a node is added to the analysis tree. Node is not yet necessarily fully created |
| NodeAfterCreated            | Fired after a node is newly created (not loaded from saved). Useful to display a default view after initial creation. |
| NodeCreated                 | Fired when a node is newly created (not loaded from saved) |
| NodeInteraction             | Fired when suppored actions are performed upon a node. Currenlty only double-click is supported |
| NodeLoaded                  | Fired when a node is loaded from saved state upon opening a saved analysis tree. |
| NodeRemoved                 | Fired when a node is removed from the analysis tree. Can be used from any required resource cleanup. |
| NodeRename                  | Requests that a node is to be renamed to new name as provided in the event arguments. |
| NodeRequestDataUpdate       | Requests that the main data object (IonData) associated with the node be updated if necessary. |
| NodeSaved                   | Fired when a node has been saved. |
| ViewModelActivated          | Fired when a view is activated. Typically this means the user focused the view associated with this view model. |
| ViewModelClosed             | Fired when a view is closed. |
| ViewModelCreated            | Fired when a view is created. Views are never loaded by default upon loading an analysis tree, so note the lack of a corresponding ViewModelLoaded event |
| ViewModelPreviewLoaded      | Fired when a preview view model is created. Provided with serialized preview data from an associated node preview save delegate |

### Resources
Resources are persistant objects that are implemented by AP Suite. Generally they provide access to methods in the native application or expose persistent state objects that would not be well represented by one-time events.

Nearly all resources are resolved by using dependency injection to resolve a *Provider* implementation. The provider will expose a generic `IResource Resolve(Guid id);` method or similar. Calling the resolve method on the provider with the instance ID (Guid) of the current object (as provided by the IIdProvided) will provide the appropriate resource for the current application state. The following table lists resources, but be aware that most resources may not be directly injected. Most will need to be resolved by using an associated provider.

>**IMPORTANT:** Due to limitations in how custom analyses are hosted, resource providers do not link resources until a custom analysis node instance is fully registered with AP Suite. This does not occur until an instance is fully constructed. For this reason, resource providers are not yet aware of a node *while still in the constructor*. Therefore, attempting to call the Resolve method of a resource provider in the constructor will always return `null`. A resource provider will not return the resource until either the NodeCreated or NodeLoaded events have been fired. The recommended pattern is therefore to save a reference to the resource provider in the constructor and handle the created and/or loaded events to resolve a concrete resource instance. This unfortunately creates more complex code requiring additional boilerplate, but using the Utilities project base classes can largely mitigate this extra work. Simply remember to override the base `OnInstantiated` method and resolve all instances at that point using the `InstanceId` property.
```csharp
internal class MyCustomAnalysisNode : AnalysisNodeBase // From Cameca.CustomAnalysis.Utilities
{
    private readonly IResourceProvider _resourceProvider;
    private IResource? resource = null;  // Will not have a resource upon object creation

    public MyCustomAnalysisNode(IResourceProvider resourceProvider, ...) : base(...)
    {
        // BAD -- Do not resolve in constructor
        IResource resource = resourceProvider.Resolve(InstanceId);
        Debug.Assert(resource is null);  // !! Always true: resource is null when resolved in ctor !!

        // GOOD -- Just save reference for now
        _resourceProvider = resourceProvider;
    }

    // From Cameca.CustomAnalysis.Utilities.AnalysisNodeBase
	protected override void OnInstantiated(INodeInstantiatedEventArgs eventArgs)
	{
		// Base class calls this method on both NodeCreated and NodeLoaded events
		base.OnInstantiated(eventArgs);

        // GOOD -- Resolve resource here
        resource = resourceProvider.Resolve(InstanceId);
        Debug.Assert(resource is not null);  // !! Resource will not be null if resolved on Created/Loaded event !!
	}
}
```

| Resource | Organization Type | Description |
|-|-|-|
| Composition1D | Analysis | Provides access to the 1D composition analysis |
| Isosurface | Analysis | Provides access to the isosurface analysis returning a indexed triangle array mesh |
| Proxigram | Analysis | Provides access to the proxigram analysis |
| IonData | | The main ion data object for the analysis at the current level in the analysis tree. |
| CanSaveState | Persistence | Exposes a read/write flag that indicates if the current analysis should be marked as "dirty" and can be saved |
| NodePersistence | Persistence | Exposes delegate functions that control how data is saved (data specifically for previews can be saved seperately) |
| ExtensionDataStore | Persistence | Used by extensions to store key/value pair data independent of the current analysis tree. Can be stored at global or user scope. |
| NodeDataFilter | Filter | Exposes controls to filter ion data based on custom criteria. Required for implementing a custom data filter node. |
| AnalysisSetInfo | Display Info | High level display information and structure for the associated analysis set (analysis tree) |
| NodeInfo | Dispaly Info | High level display information for a given node as request by the node instance ID |
| NodeVisibilityControl | | Exposes control of the associated checkbox in the analysis tree for this node. Intended to control visiblity of node associated objects in the main 3D chart. |
| ProgressDialog | Async | Exposes a wrapper that can execute a provided C# async Task and displays a modal progress dialog |
| Properties | | Exposes a read/write reference to the property grid in the lower left corner of IVAS. Displayed when the associate node is selected, regardless of other view state. |
| ViewModelCaption | Display Info | Controls the icon and title of the tab header for view instances |

## IonData Access
The IonData object is the main source of analysis information. By default it provides mass, position, and ion type information for either the entire data set, or a filtered subset if retrieved from an ROI or data filter context.

In AP Suite, this is a heavily optimized object that interfaces with direct memory mapping to an APT file for performance considerations when handling large datasets. Due to limitations of C#, data must be iterated over in chunks (to be able to index into arrays), and it is desirable to minimize function calls when iterating over the resulting data. This causes difficulty in providing a simply API for accessing the IonData object in a performance considerate manor.

The Cameca.CustomAnalysis.Interface package attempts to handle this challenge by using [.NET System.Buffers](https://docs.microsoft.com/en-us/dotnet/api/system.buffers?view=net-6.0). By exposing ReadOnlyMemory objects, the direct memory mapped data can be pinned and passed up through managed C# code to be iterated over by a custom analysis directly. Familiarity with the concepts and restrictions of the System.Buffers namespace will make consuming ion data object easier.

Reading the IIonData object uses the following approach:
1. A chunk iterator is requested for specific ion data sections (e.g. Position, Mass, etc.). Recommeneded to use the stateic IonDataSectionName class for standard section name constants.
2. The ion data object returns an enumerator over chunks of data. The utilites package exposes a convenience enumerable extension method that wraps this enumerator so that each chunk can be accessed sequentially in a foreach loop.
3. Each chunk object contains some metadata (such as length), but importantly contains a `ReadOnlyMemory<T> ReadSectionData<T>(string sectionName)` method. This returns a pinned memory object that references the memory mapped data for the given section. Ensure that the generic type is accurate for the given section. For example, the Mass section should be called with a generic parameter of *float*.
4. Calling the .Span property on the returned ReadOnlyMemory struct will provide a Span struct that allows for indexing into the memory collection. Iterating over this with an indexed for loop provides an efficient method of directly accessing the section data memory.

Note that chunk boundaries are indeterminate. Data may be chunked even if small enough for a single array, and chunks may not always be the same size on repeated runs. If performing logic that requires knowledge of sequentially nearby ions, be sure to consider and handle the possible fragmentation of data at chunk boundaries. The only guarenteed is that a single ion will never be split. (e.g. position request will always break on a full position triplet).

Then general pattern commonly looks similar to the following:
```csharp
using System.Numerics;  // Vector3
using System.Buffers;  // ReadOnlyMemory
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;  // CreateSectionDataEnumerable extension method

IIonData ionData; // Assume a resolved instance of IIonData

string[] sections = new string[] { IonDataSectionName.Position, IonDataSectionName.Mass };
foreach (IChunkState chunk in ionData.CreateSectionDataEnumerable(sections))  // From utilities package
{
    // Get pinned memory referencing ion data section data for the current chunk
    // "Position" is 3 floats, matching the Vector3 struct
    ReadOnlyMemory<Vector3> positionsMemory = chunk.ReadSectionData<Vector3>(IonDataSectionName.Position);
    // "Mass" is a single float
    ReadOnlyMemory<float> massesMemory = chunk.ReadSectionData<float>(IonDataSectionName.Mass);

    // Get ReadOnlySpan<T> for indexing into the data
    ReadOnlySpan<Vector3> positions = positionsMemory.Span;
    ReadOnlySpan<float> masses = massesMemory.Span;

    // Iterate over data
    for (int i = 0; i < chunk.Length; i++)
    {
        // Data can be access by index
        Vector3 position = positions[i];
        float mass = masses[i];

        // Do work here ...
    }

    // Loop back and repeat for next chunk.
}
```
### Adding Virtual Section Data
The chunk interator additionally exposes a WriteSectionData method. When paired with `IIonData.AddVirtualSection`, additional data sections can be added to the IonData object at runtime. These are considered virtual because as noted below, they are not persisted to the underlying APT file and only exist for the same lifetime of the analysis set.

The general pattern to write a virtual section is as follows
```csharp
using System.Numerics;  // Vector3
using System.Buffers;  // ReadOnlyMemory
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;  // CreateSectionDataEnumerable extension method

IIonData ionData; // Assume a resolved instance of IIonData

// Add a new virtual section
string sectionName = "MyCustomSection";
Type type = typeof(float);  // For this example we will be writing 1 float per ion
string unit = "unitless";  // (Optional) Metadata describing describing the data to be written to the section
uint valuesPerRecord = 1;  // (Optional) Default = 1: number of instance of the given type that will be written per ion record
// Add the section metadata. Only section name and type are required
ionData.AddVirtualSection(sectionName, type, unit, valuesPerRecord);

// Not that unlike when reading data, the section that is being newly written is *NOT* included in the section list
// The section names defined when creating the iterator are sections to be read, and the virtual section does not yet exist.
// An exception will be throw if the not yet present virtual section is included at this point.
foreach (IChunkState chunk in ionData.CreateSectionDataEnumerable())  // From utilities package
{
    // In this exmaple only zero values are to be written to the section. Simply create an array of the approriate size.
    ReadOnlyMemory<float> data = new float[chunk.Length];
    chunk.WriteSectionData<float>(sectionName, data);

    // Loop back and repeat for next chunk.
}
```

Reading and writing can be combined for writing calculated or conditional data to a new virtual section
```csharp
// ... Same setup as previous examples
IIonData ionData; // Assume a resolved instance of IIonData
string sectionName = "MassGreaterThan30";
Type type = typeof(byte);
ionData.AddVirtualSection(sectionName, type);

// Again note that the new section to be written is not include in the read section list
foreach (IChunkState chunk in ionData.CreateSectionDataEnumerable(IonDataSectionName.Mass))
{
    var masses = chunk.ReadSectionData<float>(IonDataSectionName.Mass).Span;

    var writeData = new byte[chunk.Length];
    for (int i = 0; i < chunk.Length; i++)
    {
        writeData[i] = masses[i] > 30f ? 1 : 0;
    }
    chunk.WriteSectionData<float>(sectionName, writeData);

    // Loop back and repeat for next chunk.
}
```

Unfortunately, there is currently no support for writing a virtual section to the APT file. The lifetime of a virtual section is the same as the analysis set. Each time the analysis set is re-opened the section data will need to be added to the IonData object again. Adding support to write persistent additional sections to the APT file is on the roadmap for future improvements.
