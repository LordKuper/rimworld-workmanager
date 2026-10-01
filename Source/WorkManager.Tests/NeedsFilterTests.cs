using System.Collections.Generic;
using System.Linq;
using LordKuper.Common;
using RimWorld;
using Verse;

namespace LordKuper.WorkManager.Tests;

/// <summary>
///     Tests for the per-work-type needs filter on <see cref="WorkTypeAssignmentRule" />: validation, shipped and new
///     rule defaults, inheritance through <see cref="WorkTypeAssignmentRule.Combine" />, evaluation through the static
///     IsNeedBlocked seam, and the add-need menu contents.
/// </summary>
[TestFixture]
[NonParallelizable]
public class NeedsFilterTests : StateIsolationTestBase
{
    private FakeDefProvider _defProvider = null!;
    private IDefProvider _originalDefProvider = null!;

    /// <summary>
    ///     Builds a rule pair the way the game combines them: a freshly created work-type rule and the shipped default
    ///     rule as the fallback.
    /// </summary>
    private static (WorkTypeAssignmentRule Main, WorkTypeAssignmentRule Fallback) CreateCombinablePair()
    {
        var main = WorkTypeAssignmentRule.CreateRule("Mining");
        var fallback = WorkTypeAssignmentRule.DefaultRules.First(r => r.DefName == null);
        return (main, fallback);
    }

    /// <summary>
    ///     Creates a need def with a label derived from its def name and registers it with the fake def provider.
    /// </summary>
    private NeedDef RegisterNeed(string defName, int listPriority = 0)
    {
        var def = new NeedDef { defName = defName, label = $"{defName} (test)", listPriority = listPriority };
        _defProvider.Register(def);
        return def;
    }

    /// <summary>
    ///     Installs a fresh fake def provider so need defs resolve without the game's def database.
    /// </summary>
    [SetUp]
    public void InstallFakeDefProvider()
    {
        _originalDefProvider = DefProvider.Current;
        _defProvider = new FakeDefProvider();
        DefProvider.Current = _defProvider;
    }

    /// <summary>
    ///     Restores the def provider that was current before the test.
    /// </summary>
    [TearDown]
    public void RestoreDefProvider()
    {
        DefProvider.Current = _originalDefProvider;
    }

    // ==================== ValidateNeedsFilter ====================

    private static WorkTypeAssignmentRule RuleWithRange(float min, float max)
    {
        return new WorkTypeAssignmentRule("Mining")
        {
            NeedLimits = [new NeedLimit("Hunger") { Limit = new FloatRange(min, max) }]
        };
    }

    [Test]
    public void ValidateNeedsFilter_BoundsOutsideUnitRange_Clamped()
    {
        var rule = RuleWithRange(-0.5f, 1.5f);

        rule.ValidateNeedsFilter();

        rule.NeedLimits![0].Limit.Should().Be(new FloatRange(0f, 1f));
    }

    [Test]
    public void ValidateNeedsFilter_BothBoundsAboveOne_ClampedToOne()
    {
        var rule = RuleWithRange(1.2f, 1.5f);

        rule.ValidateNeedsFilter();

        rule.NeedLimits![0].Limit.Should().Be(new FloatRange(1f, 1f));
    }

    [Test]
    public void ValidateNeedsFilter_MinNaN_ResetToDefaultMin()
    {
        var rule = RuleWithRange(float.NaN, 0.8f);

        rule.ValidateNeedsFilter();

        rule.NeedLimits![0].Limit.Should().Be(new FloatRange(NeedLimit.DefaultMin, 0.8f));
    }

    [Test]
    public void ValidateNeedsFilter_MaxNaN_ResetToDefaultMax()
    {
        var rule = RuleWithRange(0.2f, float.NaN);

        rule.ValidateNeedsFilter();

        rule.NeedLimits![0].Limit.Should().Be(new FloatRange(0.2f, NeedLimit.DefaultMax));
    }

    [Test]
    public void ValidateNeedsFilter_MinAboveMax_Swapped()
    {
        var rule = RuleWithRange(0.8f, 0.2f);

        rule.ValidateNeedsFilter();

        rule.NeedLimits![0].Limit.Should().Be(new FloatRange(0.2f, 0.8f));
    }

