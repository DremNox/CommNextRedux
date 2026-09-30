using System;
using CommNext.Unity.Runtime.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommNextRedux
{
    internal static class ReduxUiFactory
    {
        private static readonly Color Background = new Color(0.055f, 0.065f, 0.085f, 0.96f);
        private static readonly Color Panel = new Color(0.10f, 0.12f, 0.16f, 0.98f);
        private static readonly Color Border = new Color(0.36f, 0.38f, 0.86f, 1f);
        private static readonly Color Text = new Color(0.82f, 0.85f, 0.91f, 1f);
        private static readonly Color Accent = new Color(0.10f, 1f, 0.40f, 1f);
        private static readonly Color Muted = new Color(0.48f, 0.52f, 0.62f, 1f);

        internal static VisualElement BuildMapToolbar()
        {
            var root = Box("toolbar", 232, 42);
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.SpaceAround;
            root.style.paddingLeft = 5;
            root.style.paddingRight = 5;

            root.Add(ToolbarButton("lines-button", "NET"));
            root.Add(ToolbarButton("rulers-button", "RNG"));
            root.Add(ToolbarButton("vessel-report-button", "REP"));
            root.Add(ToolbarButton("preview-button", "PREV"));

            return root;
        }

        internal static VisualElement BuildRangePreview()
        {
            var root = Box("range-preview-root", 330, 178);
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;

            var header = Row();
            header.style.height = 30;
            header.style.justifyContent = Justify.SpaceBetween;

            var title = new Label("PREVIEW DE ALCANCE");
            title.style.color = Accent;
            title.style.fontSize = 13;
            header.Add(title);

            var close = new Button { name = "close-button", text = "X" };
            StyleSmallButton(close, 28);
            header.Add(close);
            root.Add(header);

            var bodyRow = Row();
            bodyRow.style.height = 38;
            var bodyLabel = new Label("CUERPO");
            bodyLabel.style.width = 78;
            bodyLabel.style.fontSize = 10;
            bodyLabel.style.color = Muted;
            bodyRow.Add(bodyLabel);
            var bodyDropdown = new DropdownField { name = "preview-body-dropdown" };
            bodyDropdown.style.flexGrow = 1;
            bodyRow.Add(bodyDropdown);
            root.Add(bodyRow);

            var antennaRow = Row();
            antennaRow.style.height = 38;
            var antennaLabel = new Label("ANTENA");
            antennaLabel.style.width = 78;
            antennaLabel.style.fontSize = 10;
            antennaLabel.style.color = Muted;
            antennaRow.Add(antennaLabel);
            var antennaDropdown = new DropdownField { name = "preview-antenna-dropdown" };
            antennaDropdown.style.flexGrow = 1;
            antennaRow.Add(antennaDropdown);
            root.Add(antennaRow);

            var range = new Label("Alcance: -") { name = "preview-range-label" };
            range.style.marginTop = 7;
            range.style.color = Text;
            root.Add(range);

            var type = new Label("X Band") { name = "preview-type-label" };
            type.style.marginTop = 3;
            type.style.fontSize = 11;
            type.style.color = Muted;
            root.Add(type);

            return root;
        }

        internal static VisualElement BuildVesselReport()
        {
            var root = Box("root", 420, 548);
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 8;
            root.style.paddingBottom = 8;

            var header = Row();
            header.name = "toolbar";
            header.style.height = 34;
            header.style.justifyContent = Justify.SpaceBetween;

            var title = new Label("VESSEL COMMS REPORT");
            title.style.color = Accent;
            title.style.fontSize = 14;
            header.Add(title);

            var close = new Button { name = "close-button", text = "X" };
            StyleSmallButton(close, 28);
            header.Add(close);
            root.Add(header);

            var vesselRow = Row();
            vesselRow.name = "vessel-row";
            vesselRow.style.marginTop = 5;
            vesselRow.style.marginBottom = 7;

            var nameLabel = new Label("Vessel") { name = "name-label" };
            nameLabel.style.flexGrow = 1;
            nameLabel.style.color = new Color(0.55f, 0.65f, 0.85f, 1f);
            vesselRow.Add(nameLabel);

            var powerIcon = new VisualElement { name = "power-icon" };
            powerIcon.style.width = 14;
            powerIcon.style.height = 14;
            powerIcon.style.backgroundColor = new Color(1f, 0.36f, 0.28f, 1f);
            powerIcon.style.marginRight = 6;
            vesselRow.Add(powerIcon);

            var rangeLabel = new Label("Range") { name = "range-label" };
            rangeLabel.enableRichText = true;
            rangeLabel.style.marginRight = 6;
            vesselRow.Add(rangeLabel);

            var focus = new Button { name = "focus-button", text = "Focus" };
            StyleSmallButton(focus, 56);
            vesselRow.Add(focus);
            root.Add(vesselRow);

            var controls = Row();
            controls.style.height = 36;
            controls.style.marginBottom = 6;

            var filter = new DropdownField { name = "filter-dropdown" };
            filter.style.flexGrow = 1;
            filter.style.marginRight = 4;
            controls.Add(filter);

            var sort = new DropdownField { name = "sort-dropdown" };
            sort.style.flexGrow = 1;
            sort.style.marginRight = 4;
            controls.Add(sort);

            var direction = new SortDirectionButton { name = "sort-direction-button", text = "⇅" };
            direction.direction = SortDirection.Descending;
            StyleSmallButton(direction, 34);
            controls.Add(direction);
            root.Add(controls);

            var signalControlRow = Row();
            signalControlRow.style.height = 32;
            signalControlRow.style.marginBottom = 6;

            var signalControlLabel = new Label("CONTROL SIN SEÑAL");
            signalControlLabel.style.width = 145;
            signalControlLabel.style.fontSize = 10;
            signalControlLabel.style.color = Muted;
            signalControlRow.Add(signalControlLabel);

            var signalControl = new DropdownField { name = "control-mode-dropdown" };
            signalControl.style.flexGrow = 1;
            signalControlRow.Add(signalControl);
            root.Add(signalControlRow);

            var list = new ScrollView { name = "connections-list" };
            list.style.flexGrow = 1;
            list.style.minHeight = 260;
            root.Add(list);

            var bands = new VisualElement { name = "bands-list" };
            bands.style.flexShrink = 0;
            bands.style.marginTop = 6;
            root.Add(bands);

            return root;
        }

        internal static VisualElement BuildTooltip()
        {
            var root = new VisualElement { name = "tooltip-root", pickingMode = PickingMode.Ignore };
            root.style.position = Position.Absolute;
            root.style.left = 0;
            root.style.top = 0;
            root.style.right = 0;
            root.style.bottom = 0;

            var tooltip = new VisualElement { name = "tooltip", pickingMode = PickingMode.Ignore };
            tooltip.style.position = Position.Absolute;
            tooltip.style.backgroundColor = new Color(0.05f, 0.06f, 0.08f, 0.96f);
            tooltip.style.borderTopWidth = 1;
            tooltip.style.borderBottomWidth = 1;
            tooltip.style.borderLeftWidth = 1;
            tooltip.style.borderRightWidth = 1;
            tooltip.style.borderTopColor = Border;
            tooltip.style.borderBottomColor = Border;
            tooltip.style.borderLeftColor = Border;
            tooltip.style.borderRightColor = Border;
            tooltip.style.paddingLeft = 6;
            tooltip.style.paddingRight = 6;
            tooltip.style.paddingTop = 4;
            tooltip.style.paddingBottom = 4;
            tooltip.style.opacity = 0;

            var border = new VisualElement { name = "tooltip__border", pickingMode = PickingMode.Ignore };
            tooltip.Add(border);

            var text = new Label("") { name = "tooltip__text", pickingMode = PickingMode.Ignore };
            text.style.color = Text;
            text.style.fontSize = 11;
            tooltip.Add(text);
            root.Add(tooltip);
            return root;
        }

        internal static VisualElement BuildComponent(string assetPath)
        {
            var normalized = (assetPath ?? "").Replace('\\', '/').ToLowerInvariant();
            if (normalized.EndsWith("components/bandrow.uxml"))
                return BuildBandRow();
            if (normalized.EndsWith("components/networkconnectionview.uxml"))
                return BuildNetworkConnectionRow();

            var fallback = new VisualElement();
            fallback.style.flexGrow = 1;
            return fallback;
        }

        private static VisualElement BuildBandRow()
        {
            var outer = new VisualElement();
            outer.style.flexShrink = 0;
            outer.style.paddingLeft = 2;
            outer.style.paddingRight = 2;
            outer.style.paddingTop = 1;
            outer.style.paddingBottom = 1;

            var row = Row();
            row.style.backgroundColor = Panel;
            row.style.paddingLeft = 5;
            row.style.paddingRight = 5;
            row.style.paddingTop = 3;
            row.style.paddingBottom = 3;

            var icon = new BandIcon { name = "band-icon" };
            icon.SetBand("X", new Color(0.65f, 0.30f, 0.85f, 1f));
            icon.style.width = 26;
            icon.style.marginRight = 6;
            row.Add(icon);

            var name = new Label("Band") { name = "name-label" };
            name.style.width = 115;
            name.style.fontSize = 12;
            row.Add(name);

            var range = new Label("Range") { name = "range-label" };
            range.enableRichText = true;
            range.style.flexGrow = 1;
            range.style.fontSize = 12;
            row.Add(range);

            var toggle = new Toggle { name = "activate-toggle", value = false };
            row.Add(toggle);

            outer.Add(row);
            return outer;
        }

        private static VisualElement BuildNetworkConnectionRow()
        {
            var spacer = new VisualElement();
            spacer.style.paddingTop = 2;
            spacer.style.paddingBottom = 2;

            var row = Row();
            row.name = "row__container";
            row.style.backgroundColor = Panel;
            row.style.paddingLeft = 6;
            row.style.paddingRight = 6;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.minHeight = 54;

            var connectionIcon = new VisualElement { name = "connection-icon" };
            connectionIcon.style.width = 12;
            connectionIcon.style.height = 32;
            connectionIcon.style.backgroundColor = Accent;
            connectionIcon.style.marginRight = 7;
            row.Add(connectionIcon);

            var content = new VisualElement();
            content.style.flexGrow = 1;

            var top = Row();
            var signal = new SignalStrengthIcon { name = "signal-strength-icon" };
            signal.style.width = 20;
            signal.style.marginRight = 3;
            top.Add(signal);

            var band = new BandIcon { name = "band-icon" };
            band.SetBand("X", new Color(0.70f, 0.25f, 0.85f, 1f));
            band.style.width = 24;
            band.style.marginRight = 4;
            top.Add(band);

            var name = new Label("Relay") { name = "name-label" };
            name.style.flexGrow = 1;
            name.style.fontSize = 13;
            top.Add(name);

            var power = new VisualElement { name = "power-icon" };
            power.style.width = 12;
            power.style.height = 12;
            power.style.backgroundColor = new Color(1f, 0.36f, 0.28f, 1f);
            power.style.marginRight = 4;
            top.Add(power);

            var directionTag = Row();
            directionTag.name = "direction-tag";
            directionTag.style.backgroundColor = new Color(0.14f, 0.16f, 0.20f, 1f);
            directionTag.style.paddingLeft = 4;
            directionTag.style.paddingRight = 4;

            var directionLabel = new Label("In") { name = "direction-label" };
            directionLabel.style.fontSize = 10;
            directionTag.Add(directionLabel);

            var directionIcon = new VisualElement { name = "direction-icon" };
            directionIcon.style.width = 8;
            directionIcon.style.height = 8;
            directionIcon.style.backgroundColor = Accent;
            directionTag.Add(directionIcon);
            top.Add(directionTag);

            content.Add(top);

            var details = new Label("Distance") { name = "details-label" };
            details.enableRichText = true;
            details.style.color = Muted;
            details.style.fontSize = 11;
            details.style.marginTop = 3;
            content.Add(details);
            row.Add(content);

            var control = new Button { name = "control-button", text = "Control" };
            StyleSmallButton(control, 62);
            control.style.marginLeft = 5;
            row.Add(control);

            spacer.Add(row);
            return spacer;
        }

        private static VisualElement Box(string name, float width, float height)
        {
            var root = new VisualElement { name = name };
            root.style.width = width;
            root.style.height = height;
            root.style.backgroundColor = Background;
            root.style.borderTopWidth = 1;
            root.style.borderBottomWidth = 2;
            root.style.borderLeftWidth = 1;
            root.style.borderRightWidth = 1;
            root.style.borderTopColor = Border;
            root.style.borderBottomColor = Border;
            root.style.borderLeftColor = Border;
            root.style.borderRightColor = Border;
            root.style.borderTopLeftRadius = 7;
            root.style.borderTopRightRadius = 7;
            root.style.borderBottomLeftRadius = 7;
            root.style.borderBottomRightRadius = 7;
            root.style.color = Text;
            return root;
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            return row;
        }

        private static Button ToolbarButton(string name, string text)
        {
            var button = new Button { name = name, text = text };
            button.style.width = 49;
            button.style.height = 28;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            button.style.backgroundColor = Panel;
            button.style.color = Text;
            button.style.fontSize = 10;
            return button;
        }

        private static void StyleSmallButton(Button button, float width)
        {
            button.style.width = width;
            button.style.height = 26;
            button.style.backgroundColor = new Color(0.13f, 0.15f, 0.20f, 1f);
            button.style.color = Text;
            button.style.fontSize = 11;
        }
    }
}
