using Verse;

namespace LordKuper.WorkManager;

public class PawnCountSettings : IExposable
{
    public bool Animals;
    public bool Colonists = true;
    public float Factor = 1f;
    public bool Foreigners;
    public bool Injured;
    public bool Prisoners;
    public bool Slaves;

    public void ExposeData()
    {
        Scribe_Values.Look(ref Colonists, nameof(Colonists), true);
        Scribe_Values.Look(ref Foreigners, nameof(Foreigners));
        Scribe_Values.Look(ref Prisoners, nameof(Prisoners));
        Scribe_Values.Look(ref Slaves, nameof(Slaves));
        Scribe_Values.Look(ref Animals, nameof(Animals));
        Scribe_Values.Look(ref Injured, nameof(Injured));
        Scribe_Values.Look(ref Factor, nameof(Factor), 1f);
    }
}