    [TestCase(0f, 1f)]
    [TestCase(0.37f, 0.37f)]
    [TestCase(0.3f, 0.6f)]
    public void ValidateNeedsFilter_RangeInBounds_Unchanged(float min, float max)
    {
        var rule = RuleWithRange(min, max);

        rule.ValidateNeedsFilter();

        rule.NeedLimits![0].Limit.Should().Be(new FloatRange(min, max));
    }

    [Test]
    public void NewNeedLimit_DefaultsToHalfToFull()
    {
        new NeedLimit("Hunger").Limit.Should().Be(new FloatRange(0.5f, 1f));
    }

    [Test]
    public void ValidateNeedsFilter_DuplicateDefNames_KeepFirst()
    {
        var rule = new WorkTypeAssignmentRule("Mining")
        {
            NeedLimits =
            [
                new NeedLimit("Hunger") { Limit = new FloatRange(0.3f, 1f) },
                new NeedLimit("Hunger") { Limit = new FloatRange(0.7f, 1f) },
                new NeedLimit("Beauty") { Limit = new FloatRange(0.4f, 1f) }
            ]
        };

        rule.ValidateNeedsFilter();

        rule.NeedLimits.Should().HaveCount(2);
        rule.NeedLimits![0].DefName.Should().Be("Hunger");
        rule.NeedLimits[0].Limit.min.Should().Be(0.3f);
        rule.NeedLimits[1].DefName.Should().Be("Beauty");
    }

    [Test]
    public void ValidateNeedsFilter_DuplicateDefNames_CaseInsensitive()
    {
        var rule = new WorkTypeAssignmentRule("Mining")
        {
            NeedLimits =
            [
                new NeedLimit("Hunger") { Limit = new FloatRange(0.3f, 1f) },
                new NeedLimit("HUNGER") { Limit = new FloatRange(0.7f, 1f) },
                new NeedLimit("Beauty") { Limit = new FloatRange(0.4f, 1f) }
            ]
        };

        rule.ValidateNeedsFilter();

        rule.NeedLimits.Should().HaveCount(2);
        rule.NeedLimits![0].DefName.Should().Be("Hunger");
        rule.NeedLimits[1].DefName.Should().Be("Beauty");
    }

    [Test]
    public void ValidateNeedsFilter_NullAndEmptyEntries_Dropped()
    {
        var rule = new WorkTypeAssignmentRule("Mining")
        {
            NeedLimits =
            [
                new NeedLimit("Hunger") { Limit = new FloatRange(0.3f, 1f) },
                null!,
                new NeedLimit("") { Limit = new FloatRange(0.5f, 1f) },
                new NeedLimit("Beauty") { Limit = new FloatRange(0.4f, 1f) }
            ]
        };

        rule.ValidateNeedsFilter();

        rule.NeedLimits.Should().HaveCount(2);
        rule.NeedLimits![0].DefName.Should().Be("Hunger");
        rule.NeedLimits[1].DefName.Should().Be("Beauty");
    }

    [Test]
    public void ValidateNeedsFilter_UnresolvedDefName_Kept()
    {
        var rule = new WorkTypeAssignmentRule("Mining")
        {
            NeedLimits = [new NeedLimit("NeedFromRemovedMod") { Limit = new FloatRange(0.4f, 1f) }]
        };

        rule.ValidateNeedsFilter();

        rule.NeedLimits.Should().ContainSingle().Which.DefName.Should().Be("NeedFromRemovedMod");
    }

