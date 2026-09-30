using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using CommNext.Network.Bands;
using CommNext.Rendering;
using CommNext.UI.Screen;
using CommNext.UI.Utils;
using KSP;
using KSP.Game;
using KSP.Sim.impl;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommNext.UI;

public class RangePreviewWindowController : MonoBehaviour
{
    private static readonly ManualLogSource Logger =
        BepInEx.Logging.Logger.CreateLogSource("CommNext.RangePreviewWindowController");

    public static WindowOptions WindowOptions = new()
    {
        WindowId = "CommNext_RangePreviewWindow",
        Parent = null,
        IsHidingEnabled = true,
        DisableGameInputForTextFields = true,
        MoveOptions = new MoveOptions
        {
            IsMovingEnabled = true,
            CheckScreenBounds = false
        }
    };

    private UIDocument _window = null!;
    private VisualElement _root = null!;
    private DropdownField _bodyDropdown = null!;
    private DropdownField _antennaDropdown = null!;
    private Label _rangeLabel = null!;
    private Label _typeLabel = null!;
    private bool _isUiInitialized;
    private bool _isWindowOpen;

    private readonly Dictionary<string, CelestialBodyComponent> _bodies =
        new Dictionary<string, CelestialBodyComponent>();

    public bool IsWindowOpen
    {
        get => _isWindowOpen;
        set
        {
            _isWindowOpen = value;
            if (!_isUiInitialized || _root == null)
                return;

            _root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            if (value)
            {
                PopulateChoices();
                AlignWindowToToolbar();
                ApplyPreview();
            }
            else
            {
                ConnectionsRenderer.Instance?.ClearRangePreview();
            }

            MainUIManager.Instance.MapToolbarWindow?.UpdateButtonState();
        }
    }

    private void OnEnable()
    {
        _window = GetComponent<UIDocument>();
        StartCoroutine(InitializeWhenVisualTreeReady());
    }

    private IEnumerator InitializeWhenVisualTreeReady()
    {
        const int maxFrames = 120;
        var frame = 0;

        while ((_window == null || _window.rootVisualElement == null ||
                _window.rootVisualElement.childCount == 0) && frame++ < maxFrames)
            yield return null;

        if (_window == null || _window.rootVisualElement == null ||
            _window.rootVisualElement.childCount == 0)
        {
            Logger.LogError("Range preview UI was not instantiated.");
            yield break;
        }

        _root = _window.rootVisualElement[0];
        _root.StopMouseEventsPropagation();
        _root.CenterByDefault();

        _bodyDropdown = _root.Q<DropdownField>("preview-body-dropdown");
        _antennaDropdown = _root.Q<DropdownField>("preview-antenna-dropdown");
        _rangeLabel = _root.Q<Label>("preview-range-label");
        _typeLabel = _root.Q<Label>("preview-type-label");

        _bodyDropdown.RegisterValueChangedCallback(_ => ApplyPreview());
        _antennaDropdown.RegisterValueChangedCallback(_ => ApplyPreview());

        var closeButton = _root.Q<Button>("close-button");
        closeButton.clicked += () => IsWindowOpen = false;

        _isUiInitialized = true;
        _root.style.display = _isWindowOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (_isWindowOpen)
        {
            PopulateChoices();
            ApplyPreview();
        }

        Logger.LogInfo("Range preview UI initialized.");
    }

    private void PopulateChoices()
    {
        var game = GameManager.Instance?.Game;
        if (game == null)
            return;

        var previousBody = _bodyDropdown.value;
        var previousAntenna = _antennaDropdown.value;

        _bodies.Clear();
        var bodyNames = new List<string>();

        foreach (var body in game.UniverseModel.GetAllCelestialBodies()
                     .OrderBy(b => b.DisplayName))
        {
            var displayName = string.IsNullOrWhiteSpace(body.DisplayName)
                ? body.bodyName
                : body.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName) || _bodies.ContainsKey(displayName))
                continue;

            _bodies[displayName] = body;
            bodyNames.Add(displayName);
        }

        _bodyDropdown.choices = bodyNames;
        _antennaDropdown.choices = CommNextRedux.AntennaCatalog.GetDisplayNames();

        if (!string.IsNullOrEmpty(previousBody) && _bodies.ContainsKey(previousBody))
        {
            _bodyDropdown.SetValueWithoutNotify(previousBody);
        }
        else
        {
            var preferred = GetPreferredBodyName();
            _bodyDropdown.SetValueWithoutNotify(
                !string.IsNullOrEmpty(preferred) && _bodies.ContainsKey(preferred)
                    ? preferred
                    : bodyNames.FirstOrDefault() ?? string.Empty);
        }

        if (!string.IsNullOrEmpty(previousAntenna) &&
            CommNextRedux.AntennaCatalog.FindByDisplayName(previousAntenna) != null)
        {
            _antennaDropdown.SetValueWithoutNotify(previousAntenna);
        }
        else
        {
            _antennaDropdown.SetValueWithoutNotify("RA-15");
        }
    }

    private string GetPreferredBodyName()
    {
        var game = GameManager.Instance?.Game;
        if (game?.ViewController == null)
            return "Kerbin";

        VesselComponent vessel;
        if (game.ViewController.TryGetActiveSimVessel(out vessel) &&
            vessel?.mainBody != null)
        {
            return string.IsNullOrWhiteSpace(vessel.mainBody.DisplayName)
                ? vessel.mainBody.bodyName
                : vessel.mainBody.DisplayName;
        }

        return "Kerbin";
    }

    private void ApplyPreview()
    {
        if (!_isUiInitialized || !_isWindowOpen)
            return;

        CelestialBodyComponent body;
        if (!_bodies.TryGetValue(_bodyDropdown.value ?? string.Empty, out body))
            return;

        var antenna = CommNextRedux.AntennaCatalog.FindByDisplayName(_antennaDropdown.value);
        if (antenna == null)
            return;

        var xBand = NetworkBands.Instance.BandsByCode[NetworkBands.DefaultBand];
        ConnectionsRenderer.Instance.SetRangePreview(body.GlobalId, antenna.RangeMeters, xBand.Color);

        _rangeLabel.text = "Alcance: " + Units.PrintSI(antenna.RangeMeters, Units.SymbolMeters);
        _typeLabel.text = antenna.IsRelay
            ? "X Band · Relé"
            : "X Band · Antena directa";

        Logger.LogInfo(
            $"Preview {body.DisplayName} + {antenna.DisplayName}: {antenna.RangeMeters:F0}m");
    }

    private void AlignWindowToToolbar()
    {
        if (_root == null)
            return;

        var toolbar = MainUIManager.Instance.MapToolbarWindow;
        if (toolbar?.Root == null)
            return;

        var toolbarPosition = toolbar.Root.transform.position;
        _root.transform.position = new Vector3(
            toolbarPosition.x - 95f,
            toolbarPosition.y + toolbar.Height + 12f,
            toolbarPosition.z);
    }
}
