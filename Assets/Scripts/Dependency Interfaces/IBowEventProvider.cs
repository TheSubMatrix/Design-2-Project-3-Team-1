using UnityEngine;

public interface IBowEventProvider
{
    public delegate void OnBowCharge();
    public delegate void OnBowChargeCancel();
    public delegate void OnBowFire();
    public delegate void OnBowArrowSelectionChanged();
    public event OnBowFire OnBowFireEvent;
    public event OnBowCharge OnBowChargeEvent;
    public event OnBowChargeCancel OnBowChargeCancelEvent;
    public event OnBowArrowSelectionChanged OnBowArrowSelectionChangedEvent;
}