    [Test]
    public void ValidateNeedsFilter_DefaultRuleNullState_NormalisedToOff()
    {
        var rule = new WorkTypeAssignmentRule(null) { FilterNeeds = null };

        rule.ValidateNeedsFilter();

        rule.FilterNeeds.Should().Be(false);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ValidateNeedsFilter_DefaultRuleExplicitState_Kept(bool state)
    {
        var rule = new WorkTypeAssignmentRule(null) { FilterNeeds = state };

        rule.ValidateNeedsFilter();

        rule.FilterNeeds.Should().Be(state);
    }

    [Test]
    public void ValidateNeedsFilter_WorkTypeRuleNullState_RemainsInherit()
    {
        var rule = new WorkTypeAssignmentRule("Mining") { FilterNeeds = null };

        rule.ValidateNeedsFilter();

        rule.FilterNeeds.Should().BeNull();
    }

    [Test]
    public void ValidateNeedsFilter_NullNeedLimits_Initialised()
    {
        var rule = new WorkTypeAssignmentRule("Mining") { NeedLimits = null };

        rule.ValidateNeedsFilter();

        rule.NeedLimits.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void ValidateNeedsFilter_OlderSettingsWithBothFieldsNull_Normalised()
    {
        // Settings saved before the needs filter existed load with both fields null.
        var rule = new WorkTypeAssignmentRule(null) { FilterNeeds = null, NeedLimits = null };

        rule.ValidateNeedsFilter();

        rule.FilterNeeds.Should().Be(false);
        rule.NeedLimits.Should().NotBeNull().And.BeEmpty();
    }

    // ==================== Shipped and newly created rules ====================

    [Test]
    public void DefaultRules_DefaultRuleNoEntries()
    {
        var defaultRule = WorkTypeAssignmentRule.DefaultRules.First(r => r.DefName == null);

        defaultRule.NeedLimits.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void DefaultRules_DefaultRuleFilterNeedsOff()
    {
        var defaultRule = WorkTypeAssignmentRule.DefaultRules.First(r => r.DefName == null);

        defaultRule.FilterNeeds.Should().Be(false);
    }

    [Test]
    public void DefaultRules_NoRuleHasNeedsOnOrEntries()
    {
        foreach (var rule in WorkTypeAssignmentRule.DefaultRules)
        {
            rule.NeedLimits.Should().NotBeNull().And.BeEmpty();
            rule.FilterNeeds.Should().NotBe(true);
        }
    }

    [Test]
    public void CreateRule_WorkTypeRule_InheritAndEmpty()
    {
        var rule = WorkTypeAssignmentRule.CreateRule("Mining");

        rule.FilterNeeds.Should().BeNull();
        rule.NeedLimits.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void CreateRule_DefaultRule_OffAndEmpty()
    {
        var rule = WorkTypeAssignmentRule.CreateRule(null);

        rule.FilterNeeds.Should().Be(false);
        rule.NeedLimits.Should().NotBeNull().And.BeEmpty();
    }

    // ==================== Combine ====================

    [TestCase(true)]
    [TestCase(false)]
    public void Combine_MainInherit_TakesFallbackState(bool fallbackState)
    {
        var (main, fallback) = CreateCombinablePair();
        fallback.FilterNeeds = fallbackState;

        var combined = WorkTypeAssignmentRule.Combine(main, fallback);

        combined.FilterNeeds.Should().Be(fallbackState);
    }

    [Test]
    public void Combine_MainInherit_TakesFallbackEntries()
    {
        var (main, fallback) = CreateCombinablePair();
        var hunger = new NeedLimit("Hunger") { Limit = new FloatRange(0.5f, 1f) };
        var rest = new NeedLimit("Rest") { Limit = new FloatRange(0.2f, 1f) };
        main.NeedLimits = [new NeedLimit("Beauty") { Limit = new FloatRange(0.9f, 1f) }];
        fallback.FilterNeeds = true;
        fallback.NeedLimits = [hunger, rest];

        var combined = WorkTypeAssignmentRule.Combine(main, fallback);

        combined.NeedLimits.Should().Equal(hunger, rest);
    }

    [Test]
    public void Combine_MainInherit_CopiesFallbackList()
    {
        var (main, fallback) = CreateCombinablePair();
        fallback.FilterNeeds = true;
        fallback.NeedLimits = [new NeedLimit("Hunger")];

        var combined = WorkTypeAssignmentRule.Combine(main, fallback);
        combined.NeedLimits!.Add(new NeedLimit("Rest"));

        combined.NeedLimits.Should().NotBeSameAs(fallback.NeedLimits);
        fallback.NeedLimits.Should().ContainSingle();
    }

    [Test]
    public void Combine_MainInheritFallbackListNull_EmptyEntries()
    {
        var (main, fallback) = CreateCombinablePair();
        fallback.NeedLimits = null;

        var combined = WorkTypeAssignmentRule.Combine(main, fallback);

        combined.NeedLimits.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void Combine_MainOff_IgnoresFallback()
    {
        var (main, fallback) = CreateCombinablePair();
        main.FilterNeeds = false;
        fallback.FilterNeeds = true;
        fallback.NeedLimits = [new NeedLimit("Hunger") { Limit = new FloatRange(0.5f, 1f) }];

        var combined = WorkTypeAssignmentRule.Combine(main, fallback);

        combined.FilterNeeds.Should().Be(false);
        combined.NeedLimits.Should().BeEmpty();
    }

    [Test]
    public void Combine_MainOn_UsesMainEntriesOnly()
    {
        var (main, fallback) = CreateCombinablePair();
        var hunger = new NeedLimit("Hunger") { Limit = new FloatRange(0.3f, 1f) };
        main.FilterNeeds = true;
        main.NeedLimits = [hunger];
        fallback.FilterNeeds = true;
        fallback.NeedLimits = [new NeedLimit("Beauty") { Limit = new FloatRange(0.5f, 1f) }];

        var combined = WorkTypeAssignmentRule.Combine(main, fallback);

        combined.FilterNeeds.Should().Be(true);
        combined.NeedLimits.Should().Equal(hunger);
        combined.NeedLimits.Should().NotBeSameAs(main.NeedLimits);
    }

    [Test]
    public void Combine_MainOnWithNoEntries_DoesNotInheritFallbackEntries()
    {
        var (main, fallback) = CreateCombinablePair();
        main.FilterNeeds = true;
        fallback.FilterNeeds = true;
        fallback.NeedLimits = [new NeedLimit("Hunger") { Limit = new FloatRange(0.5f, 1f) }];

        var combined = WorkTypeAssignmentRule.Combine(main, fallback);

        combined.FilterNeeds.Should().Be(true);
        combined.NeedLimits.Should().BeEmpty();
    }

    // ==================== IsNeedBlocked ====================

    [Test]
    public void IsNeedBlocked_StateOff_NeverBlocks()
    {
        var hunger = RegisterNeed("Hunger");
        var limits = new List<NeedLimit> { new(hunger) { Limit = new FloatRange(0.5f, 1f) } };

        WorkTypeAssignmentRule.IsNeedBlocked(false, limits, _ => 0.1f).Should().BeFalse();
    }

    [Test]
    public void IsNeedBlocked_StateInherit_NeverBlocks()
    {
        var hunger = RegisterNeed("Hunger");
        var limits = new List<NeedLimit> { new(hunger) { Limit = new FloatRange(0.5f, 1f) } };

        WorkTypeAssignmentRule.IsNeedBlocked(null, limits, _ => 0.1f).Should().BeFalse();
    }

    [Test]
    public void IsNeedBlocked_StateOnNoEntries_NeverBlocks()
    {
        WorkTypeAssignmentRule.IsNeedBlocked(true, [], _ => 0f).Should().BeFalse();
    }

    [Test]
    public void IsNeedBlocked_StateOnNullList_NeverBlocks()
    {
        WorkTypeAssignmentRule.IsNeedBlocked(true, null, _ => 0f).Should().BeFalse();
    }

    [TestCase(0.49f, true)]
    [TestCase(0.5f, false)]
    [TestCase(0.51f, false)]
    [TestCase(0f, true)]
    public void IsNeedBlocked_DefaultRange_BlocksBelowMin(float level, bool expected)
    {
        var hunger = RegisterNeed("Hunger");
        var limits = new List<NeedLimit> { new(hunger) };

        WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => level).Should().Be(expected);
    }

    [TestCase(0.29f, true)]
    [TestCase(0.3f, false)]
    [TestCase(0.45f, false)]
    [TestCase(0.6f, false)]
    [TestCase(0.61f, true)]
    [TestCase(1f, true)]
    public void IsNeedBlocked_InteriorRange_BlocksOutsideInclusiveBounds(float level, bool expected)
    {
        var hunger = RegisterNeed("Hunger");
        var limits = new List<NeedLimit> { new(hunger) { Limit = new FloatRange(0.3f, 0.6f) } };

        WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => level).Should().Be(expected);
    }

    [Test]
    public void IsNeedBlocked_AboveMax_Blocks()
    {
        var hunger = RegisterNeed("Hunger");
        var limits = new List<NeedLimit> { new(hunger) { Limit = new FloatRange(0f, 0.8f) } };

        WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => 0.81f).Should().BeTrue();
    }

    [Test]
    public void IsNeedBlocked_ExactlyMinOrMax_Passes()
    {
        var hunger = RegisterNeed("Hunger");
        var limits = new List<NeedLimit> { new(hunger) { Limit = new FloatRange(0.2f, 0.8f) } };

        WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => 0.2f).Should().BeFalse();
        WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => 0.8f).Should().BeFalse();
    }

