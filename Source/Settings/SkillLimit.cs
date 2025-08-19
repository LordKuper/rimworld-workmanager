using System.Globalization;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager;

public class SkillLimit : IExposable
{
    private const float ValueCap = 20f;
    private bool _isInitialized;
    private string _maxValueBuffer;
    private string _minValueBuffer;
    private SkillDef _skillDef;
    private string _skillDefName;
    public float? MaxValue;
    public float? MinValue;

    [UsedImplicitly]
    public SkillLimit() { }

    public SkillLimit(string skillDefName)
    {
        _skillDefName = skillDefName;
    }

    public SkillLimit(string skillDefName, float? minValue, float? maxValue)
    {
        _skillDefName = skillDefName;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public string MaxValueBuffer
    {
        get
        {
            if (MaxValue.HasValue && string.IsNullOrEmpty(_maxValueBuffer)) _maxValueBuffer = $"{MaxValue:N2}";
            return _maxValueBuffer;
        }
        set
        {
            if (value == _maxValueBuffer) return;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var maxValue))
            {
                MaxValue = Mathf.Clamp(maxValue, -1 * ValueCap, ValueCap);
                _maxValueBuffer = $"{MaxValue:N2}";
            }
            else
            {
                MaxValue = null;
                _maxValueBuffer = value;
            }
        }
    }

    public string MinValueBuffer
    {
        get
        {
            if (MinValue.HasValue && string.IsNullOrEmpty(_minValueBuffer)) _minValueBuffer = $"{MinValue:N2}";
            return _minValueBuffer;
        }
        set
        {
            if (value == _minValueBuffer) return;
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var minValue))
            {
                MinValue = Mathf.Clamp(minValue, -1 * ValueCap, ValueCap);
                _minValueBuffer = $"{MinValue:N2}";
            }
            else
            {
                MinValue = null;
                _minValueBuffer = value;
            }
        }
    }

    public SkillDef SkillDef
    {
        get
        {
            Initialize();
            return _skillDef;
        }
    }

    public string SkillDefName => _skillDefName;

    public void ExposeData()
    {
        Scribe_Values.Look(ref _skillDefName, nameof(SkillDefName));
        Scribe_Values.Look(ref MinValue, nameof(MinValue));
        Scribe_Values.Look(ref MaxValue, nameof(MaxValue));
    }

    private void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        _skillDef = DefDatabase<SkillDef>.GetNamedSilentFail(_skillDefName);
    }
}