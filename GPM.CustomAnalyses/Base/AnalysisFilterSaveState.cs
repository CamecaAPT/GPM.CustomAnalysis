using System.ComponentModel;

namespace GPM.CustomAnalyses.Base;

/// <summary>
/// A base class wrapper for serializing save state.
/// It is generally expected that a properties class should be saved. By wrapping it in a container,
/// it is more extensible. Future additional save state can extend this class and any saved properties
/// should then not be lost when the save state serialization definition is extended.
/// </summary>
/// <typeparam name="TProperties"></typeparam>
public class AnalysisFilterSaveState<TProperties> where TProperties : INotifyPropertyChanged, new()
{
    public TProperties? Properties { get; init; }
}