    [TestCase(0f)]
    [TestCase(0.5f)]
    [TestCase(1f)]
    public void IsNeedBlocked_FullRange_NeverBlocks(float level)
    {
        var hunger = RegisterNeed("Hunger");
        var limits = new List<NeedLimit> { new(hunger) { Limit = new FloatRange(0f, 1f) } };

        WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => level).Should().BeFalse();
    }

    [Test]
    public void IsNeedBlocked_PawnLacksListedNeed_Passes()
    {
        var hunger = RegisterNeed("Hunger");
        var rest = RegisterNeed("Rest");
        var levels = new Dictionary<NeedDef, float> { [rest] = 0.9f };
        var limits = new List<NeedLimit>
        {
            new(hunger) { Limit = new FloatRange(0.5f, 1f) },
            new(rest) { Limit = new FloatRange(0.5f, 1f) }
        };

        var blocked = WorkTypeAssignmentRule.IsNeedBlocked(true, limits,
            def => levels.TryGetValue(def, out var level) ? level : null);

        blocked.Should().BeFalse();
    }

    [Test]
    public void IsNeedBlocked_PawnWithoutNeeds_Passes()
    {
        var hunger = RegisterNeed("Hunger");
        var rest = RegisterNeed("Rest");
        var limits = new List<NeedLimit>
        {
            new(hunger) { Limit = new FloatRange(0.5f, 1f) },
            new(rest) { Limit = new FloatRange(1f, 1f) }
        };

        WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => null).Should().BeFalse();
    }

    [Test]
    public void IsNeedBlocked_UnresolvedEntry_Skipped()
    {
        var limits = new List<NeedLimit> { new("NeedFromRemovedMod") { Limit = new FloatRange(1f, 1f) } };
        var lookups = 0;

        var blocked = WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ =>
        {
            lookups++;
            return 0f;
        });

        blocked.Should().BeFalse();
        lookups.Should().Be(0);
    }

    [Test]
    public void IsNeedBlocked_UnresolvedEntryBeforeBlockingEntry_StillBlocks()
    {
        var rest = RegisterNeed("Rest");
        var limits = new List<NeedLimit>
        {
            new("NeedFromRemovedMod") { Limit = new FloatRange(1f, 1f) },
            new(rest) { Limit = new FloatRange(0.5f, 1f) }
        };

        WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => 0.2f).Should().BeTrue();
    }

    [Test]
    public void IsNeedBlocked_AnyOneEntryOutsideRange_Blocks()
    {
        var hunger = RegisterNeed("Hunger");
        var beauty = RegisterNeed("Beauty");
        var joy = RegisterNeed("Joy");
        var levels = new Dictionary<NeedDef, float> { [hunger] = 0.9f, [beauty] = 0.4f, [joy] = 0.9f };
        var limits = new List<NeedLimit>
        {
            new(hunger) { Limit = new FloatRange(0.5f, 1f) },
            new(beauty) { Limit = new FloatRange(0.6f, 1f) },
            new(joy) { Limit = new FloatRange(0.5f, 1f) }
        };

        var blocked = WorkTypeAssignmentRule.IsNeedBlocked(true, limits,
            def => levels.TryGetValue(def, out var level) ? level : null);

        blocked.Should().BeTrue();
    }

    [Test]
    public void IsNeedBlocked_AllEntriesInsideRange_Passes()
    {
        var hunger = RegisterNeed("Hunger");
        var beauty = RegisterNeed("Beauty");
        var levels = new Dictionary<NeedDef, float> { [hunger] = 0.5f, [beauty] = 0.8f };
        var limits = new List<NeedLimit>
        {
            new(hunger) { Limit = new FloatRange(0.5f, 1f) },
            new(beauty) { Limit = new FloatRange(0.6f, 1f) }
        };

        var blocked = WorkTypeAssignmentRule.IsNeedBlocked(true, limits,
            def => levels.TryGetValue(def, out var level) ? level : null);

        blocked.Should().BeFalse();
    }

    [Test]
    public void IsNeedBlocked_RepeatedCalls_ReflectCurrentLevels()
    {
        var rest = RegisterNeed("Rest");
        var limits = new List<NeedLimit> { new(rest) { Limit = new FloatRange(0.5f, 1f) } };
        var level = 0.2f;

        var firstVerdict = WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => level);
        level = 0.8f;
        var secondVerdict = WorkTypeAssignmentRule.IsNeedBlocked(true, limits, _ => level);

        firstVerdict.Should().BeTrue();
        secondVerdict.Should().BeFalse();
    }

    // ==================== HasAddableNeeds ====================

    [Test]
    public void HasAddableNeeds_EligibleNeedMissing_True()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };
        var beauty = new NeedDef { defName = "Beauty", label = "beauty" };

        Settings.HasAddableNeeds([hunger, beauty], [new NeedLimit("Hunger")]).Should().BeTrue();
    }

    [Test]
    public void HasAddableNeeds_AllEligibleAdded_False()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };
        var beauty = new NeedDef { defName = "Beauty", label = "beauty" };

        Settings.HasAddableNeeds([hunger, beauty], [new NeedLimit("Hunger"), new NeedLimit("Beauty")])
            .Should().BeFalse();
    }

    [Test]
    public void HasAddableNeeds_OnlyExcludedRemain_False()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };
        var mechEnergy = new NeedDef { defName = "MechEnergy", label = "energy", playerMechsOnly = true };
        var authority = new NeedDef { defName = "Authority", label = "authority" };

        Settings.HasAddableNeeds([hunger, mechEnergy, authority], [new NeedLimit("Hunger")])
            .Should().BeFalse();
    }

    [Test]
    public void HasAddableNeeds_CaseInsensitive()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };

        Settings.HasAddableNeeds([hunger], [new NeedLimit("HUNGER")]).Should().BeFalse();
    }

    [Test]
    public void HasAddableNeeds_NoNeeds_False()
    {
        Settings.HasAddableNeeds([], []).Should().BeFalse();
    }

    // ==================== GetAddableNeeds ====================

    [Test]
    public void GetAddableNeeds_NoNeeds_Empty()
    {
        Settings.GetAddableNeeds([], []).Should().BeEmpty();
    }

    [Test]
    public void GetAddableNeeds_ExcludesPlayerMechsOnly()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };
        var mechEnergy = new NeedDef { defName = "MechEnergy", label = "energy", playerMechsOnly = true };

        var result = Settings.GetAddableNeeds([hunger, mechEnergy], []);

        result.Should().Equal(hunger);
    }

    [Test]
    public void GetAddableNeeds_ExcludesAuthority()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };
        var authority = new NeedDef { defName = "Authority", label = "authority" };

        var result = Settings.GetAddableNeeds([hunger, authority], []);

        result.Should().Equal(hunger);
    }

    [Test]
    public void GetAddableNeeds_IncludesNeedsHiddenFromNeedList()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };
        var hidden = new NeedDef { defName = "HiddenNeed", label = "hidden", showOnNeedList = false };

        var result = Settings.GetAddableNeeds([hunger, hidden], []);

        result.Should().Contain(hidden);
    }

    [Test]
    public void GetAddableNeeds_ExcludesAlreadyAdded()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };
        var beauty = new NeedDef { defName = "Beauty", label = "beauty" };

        var result = Settings.GetAddableNeeds([hunger, beauty], [new NeedLimit("Hunger")]);

        result.Should().Equal(beauty);
    }

    [Test]
    public void GetAddableNeeds_ExcludesAlreadyAdded_CaseInsensitive()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };
        var beauty = new NeedDef { defName = "Beauty", label = "beauty" };

        var result = Settings.GetAddableNeeds([hunger, beauty], [new NeedLimit("HUNGER")]);

        result.Should().Equal(beauty);
    }

    [Test]
    public void GetAddableNeeds_AllAlreadyAdded_Empty()
    {
        var hunger = new NeedDef { defName = "Hunger", label = "hunger" };

        var result = Settings.GetAddableNeeds([hunger], [new NeedLimit("Hunger")]);

        result.Should().BeEmpty();
    }

    [Test]
    public void GetAddableNeeds_SortedByListPriorityDescendingThenLabel()
    {
        var high = new NeedDef { defName = "ZebraHigh", label = "zebra", listPriority = 100 };
        var appleLow = new NeedDef { defName = "AppleLow", label = "apple", listPriority = 50 };
        var bananaLow = new NeedDef { defName = "BananaLow", label = "banana", listPriority = 50 };
        var anotherLow = new NeedDef { defName = "AnotherLow", label = "another", listPriority = 50 };
        var lowest = new NeedDef { defName = "Aardvark", label = "aardvark", listPriority = 0 };

        var result = Settings.GetAddableNeeds([lowest, appleLow, high, bananaLow, anotherLow], []);

        result.Should().Equal(high, anotherLow, appleLow, bananaLow, lowest);
    }
}
