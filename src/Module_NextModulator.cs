using System;
using KSP.Sim.Definitions;
using UnityEngine;

namespace CommNextRedux.Modules
{
    [DisallowMultipleComponent]
    public class Module_NextModulator : PartBehaviourModule
    {
        public override Type PartComponentModuleType => typeof(PartComponentModule_NextModulator);

        [SerializeField]
        protected Data_NextModulator dataModulator;

        protected override void AddDataModules()
        {
            base.AddDataModules();
            if (dataModulator == null) dataModulator = new Data_NextModulator();
            DataModules.TryAddUnique(dataModulator, out dataModulator);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (dataModulator == null) return;
            dataModulator.OmniBand.OnChangedValue += OnOmniBandChanged;
            dataModulator.Band.OnChangedValue += OnBandChanged;
            dataModulator.SecondaryBand.OnChangedValue += OnBandChanged;
        }

        private void OnOmniBandChanged(bool value)
        {
            Refresh();
        }

        private void OnBandChanged(string value)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (part != null && part.partOwner != null && part.partOwner.SimObjectComponent != null)
                CommNetBridge.RefreshTelemetry(part.partOwner.SimObjectComponent.SimulationObject.Telemetry);
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
            if (dataModulator == null) return;
            dataModulator.OmniBand.OnChangedValue -= OnOmniBandChanged;
            dataModulator.Band.OnChangedValue -= OnBandChanged;
            dataModulator.SecondaryBand.OnChangedValue -= OnBandChanged;
        }
    }
}
