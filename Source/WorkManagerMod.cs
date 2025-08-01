using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager
{
    [UsedImplicitly]
    public class WorkManagerMod : Mod
    {
        internal const string ModId = "LordKuper.WorkManager";

        public WorkManagerMod(ModContentPack content) : base(content)
        {
            Logger.LogMessage($"Initializing (v.{Assembly.GetExecutingAssembly().GetName().Version})...");
            GetSettings<Settings.Settings>();
            var harmony = new Harmony(ModId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Compatibility.Compatibility.Initialize(harmony);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.Settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return Resources.Strings.ModTitle;
        }
    }
}