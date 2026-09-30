-- CommNext Redux full-port: inject the original Relay/Modulator modules
-- into stock antennas using Redux Patch Manager's Lua API.

local function visuals(componentName, displayName)
    return {
        PartComponentModuleName = componentName,
        ModuleDisplayName = displayName,
        ShowHeader = true,
        ShowFooter = true,
    }
end

local function appendVisual(part, entry)
    if part.PAMModuleVisualsOverride ~= nil then
        part.PAMModuleVisualsOverride:Append(entry)
    else
        part.PAMModuleVisualsOverride = { entry }
    end
end

local function addModulator(part, kind)
    part:AddModule("Module_NextModulator", function(module)
        module:AddData("Data_NextModulator", function(data)
            data.ModulatorKind = kind
        end)
    end)

    appendVisual(
        part,
        visuals("PartComponentModule_NextModulator", "PartModules/NextModulator/Name")
    )
end

local function addRelay(part, ec)
    part:AddModule("Module_NextRelay", function(module)
        module:AddData("Data_NextRelay", function(data)
            data.RequiredResource = {
                Rate = ec,
                ResourceName = "ElectricCharge",
                AcceptanceThreshold = 0.1,
            }
        end)
    end)

    appendVisual(
        part,
        visuals("PartComponentModule_NextRelay", "PartModules/NextRelay/Name")
    )
end

local modulators = {
    { id = "antenna_0v_16",                kind = "MonoBand" },
    { id = "antenna_0v_16s",               kind = "MonoBand" },
    { id = "antenna_1v_parabolic_dts-m1",  kind = "DualBand" },
    { id = "antenna_1v_dish_hg55",         kind = "DualBand" },
    { id = "antenna_1v_dish_hg55s",        kind = "DualBand" },
    { id = "antenna_1v_dish_88-88",        kind = "OmniBand" },
}

local relays = {
    { id = "antenna_1v_dish_hg5",    kind = "DualBand", ec = 0.2 },
    { id = "antenna_0v_dish_ra-2",   kind = "OmniBand", ec = 0.5 },
    { id = "antenna_0v_dish_ra-15",  kind = "OmniBand", ec = 1.0 },
    { id = "antenna_1v_dish_ra-100", kind = "OmniBand", ec = 2.0 },
}

for _, p in ipairs(modulators) do
    PM.Parts:Patch("CommNext_Modulator_" .. p.id)
        :Named(p.id)
        :Do(function(part)
            addModulator(part, p.kind)
        end)
end

for _, p in ipairs(relays) do
    PM.Parts:Patch("CommNext_Relay_" .. p.id)
        :Named(p.id)
        :Do(function(part)
            addRelay(part, p.ec)
            addModulator(part, p.kind)
        end)
end
