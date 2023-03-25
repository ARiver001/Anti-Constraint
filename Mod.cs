using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLZ.Props;
using UnityEngine;
using HarmonyLib;
using SLZ.Rig;
using BoneLib;

[assembly: MelonInfo(typeof(AntiConstraint.Mod), "AntiConstraint", "1.0.0", "ARiver001")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace AntiConstraint
{
    public class Mod : MelonMod
    {
        public override void OnUpdate()
        {
            bool input_flag = false;
            for (byte i = 0; i < 2; i++)
            {
                BaseController controller = i == 0 ? Player.rightController : Player.leftController;
                input_flag = GetInput(controller);
            }

            if (!input_flag)
            {
                return;
            }

            MelonLogger.Msg("Removing constraints!");

            foreach (var tracker in Resources.FindObjectsOfTypeAll<ConstraintTracker>())
            {
                string path = GetGameObjectPath(tracker.transform);
                if (path.Contains("PhysicsRig"))
                {
                    tracker.DeleteConstraint();
                }
            }
        }

        private static float _lastTimeInput;
        private static bool _ragdollNextButton;
        public static MelonPreferences_Category MelonPrefCategory { get; private set; }
        public static RagdollBinding Binding { get; private set; }
        public static MelonPreferences_Entry<RagdollBinding> MelonPrefBinding { get; private set; }
        public enum RagdollBinding
        {
            DOUBLE_TAP_B,
            THUMBSTICK_PRESS
        }
        public override void OnInitializeMelon()
        {
            SetupMelonPrefs();
        }
        public static void SetupMelonPrefs()
        {
            MelonPrefCategory = MelonPreferences.CreateCategory("Anti Constraint");
            MelonPrefBinding = MelonPrefCategory.CreateEntry<RagdollBinding>("Binding", RagdollBinding.DOUBLE_TAP_B, null, null, false, false, null, null);
            Binding = MelonPrefBinding.Value;
        }
        public override void OnPreferencesLoaded()
        {
            Binding = MelonPrefBinding.Value;
        }
        private static bool GetInput(BaseController controller)
        {
            if (controller == null) return false;

            if (Binding == RagdollBinding.THUMBSTICK_PRESS || Binding != RagdollBinding.DOUBLE_TAP_B)
            {
                _lastTimeInput = 0f;
                _ragdollNextButton = false;
                return controller.GetThumbStickDown();
            }
            bool bbuttonDown = controller.GetBButtonDown();
            float realtimeSinceStartup = Time.realtimeSinceStartup;
            if (bbuttonDown && _ragdollNextButton)
            {
                if (realtimeSinceStartup - _lastTimeInput <= 0.32f)
                {
                    return true;
                }
                _ragdollNextButton = false;
                _lastTimeInput = 0f;
            }
            else if (bbuttonDown)
            {
                _lastTimeInput = realtimeSinceStartup;
                _ragdollNextButton = true;
            }
            else if (realtimeSinceStartup - _lastTimeInput > 0.32f)
            {
                _ragdollNextButton = false;
                _lastTimeInput = 0f;
            }
            return false;
        }
        internal static string GetGameObjectPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
/*
    [HarmonyPatch(typeof(ConstraintTracker))]
    public class ConstraintTrackerPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ConstraintTracker.joint), MethodType.Setter)]
        public void Postfix(ConstraintTracker __instance, ConfigurableJoint value)
        {
            MelonLogger.Msg("Deleted constraint!");
            if (!__instance || Mod.GetGameObjectPath(__instance.transform).Contains("[RigManager (Blank)]"))
            {
                return;
            }
            MelonLogger.Msg(Mod.GetGameObjectPath(value.transform));
            __instance.DeleteConstraint();
        }
    }*/
}
