using System;
using System.Collections.Generic;
using System.Reflection;
using KSP.Map;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommNextRedux.Compat
{
    internal static class CollectionCompatExtensions
    {
        internal static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            if (dictionary == null) return default(TValue);
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : default(TValue);
        }
    }

    internal static class UICompatExtensions
    {
        internal static void CenterByDefault(this VisualElement element)
        {
            if (element == null) return;

            Action apply = () =>
            {
                var width = element.resolvedStyle.width;
                var height = element.resolvedStyle.height;
                if (float.IsNaN(width) || float.IsNaN(height) || width <= 0f || height <= 0f)
                    return;

                element.transform.position = new Vector3(
                    (Screen.width - width) * 0.5f,
                    (Screen.height - height) * 0.5f,
                    0f);
            };

            apply();
            element.RegisterCallback<GeometryChangedEvent>(_ => apply());
        }

        internal static void SetDefaultPosition(
            this VisualElement element,
            Func<Vector2, Vector2> positionFactory)
        {
            if (element == null || positionFactory == null) return;

            var applied = false;
            Action apply = () =>
            {
                if (applied) return;
                var width = element.resolvedStyle.width;
                var height = element.resolvedStyle.height;
                if (float.IsNaN(width) || float.IsNaN(height) || width <= 0f || height <= 0f)
                    return;

                var target = positionFactory(new Vector2(width, height));
                element.transform.position = new Vector3(target.x, target.y, 0f);
                applied = true;
            };

            apply();
            element.RegisterCallback<GeometryChangedEvent>(_ => apply());
        }
    }

    internal static class MapFocusCompatExtensions
    {
        private static readonly MethodInfo FocusMethod =
            typeof(Map3DFocusItem).GetMethod(
                "FocusSimObject",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly MethodInfo ControlMethod =
            typeof(Map3DFocusItem).GetMethod(
                "ControlVessel",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static void FocusSimObject(this Map3DFocusItem item)
        {
            if (item == null || FocusMethod == null) return;
            FocusMethod.Invoke(item, null);
        }

        internal static void ControlVessel(this Map3DFocusItem item)
        {
            if (item == null || ControlMethod == null) return;
            ControlMethod.Invoke(item, null);
        }
    }
}
