using System;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Sim.ResourceSystem;

namespace CommNextRedux.Modules
{
    [Serializable]
    public class Data_NextRelay : ModuleData
    {
        public override Type ModuleType => typeof(Module_NextRelay);

        [KSPState]
        public ModuleProperty<bool> EnableRelay = new ModuleProperty<bool>(true);

        public bool HasResourcesToOperate = true;

        [KSPDefinition]
        public PartModuleResourceSetting RequiredResource;

        public override void OnPartBehaviourModuleInit()
        {
        }

        public override void Copy(ModuleData sourceModuleData)
        {
            var source = sourceModuleData as Data_NextRelay;
            if (source == null) return;
            EnableRelay.SetValue(source.EnableRelay.GetValue());
            HasResourcesToOperate = source.HasResourcesToOperate;
        }
    }
}
