using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace WorkManager
{
    [UsedImplicitly]
    public class WorkManagerMod : Mod
    {
        public WorkManagerMod(ModContentPack content) : base(content)
        {
            GetSettings<Settings>();
            new Harmony("LordKuper.WorkManager").PatchAll(Assembly.GetExecutingAssembly());
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Settings.DoWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "WorkManager".Translate();
        }
    }
}