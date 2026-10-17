---
title: Common tasks
parent: Medical
grand_parent: Player life and skills
nav_order: 1
---

# Common tasks

Check whether the player can buy bandages right now, and buy them if so. This only works while the doctor's screen is open.

```csharp
if (Medical.CanTreat(MedicalTreatment.Bandages))
    Log.Info("Bought: " + Medical.Treat(MedicalTreatment.Bandages));
```

Show which treatments the doctor offers right now.

```csharp
foreach (MedicalTreatmentInfo treatment in Medical.Treatments())
    if (treatment.Available)
        Log.Info(treatment.Kind + " costs " + treatment.Price + " " + treatment.Currency);
```
