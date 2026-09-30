using System;
using BepInEx.Logging;
using CommNext.UI.Tooltip;
using SpaceWarp.API.Assets;
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
        Logger.LogInfo("Initializing UI");

        // Redux/UITK compatibility:
        // Window.Create() creates the UIDocument correctly, but on current Redux it
        // does not populate rootVisualElement from the supplied VisualTreeAsset.
        // Clone the tree explicitly before adding the controller so OnEnable sees
        // the complete hierarchy immediately.
        _mapToolbarDocument = CreateWindow(
            MapToolbarWindowController.WindowOptions,
            $"{CommNextPlugin.ModGuid}/commnext_ui/ui/commnextmaptoolbar.uxml",
            "map toolbar");
        MapToolbarWindow = _mapToolbarDocument.gameObject.AddComponent<MapToolbarWindowController>();

        _vesselReportDocument = CreateWindow(
            VesselReportWindowController.WindowOptions,
            $"{CommNextPlugin.ModGuid}/commnext_ui/ui/vesselreportwindow.uxml",
            "vessel report");
        VesselReportWindow = _vesselReportDocument.gameObject.AddComponent<VesselReportWindowController>();

        // Tooltip should be created last so it renders over the other windows.
        _tooltipDocument = CreateWindow(
            TooltipWindowController.WindowOptions,
            $"{CommNextPlugin.ModGuid}/commnext_ui/ui/tooltipwindow.uxml",
            "tooltip");
        TooltipWindow = _tooltipDocument.gameObject.AddComponent<TooltipWindowController>();
    }

    private static UIDocument CreateWindow(WindowOptions options, string assetPath, string label)
    {
        var tree = AssetManager.GetAsset<VisualTreeAsset>(assetPath);
        if (tree == null)
            throw new InvalidOperationException($"CommNext {label} VisualTreeAsset not found: {assetPath}");

        var document = Window.Create(options, tree);
        if (document == null)
            throw new InvalidOperationException($"CommNext {label} Window.Create returned null.");

        var root = document.rootVisualElement;
        if (root == null)
            throw new InvalidOperationException($"CommNext {label} UIDocument has no rootVisualElement.");

        if (root.childCount == 0)
        {
            tree.CloneTree(root);
            Logger.LogInfo($"Redux manual UXML clone: {label}, children={root.childCount}");
        }
        else
        {
            Logger.LogInfo($"UITK populated UXML normally: {label}, children={root.childCount}");
        }

        return document;
    }
}
