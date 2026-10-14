using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ObenseuerCore
{
    internal sealed class MedicalSlot
    {
        internal MedicalTreatment Kind { get; private set; }
        internal Type Selection { get; private set; }
        internal string FieldName { get; private set; }
        internal FieldInfo Field { get; private set; }

        internal MedicalSlot(MedicalTreatment kind, Type selection, string fieldName)
        {
            Kind = kind;
            Selection = selection;
            FieldName = fieldName;
            Field = AccessTools.Field(selection, fieldName);
        }
    }

    internal sealed class MedicalOffer
    {
        internal DoctorPanelSelection Selection { get; private set; }
        internal DoctorPanelSelection.Treatment Treatment { get; private set; }

        internal MedicalOffer(DoctorPanelSelection selection, DoctorPanelSelection.Treatment treatment)
        {
            Selection = selection;
            Treatment = treatment;
        }
    }

    internal static class MedicalTreatments
    {
        internal static readonly MedicalSlot[] Slots =
        {
            new MedicalSlot(MedicalTreatment.PrescriptionShroomSubstitute, typeof(TreatmentAddictions), "PrescriptionShroomSubtitute"),
            new MedicalSlot(MedicalTreatment.Fungicide, typeof(TreatmentFungalDisease), "Fungicide"),
            new MedicalSlot(MedicalTreatment.PrescriptionFungicide, typeof(TreatmentFungalDisease), "PrescriptionFungicide"),
            new MedicalSlot(MedicalTreatment.GrowthsRemovalBasic, typeof(TreatmentFungalDisease), "GrowthsRemovalBasic"),
            new MedicalSlot(MedicalTreatment.DangerousExperimentalTreatment, typeof(TreatmentFungalDisease), "DangerousExperimentalTreatment"),
            new MedicalSlot(MedicalTreatment.Antidepressants, typeof(TreatmentMentalHealth), "Antidepressants"),
            new MedicalSlot(MedicalTreatment.PrescriptionAntidepressants, typeof(TreatmentMentalHealth), "PrescriptionAntidepressants"),
            new MedicalSlot(MedicalTreatment.RubberDuck, typeof(TreatmentMentalHealth), "Rubberduck"),
            new MedicalSlot(MedicalTreatment.PhysicalPainkillers, typeof(TreatmentPhysicalHealth), "Painkillers"),
            new MedicalSlot(MedicalTreatment.PhysicalPrescriptionPainkillers, typeof(TreatmentPhysicalHealth), "PrescriptionPainkillers"),
            new MedicalSlot(MedicalTreatment.AdhesiveBandages, typeof(TreatmentPhysicalHealth), "AdhesiveBandages"),
            new MedicalSlot(MedicalTreatment.Bandages, typeof(TreatmentPhysicalHealth), "Bandages"),
            new MedicalSlot(MedicalTreatment.HealWounds, typeof(TreatmentPhysicalHealth), "HealWounds"),
            new MedicalSlot(MedicalTreatment.RadiationPainkillers, typeof(TreatmentRadiation), "Painkillers"),
            new MedicalSlot(MedicalTreatment.RadiationPrescriptionPainkillers, typeof(TreatmentRadiation), "PrescriptionPainkillers"),
            new MedicalSlot(MedicalTreatment.IodinePills, typeof(TreatmentRadiation), "IodinePills")
        };

        internal static MedicalOffer Find(MedicalTreatment kind, Doctor only)
        {
            MedicalSlot slot = SlotOf(kind);
            if (slot == null || slot.Field == null) return null;
            foreach (DoctorPanelSelection selection in Selections(only))
            {
                if (selection.GetType() != slot.Selection) continue;
                DoctorPanelSelection.Treatment treatment = slot.Field.GetValue(selection) as DoctorPanelSelection.Treatment;
                return treatment == null ? null : new MedicalOffer(selection, treatment);
            }
            return null;
        }

        internal static bool KindOf(DoctorPanelSelection selection, DoctorPanelSelection.Treatment treatment, out MedicalTreatment kind)
        {
            foreach (MedicalSlot slot in Slots)
            {
                if (slot.Field == null || slot.Selection != selection.GetType()) continue;
                if (!ReferenceEquals(slot.Field.GetValue(selection), treatment)) continue;
                kind = slot.Kind;
                return true;
            }
            kind = default(MedicalTreatment);
            return false;
        }

        private static MedicalSlot SlotOf(MedicalTreatment kind)
        {
            foreach (MedicalSlot slot in Slots)
            {
                if (slot.Kind == kind) return slot;
            }
            return null;
        }

        private static List<DoctorPanelSelection> Selections(Doctor only)
        {
            List<DoctorPanelSelection> all = new List<DoctorPanelSelection>();
            Doctor[] doctors = only == null ? SceneIds.Active<Doctor>().ToArray() : new[] { only };
            foreach (Doctor doctor in doctors)
            {
                if (doctor.treatments == null) continue;
                foreach (DoctorPanelSelection selection in doctor.treatments)
                {
                    if (selection != null && !all.Contains(selection)) all.Add(selection);
                }
            }
            return all;
        }
    }
}
