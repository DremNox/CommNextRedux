using System;
using KSP.Modules;
using KSP.Sim.Definitions;
using KSP.Sim.impl;
using KSP.Sim.ResourceSystem;

namespace CommNextRedux.Modules
{
    public class PartComponentModule_NextRelay : PartComponentModule
    {
        public override Type PartBehaviourModuleType => typeof(Module_NextRelay);

        public Data_Transmitter DataTransmitter { get; private set; }
        public Data_NextRelay DataRelay { get; private set; }

        private FlowRequestResolutionState _returnedRequestResolutionState;
        private bool _hasOutstandingRequest;
        private bool _lastHasResourcesToOperate = true;

        public override void OnStart(double universalTime)
        {
            Data_NextRelay relay;
            if (!DataModules.TryGetByType<Data_NextRelay>(out relay))
            {
                CommNetBridge.Log?.LogError("[CommNextRedux] Data_NextRelay missing on " + Part.PartName);
                return;
            }

            DataRelay = relay;
            DataRelay.SetupResourceRequest(resourceFlowRequestBroker);

            Data_Transmitter transmitter;
            Part.TryGetModuleData<PartComponentModule_DataTransmitter, Data_Transmitter>(out transmitter);
            DataTransmitter = transmitter;
        }

        public override void OnUpdate(double universalTime, double deltaUniversalTime)
        {
            if (DataRelay == null)
                return;

            _lastHasResourcesToOperate = DataRelay.HasResourcesToOperate;

            if (!DataRelay.EnableRelay.GetValue())
            {
                if (resourceFlowRequestBroker.IsRequestActive(DataRelay.RequestHandle))
                    resourceFlowRequestBroker.SetRequestInactive(DataRelay.RequestHandle);

                DataRelay.HasResourcesToOperate = false;
                _hasOutstandingRequest = false;
                RefreshIfPowerChanged();
                return;
            }

            if (_hasOutstandingRequest)
            {
                _returnedRequestResolutionState =
                    resourceFlowRequestBroker.GetRequestState(DataRelay.RequestHandle);
                DataRelay.HasResourcesToOperate =
                    _returnedRequestResolutionState.WasLastTickDeliveryAccepted;
            }

            if (resourceFlowRequestBroker.IsRequestInactive(DataRelay.RequestHandle))
                resourceFlowRequestBroker.SetRequestActive(DataRelay.RequestHandle);

            DataRelay.RequestConfig.FlowUnits = (double)DataRelay.RequiredResource.Rate;
            resourceFlowRequestBroker.SetCommands(
                DataRelay.RequestHandle,
                1.0,
                new ResourceFlowRequestCommandConfig[] { DataRelay.RequestConfig });

            _hasOutstandingRequest = true;
            RefreshIfPowerChanged();
        }

        private void RefreshIfPowerChanged()
        {
            if (_lastHasResourcesToOperate == DataRelay.HasResourcesToOperate)
                return;

            if (Part != null && Part.PartOwner != null && Part.PartOwner.SimulationObject != null)
                CommNetBridge.RefreshTelemetry(Part.PartOwner.SimulationObject.Telemetry);
        }
    }
}
