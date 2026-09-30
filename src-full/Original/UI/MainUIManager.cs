using BepInEx.Logging;
using CommNext.UI.Tooltip;
using UitkForKsp2.API;
using UnityEngine.UIElements;

namespace CommNext.UI;

public class MainUIManager
{
    public static MainUIManager Instance { get; set; } = new();

    private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("CommNext.MainUIManager");

    public MapToolbarWindowController MapToolbarWindow { get; set; } = null!;
    private UIDocument _mapToolbarDocument = null!;

    public VesselReportWindowController? VesselReportWindow { get; set; }
    private UIDocument _vesselReportDocument = null!;

    public TooltipWindowController TooltipWindow { get; set; } = null!;
    private UIDocument _tooltipDocument = null!;

    public void Initialize()
    {
        Logger.LogInfo("Initializing Redux-native UI");

        _mapToolbarDocument = Window.Create(
            MapToolbarWindowController.WindowOptions,
            CommNextRedux.ReduxUiFactory.BuildMapToolbar());
        MapToolbarWindow = _mapToolbarDocument.gameObject.AddComponent<MapToolbarWindowController>();

        _vesselReportDocument = Window.Create(
            VesselReportWindowController.WindowOptions,
            CommNextRedux.ReduxUiFactory.BuildVesselReport());
        VesselReportWindow = _vesselReportDocument.gameObject.AddComponent<VesselReportWindowController>();

        // Tooltip is created last so it remains above the other windows.
        _tooltipDocument = Window.Create(
            TooltipWindowController.WindowOptions,
            CommNextRedux.ReduxUiFactory.BuildTooltip());
        TooltipWindow = _tooltipDocument.gameObject.AddComponent<TooltipWindowController>();

        Logger.LogInfo(
            $"Redux-native UI documents created: toolbar={_mapToolbarDocument.rootVisualElement.childCount}, " +
            $"report={_vesselReportDocument.rootVisualElement.childCount}, tooltip={_tooltipDocument.rootVisualElement.childCount}");
    }
}
