using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GPM.CustomAnalyses.Base;

[ObservableObject]
public abstract partial class NodeBase<TProperties, TSaveState> : StandardAnalysisFilterNodeBase
    where TProperties : INotifyPropertyChanged, new()
    where TSaveState : AnalysisFilterSaveState<TProperties>, new()
{
    protected TSaveState? SaveState = null;
    private bool _requiresUpdate = true;

    protected NodeBase(IStandardAnalysisFilterNodeBaseServices services) : base(services)
    {
        UpdateCommand = new AsyncRelayCommand(OnUpdateExecuted);
        PropertyChanged += OnPropertyChanged;
    }

    public bool RequiresUpdate
    {
        get => _requiresUpdate;
        set => SetProperty(ref _requiresUpdate, value);
    }

    public AsyncRelayCommand UpdateCommand { get; }

    internal Task<IIonData?> GetInputIonData(IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        => Services.IonDataProvider.GetOwnerIonData(InstanceId, progress, cancellationToken);

    protected sealed override async IAsyncEnumerable<ReadOnlyMemory<ulong>> GetIndicesDelegateAsync(IIonData ownerIonData, IProgress<double>? progress, [EnumeratorCancellation] CancellationToken token)
    {
        await OnUpdate(ownerIonData, progress, token);
        await foreach (var chunk in OutputIndices(ownerIonData, progress, token))
        {
            yield return chunk;
        }
    }

    protected sealed override byte[]? GetSaveContent() => JsonSerializer.SerializeToUtf8Bytes(Save());

    protected void LoadFromBytes(byte[]? data)
    {
        TSaveState? saveState = null;
        if (data is not null)
        {
            try
            {
                saveState = JsonSerializer.Deserialize<TSaveState>(data);
            }
            catch (Exception ex) when (ex is NotSupportedException or JsonException)
            {
            }
        }
        Load(saveState);
    }

    protected virtual void Load(TSaveState? saveState)
    {
        SaveState = saveState;
        if (saveState is null) return;
        if (saveState.Properties is not null)
            Properties = saveState.Properties;
    }

    protected override async void OnAdded(NodeAddedEventArgs eventArgs)
    {
        base.OnAdded(eventArgs);
        // If this is being added for the first time (i.e. created, not loaded), then immediately trigger the calculation
        // This is how AP Suite handled recalculation when a new analysis is added
        if (eventArgs.Trigger == EventTrigger.Create)
        {
            await UpdateCommand.ExecuteAsync(null);
        }
    }

    protected override void OnCreated(NodeCreatedEventArgs eventArgs)
    {
        base.OnCreated(eventArgs);
        if (DataState is { } dataState)
        {
            dataState.PropertyChanged += DataStateOnPropertyChanged;
        }
        if (eventArgs.Trigger is EventTrigger.Load)
        {
            LoadFromBytes(eventArgs.Data);
        }
    } 
    
    protected virtual Task OnUpdate(IIonData inputIonData, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
    {
        RequiresUpdate = false;
        return Task.CompletedTask;
    }

    protected virtual async Task OnUpdateExecuted()
    {
        if (await GetInputIonData() is not { } ionData) return;
        // Retrieving IonData will mark node as valid
        // We are delegating further update logic to analysis object, so valid state should
        // depend on successful completion of delegated update task
        try
        {
            if (Services.ProgressDialogProvider.Resolve(InstanceId) is { } progressDialog)
            {
                await progressDialog.ShowDialog(
                    "Updating",
                    (p, t) => OnUpdate(ionData, p, t));
            }
            else
            {
                await OnUpdate(ionData);
            }
            // Now we can consider the node to be valid
            DataStateIsValid = true;
        }
        catch (TaskCanceledException)
        {
            DataStateIsValid = false;

        }
    }

#pragma warning disable CS1998
    protected virtual async IAsyncEnumerable<ReadOnlyMemory<ulong>> OutputIndices(
        IIonData inputIonData,
        IProgress<double>? progress = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
#pragma warning restore CS1998
    {
        yield return ReadOnlyMemory<ulong>.Empty;
    }
    
    protected virtual TSaveState Save()
    {
        return new TSaveState()
        {
            Properties = Properties is TProperties p ? p : default,
        };
    }

    private void DataStateOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Don't directly link data state to requires update, analysis update method may want
        // to require additional work and should be the final source to mark no update as requires.
        // Here only mark update as required if data data is change to invalid.
        if (DataState is not null && !DataState.IsValid)
            RequiresUpdate = true;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The default behavior shall be that any changes to node properties will invalidate the node state
        if (Properties is INotifyPropertyChanged properties)
        {
            properties.PropertyChanged += (o, args) => DataStateIsValid = false;
        }
    }




}