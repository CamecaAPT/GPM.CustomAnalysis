using System.ComponentModel;
using System.Threading.Tasks;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.Mvvm.Input;
using GPM.CustomAnalyses.Base;

namespace AnalysisFilterTest.Base;

/// <summary>
/// Base view model class that implements an update command and requires update flag that can quickly
/// be used to support a <see cref="Cameca.CustomAnalysis.Utilities.Controls.ButtonOverlayControl" /> at the top
/// level of the custom analysis view. This replicates the standard AP Suite update pattern with parent ion data
/// is changed (due to changing ROIs or ranging, etc)
/// </summary>
internal abstract class AnalysisFilterViewModelBase<TNode, TProperties, TSaveState> : AnalysisViewModelBase<TNode>
    where TNode : NodeBase<TProperties, TSaveState>
    where TProperties : INotifyPropertyChanged, new()
    where TSaveState : AnalysisFilterSaveState<TProperties>, new()
{
    public bool RequiresUpdate
    {
        get => Node!.RequiresUpdate;
        set => Node!.RequiresUpdate = value;
    }

    public AsyncRelayCommand UpdateCommand { get; private set; } = new AsyncRelayCommand(() => Task.CompletedTask);

    protected AnalysisFilterViewModelBase(IAnalysisViewModelBaseServices services) : base(services) { }

    protected override void OnCreated(ViewModelCreatedEventArgs eventArgs)
    {
        base.OnCreated(eventArgs);
        OnCreatedInitializeViewModelBase(eventArgs);
    }

    protected async void OnCreatedInitializeViewModelBase(ViewModelCreatedEventArgs eventArgs)
    {
        if (eventArgs.Mode == ViewModelMode.Preview)
            return;
        UpdateCommand = Node!.UpdateCommand;
        Node!.PropertyChanged += AnalysisOnPropertyChanged;
        await UpdateCommand.ExecuteAsync(null);
    }

    private void AnalysisOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "RequiresUpdate")
            RaisePropertyChanged(nameof(RequiresUpdate));
    }
}