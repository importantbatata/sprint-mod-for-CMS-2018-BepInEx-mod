using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CMS2018Sprint
{
    [BepInPlugin("cms2018.sprint.standalone", "SPRINT MOD CMS 2018", "1.3.0")]
    public sealed class SprintMod : BaseUnityPlugin
    {
        internal static SprintMod Instance;
        ConfigEntry<bool> enabledSetting, toggleSetting, crouchSetting;
        ConfigEntry<float> percent;
        ConfigEntry<KeyCode> key;
        Harmony harmony;
        FPSCamera camera;
        PauseQuitMenu menu;
        Transform storedButtons;
        List<GameObject> originalItems;
        int originalSelection;
        string originalTitle;
        readonly List<GameObject> settingsButtons = new List<GameObject>();
        bool open, rebinding, toggled;
        float nextCameraSearch;
        int blockMenuInputFrame = -1;
        InputField speedInput;


        void Awake()
        {
            Instance = this;
            enabledSetting = Config.Bind("Sprint", "Enabled", true);
            percent = Config.Bind("Sprint", "SpeedPercent", 150f,
                new ConfigDescription("Speed relative to normal walking.", new AcceptableValueRange<float>(100f, 250f)));
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
                if (speedInput != null && speedInput.isFocused)
                {
                    if (Input.GetKeyDown(KeyCode.Escape)) { blockMenuInputFrame = Time.frameCount; speedInput.DeactivateInputField(); }
                    return;
                }
                if (Time.frameCount == blockMenuInputFrame) return;
                if (Input.GetKeyDown(KeyCode.Escape)) { blockMenuInputFrame = Time.frameCount; if (rebinding) { rebinding = false; RefreshLabels(); } else Close(); return; }
                if (rebinding && Time.frameCount != blockMenuInputFrame)
                {
                    foreach (KeyCode candidate in Enum.GetValues(typeof(KeyCode)))
                    {
                        if (candidate == KeyCode.None || candidate == KeyCode.Escape || (int)candidate >= (int)KeyCode.Mouse0) continue;
                        if (Input.GetKeyDown(candidate)) { key.Value = candidate; rebinding = false; blockMenuInputFrame = Time.frameCount; RefreshLabels(); break; }
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
            return isActiveAndEnabled && enabledSetting.Value && !open && Application.isFocused && Time.timeScale > 0f
                && camera != null && camera.isActiveAndEnabled && camera.readInput
                && !Traverse.Create(camera).Field("photoModeActive").GetValue<bool>()
                && (crouchSetting.Value || !FPSCamera.isCrouching)
                && (menu == null || !menu.gameObject.activeInHierarchy);
        }
        internal float Multiplier(CharacterMotor motor)
        {
            if (!CanSprint() || motor != Traverse.Create(camera).Field("FPSCharacterMotor").GetValue<CharacterMotor>() || !motor.canControl) return 1f;
            return (toggleSetting.Value ? toggled : KeyHeld()) ? Mathf.Clamp(percent.Value, 100f, 250f) / 100f : 1f;
        }

        static float FootstepMultiplier(FPSSound sound)
        {
            if (Instance == null || sound == null) return 1f;
            CharacterMotor motor = sound.GetComponent<CharacterMotor>();
            return motor == null ? 1f : Instance.Multiplier(motor);
        }
        static float FootstepVelocity(float squaredVelocity, FPSSound sound)
        {
            float multiplier = FootstepMultiplier(sound);
            // Preserve vanilla volume, walk/crouch selection and teleport cutoff at
            // the equivalent walking velocity, even when sprint exceeds that cutoff.
            return squaredVelocity / (multiplier * multiplier);
        }
        static float FootstepPitch(float vanillaPitch, FPSSound sound)
        {
            // Scale the freshly computed vanilla pitch, never an already-scaled value.
            return vanillaPitch * FootstepMultiplier(sound);
        }

        Text MenuTitle()
        {
            Transform title = menu == null ? null : menu.transform.Find("Content/TextBG/Text");
            return title == null ? null : title.GetComponent<Text>();
        }
        void Open(PauseQuitMenu owner)
        {
            if (open || owner == null) return;
            menu = owner;
            Transform group = owner.transform.Find("Content/Buttons");
            if (group == null) return;
            originalItems = Traverse.Create(owner).Field("items").GetValue<List<GameObject>>();
            originalSelection = Traverse.Create(owner).Field("selectedObjectIndex").GetValue<int>();
            Text title = MenuTitle();
            originalTitle = title == null ? null : title.text;
            GameObject storage = new GameObject("SprintStoredPauseButtons");
            storedButtons = storage.transform;
            storedButtons.SetParent(owner.transform, false);
            storage.SetActive(false);
            open = true;
            blockMenuInputFrame = Time.frameCount;
            toggled = rebinding = false;
            try
            {
                // Keep the original objects intact, outside the button group. The game's
                // navigation uses sibling indices, which must match the settings list.
                while (group.childCount > 0) group.GetChild(0).SetParent(storedButtons, false);
                Traverse.Create(owner).Field("items").SetValue(new List<GameObject>());
                Traverse.Create(owner).Field("selectedObjectIndex").SetValue(0);
                Traverse.Create(owner).Field("selectedObject").SetValue((GameObject)null);
                if (title != null) title.text = "Sprint Settings";
                AddSetting(delegate { enabledSetting.Value = !enabledSetting.Value; RefreshLabels(); });
                AddSetting(delegate { if (speedInput != null) { speedInput.Select(); speedInput.ActivateInputField(); blockMenuInputFrame = Time.frameCount; } });
                AddSetting(delegate { rebinding = true; blockMenuInputFrame = Time.frameCount; RefreshLabels(); });
                AddSetting(delegate { toggleSetting.Value = !toggleSetting.Value; toggled = false; RefreshLabels(); });
                AddSetting(delegate { crouchSetting.Value = !crouchSetting.Value; RefreshLabels(); });
                AddSetting(delegate {
                    enabledSetting.Value = true; percent.Value = 150f; key.Value = KeyCode.LeftShift;
                    toggleSetting.Value = false; crouchSetting.Value = false; toggled = rebinding = false;
                    RefreshLabels();
                });
                AddSetting(delegate { Close(); });
                CreateSpeedInput();
                RefreshLabels();
                LayoutRebuilder.ForceRebuildLayoutImmediate(group as RectTransform);
            }
            catch (Exception ex)
            {
                Close();
                Logger.LogError("Could not open native sprint settings: " + ex);
            }
        }
        void AddSetting(Action action)
        {
            Transform group = menu.transform.Find("Content/Buttons");
            int count = group.childCount;
            AccessTools.Method(typeof(PauseQuitMenu), "CreateButtonWithAction").Invoke(menu,
                new object[] { action, "Sprint Settings" });
            if (group.childCount != count + 1) throw new InvalidOperationException("Native button was not created.");
            GameObject button = group.GetChild(count).gameObject;
            button.name = "SprintSetting" + count;
            settingsButtons.Add(button);
            Traverse.Create(menu).Field("items").GetValue<List<GameObject>>().Add(button);
        }
        void RefreshLabels()
        {
            if (!open || settingsButtons.Count != 7) return;
            string[] labels = {
                "Sprint: " + (enabledSetting.Value ? "On" : "Off"),
                "Sprint Speed (100-250%)",
                rebinding ? "Press a key (Escape cancels)" : "Sprint Key: " + key.Value,
                "Sprint Mode: " + (toggleSetting.Value ? "Toggle" : "Hold"),
                "Sprint While Crouching: " + (crouchSetting.Value ? "On" : "Off"),
                "Reset Defaults", "Back"
            };
            for (int i = 0; i < labels.Length; i++)
            {
                Text label = settingsButtons[i].GetComponentInChildren<Text>(true);
                if (label != null) label.text = labels[i];
            }
            if (speedInput != null && !speedInput.isFocused) speedInput.text = Mathf.RoundToInt(percent.Value).ToString();
        }
        void CreateSpeedInput()
        {
            GameObject row = settingsButtons[1];
            Text label = row.GetComponentInChildren<Text>(true);
            if (label == null) throw new InvalidOperationException("Native speed label missing.");
            // Reuse the native text styling and button background for an inline field.
            GameObject field = new GameObject("SpeedPercentInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            RectTransform rect = field.GetComponent<RectTransform>();
            rect.SetParent(row.transform, false);
            rect.anchorMin = new Vector2(0.76f, 0.12f); rect.anchorMax = new Vector2(0.96f, 0.88f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = field.GetComponent<Image>();
            Image native = row.GetComponent<Image>();
            if (native != null) { image.sprite = native.sprite; image.type = native.type; }
            image.color = new Color(0f, 0f, 0f, 0.65f);
            Text value = Instantiate(label, rect);
            value.name = "Value";
            value.rectTransform.anchorMin = Vector2.zero; value.rectTransform.anchorMax = Vector2.one;
            value.rectTransform.offsetMin = new Vector2(5f, 0f); value.rectTransform.offsetMax = new Vector2(-5f, 0f);
            value.alignment = TextAnchor.MiddleCenter;
            value.raycastTarget = false;
            label.rectTransform.anchorMax = new Vector2(0.74f, label.rectTransform.anchorMax.y);
            label.alignment = TextAnchor.MiddleLeft;
            speedInput = field.GetComponent<InputField>();
            speedInput.targetGraphic = image;
            speedInput.textComponent = value;
            speedInput.contentType = InputField.ContentType.IntegerNumber;
            speedInput.characterLimit = 3;
            speedInput.lineType = InputField.LineType.SingleLine;
            speedInput.text = Mathf.RoundToInt(percent.Value).ToString();
            speedInput.onEndEdit.AddListener(delegate(string text) {
                int number;
                if (int.TryParse(text, out number)) percent.Value = Mathf.Clamp(number, 100, 250);
                blockMenuInputFrame = Time.frameCount;
                // Reset invalid/empty input and display the clamped value immediately.
                if (speedInput != null) speedInput.text = Mathf.RoundToInt(percent.Value).ToString();
                try { Config.Save(); } catch (Exception ex) { Logger.LogError("Could not save sprint speed: " + ex); }
            });
        }
        void Close()
        {
            if (!open) return;
            if (speedInput != null && speedInput.isFocused) speedInput.DeactivateInputField();
            speedInput = null;
            blockMenuInputFrame = Time.frameCount;
            open = rebinding = toggled = false;
            if (menu != null)
            {
                Transform group = menu.transform.Find("Content/Buttons");
                if (group != null)
                {
                    foreach (GameObject button in settingsButtons)
                        if (button != null) { button.SetActive(false); button.transform.SetParent(null, false); Destroy(button); }
                    if (storedButtons != null)
                        while (storedButtons.childCount > 0) storedButtons.GetChild(0).SetParent(group, false);
                    if (originalItems != null)
                    {
                        Traverse.Create(menu).Field("items").SetValue(originalItems);
                        Traverse.Create(menu).Field("selectedObjectIndex").SetValue(Mathf.Clamp(originalSelection, 0, Mathf.Max(0, originalItems.Count - 1)));
                        Traverse.Create(menu).Field("selectedObject").SetValue((GameObject)null);
                    }
                    Text title = MenuTitle();
                    if (title != null && originalTitle != null) title.text = originalTitle;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(group as RectTransform);
                }
            }
            if (storedButtons != null) Destroy(storedButtons.gameObject);
            storedButtons = null;
            originalItems = null;
            settingsButtons.Clear();
            try { Config.Save(); } catch (Exception ex) { Logger.LogError("Could not save sprint settings: " + ex); }
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
            string continueText = Localization.Instance.Get("GUI_Pause_ContinueButton");
            for (int i = 0; i < group.childCount; i++)
            {
                Transform child = group.GetChild(i);
                Text text = child.GetComponentInChildren<Text>(true);
                if (child != button.transform && text != null && text.text == continueText)
                {
                    child.SetAsLastSibling();
                    button.transform.SetSiblingIndex(child.GetSiblingIndex());
                    child.SetAsLastSibling();
                    break;
                }
            }
            // Native keyboard navigation expects list indices to equal sibling indices.
            if (items != null)
            {
                items.Clear();
                for (int i = 0; i < group.childCount; i++)
                {
                    GameObject child = group.GetChild(i).gameObject;
                    if (child.GetComponent<ButtonsWindowMenuItem>() != null) items.Add(child);
                }
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(group as RectTransform);
        }
        void OnDisable() { if (open) Close(); toggled = false; }
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
        [HarmonyPatch(typeof(FPSSound), "Update")]
        static class FootstepPatch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var velocity = AccessTools.PropertyGetter(typeof(Vector3), "sqrMagnitude");
                var pitch = AccessTools.PropertySetter(typeof(AudioSource), "pitch");
                int velocityMatches = 0, pitchMatches = 0;
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(pitch))
                    {
                        pitchMatches++;
                        // These instructions become the branch destination too, if
                        // another patch branches directly to the original setter.
                        CodeInstruction loadSound = new CodeInstruction(OpCodes.Ldarg_0);
                        loadSound.labels.AddRange(instruction.labels);
                        instruction.labels.Clear();
                        loadSound.blocks.AddRange(instruction.blocks);
                        instruction.blocks.Clear();
                        yield return loadSound;
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SprintMod), "FootstepPitch"));
                    }
                    yield return instruction;
                    if (instruction.Calls(velocity))
                    {
                        velocityMatches++;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SprintMod), "FootstepVelocity"));
                    }
                }
                if (velocityMatches != 1 || pitchMatches != 2)
                    throw new InvalidOperationException("Unexpected FPSSound.Update layout; sprint audio patch was not applied.");
            }
        }
        [HarmonyPatch(typeof(PauseQuitMenu), "Prepare")]
        static class MenuPatch
        {
            static void Prefix(PauseQuitMenu __instance) { if (Instance != null && Instance.open && Instance.menu == __instance) Instance.Close(); }
            [HarmonyPriority(Priority.Last)]
            static void Postfix(PauseQuitMenu __instance)
            { if (Instance == null) return; try { Instance.AddMenuButton(__instance); } catch (Exception ex) { Instance.Logger.LogError("Sprint settings button failed; leaving the pause menu available: " + ex); } }
        }
        [HarmonyPatch(typeof(PauseQuitMenu), "Hide")]
        static class MenuHidePatch
        {
            static void Prefix(PauseQuitMenu __instance)
            { if (Instance != null && Instance.open && Instance.menu == __instance) Instance.Close(); }
        }
        [HarmonyPatch(typeof(PauseQuitMenu), "HideWithoutChangeMode")]
        static class MenuHideWithoutChangePatch
        {
            static void Prefix(PauseQuitMenu __instance)
            { if (Instance != null && Instance.open && Instance.menu == __instance) Instance.Close(); }
        }
        [HarmonyPatch(typeof(PauseQuitMenu), "Update")]
        static class MenuInputPatch
        {
            static bool Prefix(PauseQuitMenu __instance)
            {
                if (Instance == null || !Instance.isActiveAndEnabled || __instance != Instance.menu) return true;
                if (Time.frameCount == Instance.blockMenuInputFrame) return false;
                // Let the vanilla menu navigate and activate our native buttons.
                // Escape is handled by SprintMod.Update, regardless of update order.
                return !Instance.open || (!Instance.rebinding && (Instance.speedInput == null || !Instance.speedInput.isFocused) && !Input.GetKeyDown(KeyCode.Escape));
            }
        }
    }
}
