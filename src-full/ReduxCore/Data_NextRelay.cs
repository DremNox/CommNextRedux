using System;
using KSP.Sim;
using KSP.Sim.Definitions;
using KSP.Game;
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

        public ResourceFlowRequestCommandConfig RequestConfig;

        public override void SetupResourceRequest(ResourceFlowRequestBroker resourceFlowRequestBroker)
        {
            if (RequiredResource.ResourceName == null)
                return;

            var resourceId =
                GameManager.Instance.Game.ResourceDefinitionDatabase.GetResourceIDFromName(RequiredResource.ResourceName);

            if (resourceId == ResourceDefinitionID.InvalidID)
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Resource not found: " + RequiredResource.ResourceName);
                return;
            }

            RequestConfig = new ResourceFlowRequestCommandConfig
            {
                FlowResource = resourceId,
                FlowDirection = FlowDirection.FLOW_OUTBOUND,
                FlowUnits = 0.0
            };

            RequestHandle = resourceFlowRequestBroker.AllocateOrGetRequest("ModuleCommNextRelay", default);
            resourceFlowRequestBroker.SetCommands(RequestHandle, 1.0,
                new ResourceFlowRequestCommandConfig[] { RequestConfig });
        }

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
