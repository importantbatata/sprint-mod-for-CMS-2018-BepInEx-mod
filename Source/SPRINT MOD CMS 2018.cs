using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CMS2018Sprint
{
    [BepInPlugin("cms2018.sprint.standalone", "SPRINT MOD CMS 2018", "1.0.0")]
    public sealed class SprintMod : BaseUnityPlugin
    {
        internal static SprintMod Instance;
        ConfigEntry<bool> enabledSetting, toggleSetting, crouchSetting;
        ConfigEntry<float> percent;
        ConfigEntry<KeyCode> key;
        Harmony harmony;
        FPSCamera camera;
        PauseQuitMenu menu;
        CanvasGroup menuGroup;
        bool oldInteractable, open, rebinding, toggled;
        float nextCameraSearch;
        Rect window;

        void Awake()
        {
            Instance = this;
            enabledSetting = Config.Bind("Sprint", "Enabled", true);
            percent = Config.Bind("Sprint", "SpeedPercent", 150f,
                new ConfigDescription("Speed relative to normal walking.", new AcceptableValueRange<float>(100f, 200f)));
            key = Config.Bind("Controls", "SprintKey", KeyCode.LeftShift);
            toggleSetting = Config.Bind("Controls", "ToggleSprint", false);
            crouchSetting = Config.Bind("Sprint", "AllowWhileCrouching", false);
            harmony = new Harmony("cms2018.sprint.standalone");
            harmony.PatchAll(typeof(SprintMod).Assembly);
            Logger.LogInfo("Sprint loaded. Escape > Sprint Mod Settings.");
        }

        void Update()
        {
            if (camera == null && Time.unscaledTime >= nextCameraSearch)
            {
                camera = UnityEngine.Object.FindObjectOfType<FPSCamera>();
                nextCameraSearch = Time.unscaledTime + 1f;
            }
            if (open)
            {
                toggled = false;
                if (menu == null || !menu.gameObject.activeInHierarchy) { Close(); return; }
                if (Input.GetKeyDown(KeyCode.Escape)) { if (rebinding) rebinding = false; else Close(); }
                if (rebinding)
                {
                    foreach (KeyCode candidate in Enum.GetValues(typeof(KeyCode)))
                    {
                        if (candidate == KeyCode.None || candidate == KeyCode.Escape || (int)candidate >= (int)KeyCode.Mouse0) continue;
                        if (Input.GetKeyDown(candidate)) { key.Value = candidate; rebinding = false; break; }
                    }
                }
                return;
            }
            if (!CanSprint()) { toggled = false; return; }
            if (toggleSetting.Value && KeyDown()) toggled = !toggled;
        }

        bool KeyDown() { return Input.GetKeyDown(key.Value) || (key.Value == KeyCode.LeftShift && Input.GetKeyDown(KeyCode.RightShift)); }
        bool KeyHeld() { return Input.GetKey(key.Value) || (key.Value == KeyCode.LeftShift && Input.GetKey(KeyCode.RightShift)); }
        bool CanSprint()
        {
            return enabledSetting.Value && !open && Application.isFocused && Time.timeScale > 0f
                && camera != null && camera.isActiveAndEnabled && camera.readInput
                && !Traverse.Create(camera).Field("photoModeActive").GetValue<bool>()
                && (crouchSetting.Value || !FPSCamera.isCrouching)
                && (menu == null || !menu.gameObject.activeInHierarchy);
        }
        internal float Multiplier(CharacterMotor motor)
        {
            if (!CanSprint() || motor != Traverse.Create(camera).Field("FPSCharacterMotor").GetValue<CharacterMotor>() || !motor.canControl) return 1f;
            return (toggleSetting.Value ? toggled : KeyHeld()) ? Mathf.Clamp(percent.Value, 100f, 200f) / 100f : 1f;
        }

        void Open(PauseQuitMenu owner)
        {
            menu = owner;
            menuGroup = owner.GetComponent<CanvasGroup>();
            if (menuGroup != null) { oldInteractable = menuGroup.interactable; menuGroup.interactable = false; }
            toggled = false;
            open = true;
            window = new Rect(Mathf.Max(0, (Screen.width - 440) / 2f), Mathf.Max(0, (Screen.height - 340) / 2f), 440, 340);
        }
        void Close()
        {
            open = rebinding = toggled = false;
            if (menuGroup != null) menuGroup.interactable = oldInteractable;
            menuGroup = null;
            Config.Save();
        }
        void OnGUI()
        {
            if (!open) return;
            GUI.depth = -1000;
            window = GUI.Window(201818, window, DrawSettings, "SPRINT MOD CMS 2018");
        }
        void DrawSettings(int id)
        {
            GUILayout.Space(12);
            enabledSetting.Value = GUILayout.Toggle(enabledSetting.Value, "Enable sprint");
            GUILayout.Label("Sprint speed: " + Mathf.RoundToInt(percent.Value) + "%");
            percent.Value = Mathf.Round(GUILayout.HorizontalSlider(percent.Value, 100f, 200f));
            GUILayout.BeginHorizontal(); GUILayout.Label("100% - normal"); GUILayout.FlexibleSpace(); GUILayout.Label("200% - double"); GUILayout.EndHorizontal();
            GUILayout.Space(10);
            if (GUILayout.Button(rebinding ? "Press a keyboard key (Escape cancels)" : "Sprint key: " + key.Value + " - click to change")) rebinding = true;
            bool mode = GUILayout.Toggle(toggleSetting.Value, "Toggle sprint (unchecked = hold key)");
            if (mode != toggleSetting.Value) { toggleSetting.Value = mode; toggled = false; }
            crouchSetting.Value = GUILayout.Toggle(crouchSetting.Value, "Allow sprint while crouching");
            GUILayout.Space(10);
            if (GUILayout.Button("Reset defaults"))
            {
                enabledSetting.Value = true; percent.Value = 150f; key.Value = KeyCode.LeftShift;
                toggleSetting.Value = false; crouchSetting.Value = false; rebinding = toggled = false;
            }
            if (GUILayout.Button("Save and return to Escape menu")) Close();
            GUI.DragWindow(new Rect(0, 0, 440, 24));
        }

        internal void AddMenuButton(PauseQuitMenu owner)
        {
            menu = owner; toggled = false;
            Transform group = owner.transform.Find("Content/Buttons");
            if (group == null || group.Find("CMS2018SprintSettings") != null) return;
            int count = group.childCount;
            AccessTools.Method(typeof(PauseQuitMenu), "CreateButtonWithAction").Invoke(owner,
                new object[] { new Action(delegate { Open(owner); }), "Sprint Mod Settings" });
            if (group.childCount <= count) { Logger.LogWarning("Sprint menu button was not created."); return; }
            GameObject button = group.GetChild(group.childCount - 1).gameObject;
            button.name = "CMS2018SprintSettings";
            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null) label.text = "Sprint Mod Settings";
            List<GameObject> items = Traverse.Create(owner).Field("items").GetValue<List<GameObject>>();
            if (items != null && !items.Contains(button)) items.Add(button);
            LayoutRebuilder.ForceRebuildLayoutImmediate(group as RectTransform);
        }
        void OnDestroy()
        {
            Close();
            if (harmony != null) harmony.UnpatchSelf();
            Instance = null;
        }

        [HarmonyPatch(typeof(CharacterMotor), "MaxSpeedInDirection")]
        static class WalkingPatch
        {
            static void Postfix(CharacterMotor __instance, ref float __result)
            { if (Instance != null) __result *= Instance.Multiplier(__instance); }
        }
        [HarmonyPatch(typeof(PauseQuitMenu), "Prepare")]
        static class MenuPatch
        {
            [HarmonyPriority(Priority.Last)]
            static void Postfix(PauseQuitMenu __instance)
            { if (Instance != null) Instance.AddMenuButton(__instance); }
        }
        [HarmonyPatch(typeof(PauseQuitMenu), "Update")]
        static class MenuInputPatch
        {
            static bool Prefix() { return Instance == null || !Instance.open; }
        }
    }
}
