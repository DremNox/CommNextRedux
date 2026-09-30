using System;
using KSP.Modules;
using KSP.Sim.Definitions;
using KSP.Sim.impl;

namespace CommNextRedux.Modules
{
    public class PartComponentModule_NextRelay : PartComponentModule
    {
        public override Type PartBehaviourModuleType => typeof(Module_NextRelay);

        public Data_Transmitter DataTransmitter { get; private set; }
        public Data_NextRelay DataRelay { get; private set; }

        public override void OnStart(double universalTime)
        {
            Data_NextRelay relay;
            if (!DataModules.TryGetByType<Data_NextRelay>(out relay))
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Data_NextRelay missing on " + Part.PartName);
                return;
            }

            DataRelay = relay;

            Data_Transmitter transmitter;
            Part.TryGetModuleData<PartComponentModule_DataTransmitter, Data_Transmitter>(out transmitter);
            DataTransmitter = transmitter;
        }
    }
}
