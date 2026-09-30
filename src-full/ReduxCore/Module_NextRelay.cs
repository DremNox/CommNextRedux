using System;
using KSP.Sim.Definitions;
using UnityEngine;

namespace CommNextRedux.Modules
{
    [DisallowMultipleComponent]
    public class Module_NextRelay : PartBehaviourModule
    {
        public override Type PartComponentModuleType => typeof(PartComponentModule_NextRelay);

        [SerializeField]
        protected Data_NextRelay dataRelay;

        protected override void AddDataModules()
        {
            base.AddDataModules();
            if (dataRelay == null) dataRelay = new Data_NextRelay();
            DataModules.TryAddUnique(dataRelay, out dataRelay);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (dataRelay != null)
                dataRelay.EnableRelay.OnChangedValue += OnEnableRelayChange;
        }

        private void OnEnableRelayChange(bool isEnabled)
        {
            if (part != null && part.partOwner != null && part.partOwner.SimObjectComponent != null)
                CommNetBridge.RefreshTelemetry(part.partOwner.SimObjectComponent.SimulationObject.Telemetry);
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            if (dataRelay != null)
                dataRelay.EnableRelay.OnChangedValue -= OnEnableRelayChange;
        }
    }
}
