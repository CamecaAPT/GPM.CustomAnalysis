using CommunityToolkit.Mvvm.ComponentModel;

namespace GPM.CustomAnalyses;

/// <summary>
/// Empty model object to show no editable properties in the Properties panel of an analysis
/// </summary>
/// <remarks>
/// Beginning in Cameca.CustomAnalysis.Utilities v3.7.0,
/// <see cref="Cameca.CustomAnalysis.Utilities.StandardAnalysisNodeBase{T}"/> requires a generic parameters
/// for strong typing of the Properties panel data model. Analyses in this assembly do not have any properties,
/// so creting an empty data object and assigning it to the generic parameter maintains the current behavior
/// in the updated dependency
/// </remarks>
public class EmptyProperties : ObservableObject
{
}
