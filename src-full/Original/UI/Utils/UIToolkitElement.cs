using SpaceWarp.API.Assets;
using UnityEngine.UIElements;

namespace CommNext.UI.Utils;

public class UIToolkitElement
{
    protected VisualElement _root;
    public VisualElement Root => _root;

    public UIToolkitElement(string assetPath)
    {
        var asset = Load(assetPath);
        VisualElement instantiated = null;

        if (asset != null)
        {
            try
            {
                instantiated = asset.Instantiate();
            }
            catch
            {
                instantiated = null;
            }
        }

        // Old CommNext VisualTreeAssets deserialize as empty trees on current Redux.
        // Keep the original asset path but rebuild the equivalent element tree in C#.
        if (instantiated == null || instantiated.childCount == 0)
            instantiated = CommNextRedux.ReduxUiFactory.BuildComponent(assetPath);

        _root = instantiated;
        _root.userData = this;
    }

    public static VisualTreeAsset Load(string assetPath)
    {
        return AssetManager.GetAsset<VisualTreeAsset>(
            $"{CommNextPlugin.ModGuid}/commnext_ui/ui/{assetPath.ToLower()}");
    }
}
