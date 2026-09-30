using UitkForKsp2;
using UitkForKsp2.API;

namespace CommNext.UI.Screen;

public static class UIScreenUtils
{
    public static float GetReferenceScreenScaledWidth()
    {
        return Configuration.ScaledScreenWidth > 0
            ? Configuration.ScaledScreenWidth
            : ReferenceResolution.Width;
    }

    public static float GetScaledReferenceCoordinate(float coordinate)
    {
        var scale = Configuration.CurrentScale;
        return scale > 0f ? coordinate / scale : coordinate;
    }
}
