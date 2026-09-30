using CommNext.Data;
using CommNext.Rendering;
using CommNext.UI;
using UnityEngine;

namespace CommNext.Managers;

/// <summary>
/// Redux persistence adapter for the original CommNext UI state.
/// The old SpaceWarp ModSaves API is no longer present, so these UI preferences
/// are persisted through PlayerPrefs. They are intentionally UI-only and do
/// not alter vessel/save-game state.
/// </summary>
public class SaveManager
{
    public static SaveManager Instance { get; } = new SaveManager();

    private const string Prefix = "CommNextRedux.";
    private SaveData? _loadedSaveData;

    private SaveManager() { }

    public void Register()
    {
        LoadFromPrefs();
    }

    public void SaveNow()
    {
        try
        {
            if (MainUIManager.Instance.MapToolbarWindow != null)
            {
                var position = MainUIManager.Instance.MapToolbarWindow.Position;
                if (position != Vector3.zero)
                {
                    PlayerPrefs.SetInt(Prefix + "Toolbar.HasPosition", 1);
                    PlayerPrefs.SetFloat(Prefix + "Toolbar.X", position.x);
                    PlayerPrefs.SetFloat(Prefix + "Toolbar.Y", position.y);
                    PlayerPrefs.SetFloat(Prefix + "Toolbar.Z", position.z);
                }
            }

            if (ConnectionsRenderer.Instance != null)
            {
                PlayerPrefs.SetInt(
                    Prefix + "ConnectionsDisplayMode",
                    (int)ConnectionsRenderer.Instance.ConnectionsDisplayMode);
                PlayerPrefs.SetInt(
                    Prefix + "RulersDisplayMode",
                    (int)ConnectionsRenderer.Instance.RulersDisplayMode);
            }

            PlayerPrefs.Save();
        }
        catch (System.Exception ex)
        {
            CommNextRedux.CommNetBridge.Log?.LogError(
                "[CommNextRedux] Saving original UI state: " + ex);
        }
    }

    private void LoadFromPrefs()
    {
        try
        {
            var data = new SaveData();

            if (PlayerPrefs.GetInt(Prefix + "Toolbar.HasPosition", 0) != 0)
            {
                data.MapToolbarPosition = new Vector3(
                    PlayerPrefs.GetFloat(Prefix + "Toolbar.X", 0f),
                    PlayerPrefs.GetFloat(Prefix + "Toolbar.Y", 0f),
                    PlayerPrefs.GetFloat(Prefix + "Toolbar.Z", 0f));
            }

            if (PlayerPrefs.HasKey(Prefix + "ConnectionsDisplayMode"))
                data.ConnectionsDisplayMode =
                    (ConnectionsDisplayMode)PlayerPrefs.GetInt(
                        Prefix + "ConnectionsDisplayMode");

            if (PlayerPrefs.HasKey(Prefix + "RulersDisplayMode"))
                data.RulersDisplayMode =
                    (RulersDisplayMode)PlayerPrefs.GetInt(
                        Prefix + "RulersDisplayMode");

            _loadedSaveData = data;
        }
        catch (System.Exception ex)
        {
            CommNextRedux.CommNetBridge.Log?.LogError(
                "[CommNextRedux] Loading original UI state: " + ex);
        }
    }

    public void LoadDataIntoUI()
    {
        if (_loadedSaveData == null) return;
        if (MainUIManager.Instance.MapToolbarWindow == null) return;

        if (_loadedSaveData.MapToolbarPosition.HasValue &&
            _loadedSaveData.MapToolbarPosition.Value != Vector3.zero)
            MainUIManager.Instance.MapToolbarWindow.Position =
                _loadedSaveData.MapToolbarPosition.Value;

        if (ConnectionsRenderer.Instance != null)
        {
            if (_loadedSaveData.RulersDisplayMode.HasValue)
                ConnectionsRenderer.Instance.RulersDisplayMode =
                    _loadedSaveData.RulersDisplayMode.Value;

            if (_loadedSaveData.ConnectionsDisplayMode.HasValue)
                ConnectionsRenderer.Instance.ConnectionsDisplayMode =
                    _loadedSaveData.ConnectionsDisplayMode.Value;
        }

        MainUIManager.Instance.MapToolbarWindow.UpdateButtonState();
        _loadedSaveData = null;
    }
}
