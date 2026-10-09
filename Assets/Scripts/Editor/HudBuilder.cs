using FPSParkour.Bounty;
using FPSParkour.Inventory;
using FPSParkour.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FPSParkour.EditorTools
{
    public static class HudBuilder
    {
        public static GameObject Build(GameObject canvas, PlayerRig rig, CreditsWallet wallet = null)
        {
            EnsureEventSystem();
            BuildCrosshair(canvas.transform);
            BuildVitals(canvas, rig, wallet);
            BuildMomentum(canvas, rig);
            BuildWeaponBar(canvas, rig);
            return BuildInventory(canvas, rig);
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
                return;

            GameObject events = new GameObject("EventSystem");
            events.AddComponent<UnityEngine.EventSystems.EventSystem>();
            events.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        static void BuildCrosshair(Transform canvas)
        {
            Image dot = PlayerRigBuilder.Panel(canvas, "Crosshair", new Color(1f, 1f, 1f, 0.75f));
            RectTransform rect = dot.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(4f, 4f);
            dot.raycastTarget = false;
        }

        static void BuildVitals(GameObject canvas, PlayerRig rig, CreditsWallet wallet)
        {
            Text weapon = PlayerRigBuilder.Corner(canvas.transform, "WeaponName",
                new Vector2(0f, 0f), new Vector2(16f, 126f), new Vector2(300f, 22f), 16);

            Text ammo = PlayerRigBuilder.Corner(canvas.transform, "Ammo",
                new Vector2(0f, 0f), new Vector2(16f, 96f), new Vector2(300f, 28f), 22);

            GameObject barObject = new GameObject("HealthBar", typeof(RectTransform), typeof(Slider));
            barObject.transform.SetParent(canvas.transform, false);
            RectTransform barRect = barObject.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(0f, 0f);
            barRect.pivot = new Vector2(0f, 0f);
            barRect.anchoredPosition = new Vector2(16f, 74f);
            barRect.sizeDelta = new Vector2(260f, 16f);

            Image barTrack = PlayerRigBuilder.Panel(barObject.transform, "Track", new Color(0.1f, 0.11f, 0.13f, 0.8f));
            PlayerRigBuilder.Stretch(barTrack.rectTransform);
            barTrack.raycastTarget = false;

            Slider bar = barObject.GetComponent<Slider>();
            bar.interactable = false;
            bar.transition = Selectable.Transition.None;

            Image fill = PlayerRigBuilder.Panel(barObject.transform, "Fill", new Color(0.75f, 0.25f, 0.22f, 0.95f));
            PlayerRigBuilder.Stretch(fill.rectTransform);
            bar.fillRect = fill.rectTransform;
            bar.value = 1f;

            GameObject reloading = PlayerRigBuilder.Corner(canvas.transform, "Reloading",
                new Vector2(0f, 0f), new Vector2(190f, 96f), new Vector2(200f, 28f), 16).gameObject;
            reloading.GetComponent<Text>().text = "RELOADING";
            reloading.GetComponent<Text>().color = new Color(1f, 0.78f, 0.35f, 1f);
            reloading.SetActive(false);

            Text credits = null;
            if (wallet != null)
            {
                credits = PlayerRigBuilder.Corner(canvas.transform, "Credits",
                    new Vector2(1f, 1f), new Vector2(-16f, -236f), new Vector2(300f, 24f), 16);
                credits.alignment = TextAnchor.UpperRight;
            }

            HudView hud = canvas.AddComponent<HudView>();
            new AssetAuthoring(hud)
                .Ref("stats", rig.Stats).Ref("weapons", rig.Weapons).Ref("wallet", wallet)
                .Ref("healthBar", bar).Ref("ammoLabel", ammo).Ref("weaponLabel", weapon)
                .Ref("creditsLabel", credits).Ref("reloadIndicator", reloading)
                .Save();
        }

        static void BuildMomentum(GameObject canvas, PlayerRig rig)
        {
            Text state = PlayerRigBuilder.Corner(canvas.transform, "MovementState",
                new Vector2(0f, 0f), new Vector2(16f, 48f), new Vector2(320f, 20f), 14);

            Image track = PlayerRigBuilder.Panel(canvas.transform, "SpeedTrack", new Color(0.1f, 0.11f, 0.13f, 0.8f));
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(0f, 0f);
            trackRect.anchorMax = new Vector2(0f, 0f);
            trackRect.pivot = new Vector2(0f, 0f);
            trackRect.anchoredPosition = new Vector2(16f, 28f);
            trackRect.sizeDelta = new Vector2(280f, 12f);
            track.raycastTarget = false;

            Image fill = PlayerRigBuilder.Panel(trackRect, "SpeedFill", Color.white);
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fill.raycastTarget = false;

            RectTransform slide = Mark(trackRect, "SlideThreshold", new Color(0.55f, 0.91f, 0.63f, 0.9f));
            RectTransform wallRun = Mark(trackRect, "WallRunThreshold", new Color(0.45f, 0.75f, 1f, 0.9f));

            MomentumView view = canvas.AddComponent<MomentumView>();
            new AssetAuthoring(view)
                .Ref("movement", rig.Movement)
                .Ref("stateLabel", state)
                .Ref("speedFill", fillRect).Ref("speedFillImage", fill)
                .Ref("slideMark", slide).Ref("wallRunMark", wallRun)
                .Save();
        }

        static RectTransform Mark(Transform track, string name, Color colour)
        {
            Image mark = PlayerRigBuilder.Panel(track, name, colour);
            RectTransform rect = mark.rectTransform;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(2f, 0f);
            mark.raycastTarget = false;
            return rect;
        }

        static void BuildWeaponBar(GameObject canvas, PlayerRig rig)
        {
            Text label = PlayerRigBuilder.Corner(canvas.transform, "WeaponBar",
                new Vector2(1f, 0f), new Vector2(-16f, 16f), new Vector2(280f, 110f), 15);
            label.alignment = TextAnchor.LowerRight;
            label.supportRichText = true;

            WeaponBarView view = canvas.AddComponent<WeaponBarView>();
            new AssetAuthoring(view)
                .Ref("weapons", rig.Weapons).Ref("label", label)
                .Save();
        }

        static GameObject BuildInventory(GameObject canvas, PlayerRig rig)
        {
            InventorySystem inventory = rig.Root.GetComponent<InventorySystem>();

            Image panel = PlayerRigBuilder.Panel(canvas.transform, "InventoryPanel",
                new Color(0.05f, 0.06f, 0.08f, 0.93f));
            RectTransform panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(720f, 460f);

            Text title = PlayerRigBuilder.Label(panelRect, "Title", new Vector2(24f, -20f), new Vector2(400f, 26f), 20);
            title.text = "INVENTORY";

            Text hint = PlayerRigBuilder.Label(panelRect, "Hint", new Vector2(24f, -50f), new Vector2(620f, 20f), 13);
            hint.text = "Tab closes. The game is paused while this is open.";
            hint.color = new Color(0.65f, 0.68f, 0.72f, 1f);

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(panelRect, false);
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = new Vector2(0f, -84f);
            content.offsetMin = new Vector2(24f, content.offsetMin.y);
            content.offsetMax = new Vector2(-24f, content.offsetMax.y);

            VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 4f;

            Button template = BuildRowTemplate(content);

            InventoryGridView view = canvas.AddComponent<InventoryGridView>();
            new AssetAuthoring(view)
                .Ref("inventory", inventory).Ref("root", panel.gameObject)
                .Ref("content", content).Ref("rowPrefab", template)
                .Save();

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        static Button BuildRowTemplate(Transform content)
        {
            GameObject row = new GameObject("RowTemplate", typeof(RectTransform), typeof(Image), typeof(Button));
            row.transform.SetParent(content, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 28f);
            row.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.17f, 0.9f);

            Text label = PlayerRigBuilder.Label(row.transform, "Label", new Vector2(10f, -4f), new Vector2(600f, 20f), 15);
            label.alignment = TextAnchor.MiddleLeft;

            row.SetActive(false);
            return row.GetComponent<Button>();
        }
    }
}
