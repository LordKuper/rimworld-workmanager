using System.Globalization;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace LordKuper.WorkManager;

public class PawnCapacityLimit : IExposable
{
    private const float ValueCap = 500f;
    private bool _isInitialized;
    private string _maxValueBuffer;
    private string _minValueBuffer;
    private PawnCapacityDef _pawnCapacityDef;
    private string _pawnCapacityDefName;
    public float? MaxValue;
    public float? MinValue;

    [UsedImplicitly]
    public PawnCapacityLimit() { }

    public PawnCapacityLimit(string pawnCapacityDefName)
    {
        _pawnCapacityDefName = pawnCapacityDefName;
    }

    public PawnCapacityLimit(string pawnCapacityDefName, float? minValue, float? maxValue)
    {
        _pawnCapacityDefName = pawnCapacityDefName;
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

    public PawnCapacityDef PawnCapacityDef
    {
        get
        {
            Initialize();
            return _pawnCapacityDef;
        }
    }

    public string PawnCapacityDefName => _pawnCapacityDefName;

    public void ExposeData()
    {
        Scribe_Values.Look(ref _pawnCapacityDefName, nameof(PawnCapacityDefName));
        Scribe_Values.Look(ref MinValue, nameof(MinValue));
        Scribe_Values.Look(ref MaxValue, nameof(MaxValue));
    }

    private void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        _pawnCapacityDef = DefDatabase<PawnCapacityDef>.GetNamedSilentFail(_pawnCapacityDefName);
    }
}