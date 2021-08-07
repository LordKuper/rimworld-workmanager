using System;
using System.Collections.Generic;
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
            if (Prefs.DevMode)
            {
                Log.Message($"Work Manager: Initializing (v.{Assembly.GetExecutingAssembly().GetName().Version})...");
            }
            GetSettings<Settings>();
            var harmony = new Harmony("LordKuper.WorkManager");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            if (LoadedModManager.RunningModsListForReading.Any(m =>
                "fluffy.worktab".Equals(m.PackageId, StringComparison.OrdinalIgnoreCase)))
            {
                ApplyWorkTabPatch(harmony);
                Settings.GetPriorityMethod = AccessTools.Method(AccessTools.TypeByName("WorkTab.Pawn_Extensions"),
                    "GetPriority", new[] { typeof(Pawn), typeof(WorkTypeDef), typeof(int) });
                Settings.SetPriorityMethod = AccessTools.Method(AccessTools.TypeByName("WorkTab.Pawn_Extensions"),
                    "SetPriority", new[] { typeof(Pawn), typeof(WorkTypeDef), typeof(int), typeof(List<int>) });
                Settings.MaxPriority = Traverse.Create(AccessTools.TypeByName("WorkTab.Settings"))
                    .Field<int>("maxPriority").Value;
                if (Prefs.DevMode) { Log.Message($"Work Manager: Max priority is {Settings.MaxPriority}."); }
            }
            if (LoadedModManager.RunningModsListForReading.Any(m =>
                "notfood.MoreThanCapable".Equals(m.PackageId, StringComparison.OrdinalIgnoreCase)))
            {
                if (Prefs.DevMode) { Log.Message("Work Manager: MoreThanCapable detected."); }
                Settings.IsBadWorkMethod =
                    AccessTools.Method(AccessTools.TypeByName("MoreThanCapable.MoreThanCapableMod"), "IsBadWork");
            }
        }

        private void ApplyWorkTabPatch(Harmony harmony)
        {
            if (Prefs.DevMode) { Log.Message("Work Manager: Fluffy's WorkTab detected."); }
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.MainTabWindow_WorkTab"), "DoWindowContents"),
                postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(WorkTabPatch.DoWindowContentsPostfix)));
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "GetMinHeaderHeight"),
                postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(WorkTabPatch.GetMinHeaderHeightPostfix)));
            harmony.Patch(AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "DoHeader"),
                new HarmonyMethod(typeof(WorkTabPatch), nameof(WorkTabPatch.DoHeaderPrefix)));
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"),
                    "HandleInteractionsDetailed"),
                new HarmonyMethod(typeof(WorkTabPatch), nameof(WorkTabPatch.HandleInteractionsDetailedPrefix)));
            harmony.Patch(
                AccessTools.Method(AccessTools.TypeByName("WorkTab.PawnColumnWorker_WorkType"), "DrawWorkTypeBoxFor"),
                postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(WorkTabPatch.DrawWorkTypeBoxForPostfix)));
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