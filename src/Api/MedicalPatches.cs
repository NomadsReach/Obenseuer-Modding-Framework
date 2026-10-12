using System;
using System.Reflection;

namespace ObenseuerCore
{
    internal static class MedicalPatches
    {
        internal static bool Install()
        {
            try
            {
                foreach (MedicalSlot slot in MedicalTreatments.Slots)
                {
                    if (slot.Field == null) throw new MissingFieldException(slot.Selection.Name, slot.FieldName);
                }
                bool installed = true;
                foreach (Type selection in new[] { typeof(TreatmentAddictions), typeof(TreatmentFungalDisease), typeof(TreatmentMentalHealth), typeof(TreatmentPhysicalHealth), typeof(TreatmentRadiation) })
                {
                    installed &= CorePatcher.PatchCore(selection, "BuyTreatment", new[] { typeof(DoctorPanelSelection.Treatment) }, null, CorePatcher.Handler(typeof(MedicalPatches), "BuyPostfix"), null, true, Medical.Owner);
                }
                return installed;
            }
            catch (Exception e)
            {
                ApiLog.Once(Medical.Owner, "install", e);
                CorePatcher.DisableOwner(Medical.Owner);
                return false;
            }
        }

        private static void BuyPostfix(DoctorPanelSelection __instance, DoctorPanelSelection.Treatment treatment, bool __result)
        {
            try
            {
                MedicalTreatment kind;
                if (__result && Medical.HasHandlers && MedicalTreatments.KindOf(__instance, treatment, out kind))
                {
                    Medical.RaiseTreated(new MedicalTreatedArgs(kind, treatment.price, Cash.FromGame(treatment.currency)));
                }
            }
            catch (Exception e)
            {
                ApiLog.Once(Medical.Owner, "BuyPostfix", e);
            }
        }
    }
}
