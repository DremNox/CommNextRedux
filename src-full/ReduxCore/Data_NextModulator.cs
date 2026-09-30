using System;
using KSP.Sim;
using KSP.Sim.Definitions;
using CommNextRedux.Network;

namespace CommNextRedux.Modules
{
    [Serializable]
    public class Data_NextModulator : ModuleData
    {
        public override Type ModuleType => typeof(Module_NextModulator);

        [KSPDefinition]
        public ModulatorKind ModulatorKind;

        [KSPState(CopyToSymmetrySet = false)]
        public ModuleProperty<bool> OmniBand = new ModuleProperty<bool>(false);

        [KSPState(CopyToSymmetrySet = false)]
        public ModuleProperty<string> Band = new ModuleProperty<string>(NetworkBands.DefaultBand);

        [KSPState(CopyToSymmetrySet = false)]
        public ModuleProperty<string> SecondaryBand = new ModuleProperty<string>("");

        public override void OnPartBehaviourModuleInit()
        {
        }

        public override void Copy(ModuleData sourceModuleData)
        {
            var source = sourceModuleData as Data_NextModulator;
            if (source == null) return;
            OmniBand.SetValue(source.OmniBand.GetValue());
            Band.SetValue(source.Band.GetValue());
            SecondaryBand.SetValue(source.SecondaryBand.GetValue());
        }
    }
}
