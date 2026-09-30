using BepInEx.Logging;
using KSP.Sim.Definitions;
using UnityEngine;
using UnityEngine.Serialization;

namespace CommNext.Modules.Relay;

[DisallowMultipleComponent]
// ReSharper disable once InconsistentNaming
public class Module_NextRelay : PartBehaviourModule
{
    private static readonly ManualLogSource
        Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.Module_NextRelay");

    public override Type PartComponentModuleType => typeof(PartComponentModule_NextRelay);

    [SerializeField]
    protected Data_NextRelay? dataRelay;

    protected override void AddDataModules()
    {
        base.AddDataModules();
        dataRelay ??= new Data_NextRelay();
        DataModules.TryAddUnique(dataRelay, out dataRelay);
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();

        var relay = dataRelay;
        if (relay != null) relay.EnableRelay.OnChangedValue += OnEnableRelayChange;
    }

    private void OnEnableRelayChange(bool isEnabled)
    {
        CommNextRedux.CommNetBridge.RefreshTelemetry(part.partOwner.SimObjectComponent.SimulationObject.Telemetry);
    }

    protected override void OnShutdown()
    {
        base.OnShutdown();
        var relay = dataRelay;
        if (relay != null) relay.EnableRelay.OnChangedValue -= OnEnableRelayChange;
    }
}