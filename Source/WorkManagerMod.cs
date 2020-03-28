using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using WorkManager.Patches;

namespace WorkManager
{
    [UsedImplicitly]
    public class WorkManagerMod : Mod
    {
        public WorkManagerMod(ModContentPack content) : base(content)
        {
            GetSettings<Settings>();
            var harmony = new Harmony("LordKuper.WorkManager");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (Harmony.HasAnyPatches("fluffy.worktab")) { ApplyWorkTabPatch(harmony); }
            if (Harmony.HasAnyPatches("rimworld.moreThanCapable"))
            {
                #if DEBUG
                Log.Message("Work Manager: MoreThanCapable detected.", true);
                #endif
                Settings.IsBadWorkMethod =
                    AccessTools.Method(AccessTools.TypeByName("MoreThanCapable.MoreThanCapableMod"), "IsBadWork");
            }
        }

        private void ApplyWorkTabPatch(Harmony harmony)
        {
            #if DEBUG
            Log.Message("Work Manager: Fluffy's WorkTab detected.", true);
            #endif
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.MainTabWindow_WorkTab"), "DoWindowContents"),
                postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(WorkTabPatch.DoWindowContentsPostfix)));
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "GetMinHeaderHeight"),
                postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(WorkTabPatch.GetMinHeaderHeightPostfix)));
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "DoHeader"),
                new HarmonyMethod(typeof(WorkTabPatch), nameof(WorkTabPatch.DoHeaderPrefix)));
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return Resources.Strings.WorkManager;
        }
    }
}