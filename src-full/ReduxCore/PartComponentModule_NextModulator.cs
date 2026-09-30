using System;
using KSP.Modules;
using KSP.Sim.Definitions;
using KSP.Sim.impl;

namespace CommNextRedux.Modules
{
    public class PartComponentModule_NextModulator : PartComponentModule
    {
        public override Type PartBehaviourModuleType => typeof(Module_NextModulator);

        public Data_Transmitter DataTransmitter { get; private set; }
        public Data_NextModulator DataModulator { get; private set; }

        public override void OnStart(double universalTime)
        {
            Data_NextModulator modulator;
            if (!DataModules.TryGetByType<Data_NextModulator>(out modulator))
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Data_NextModulator missing on " + Part.PartName);
                return;
            }

            DataModulator = modulator;

            Data_Transmitter transmitter;
            Part.TryGetModuleData<PartComponentModule_DataTransmitter, Data_Transmitter>(out transmitter);
            DataTransmitter = transmitter;
        }
    }
}
