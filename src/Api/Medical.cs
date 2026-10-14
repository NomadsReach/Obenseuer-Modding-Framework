using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public enum MedicalTreatment
    {
        PrescriptionShroomSubstitute,
        Fungicide,
        PrescriptionFungicide,
        GrowthsRemovalBasic,
        DangerousExperimentalTreatment,
        Antidepressants,
        PrescriptionAntidepressants,
        RubberDuck,
        PhysicalPainkillers,
        PhysicalPrescriptionPainkillers,
        AdhesiveBandages,
        Bandages,
        HealWounds,
        RadiationPainkillers,
        RadiationPrescriptionPainkillers,
        IodinePills
    }

    public sealed class MedicalTreatmentInfo
    {
        public MedicalTreatment Kind { get; private set; }
        public int Price { get; private set; }
        public MoneyCurrency Currency { get; private set; }
        public bool Available { get; private set; }

        internal MedicalTreatmentInfo(MedicalTreatment kind, int price, MoneyCurrency currency, bool available)
        {
            Kind = kind;
            Price = price;
            Currency = currency;
            Available = available;
        }
    }

    public sealed class MedicalTreatedArgs
    {
        public MedicalTreatment Kind { get; private set; }
        public int Price { get; private set; }
        public MoneyCurrency Currency { get; private set; }

        internal MedicalTreatedArgs(MedicalTreatment kind, int price, MoneyCurrency currency)
        {
            Kind = kind;
            Price = price;
            Currency = currency;
        }
    }

    public static class Medical
    {
        internal const string Owner = "obenseuer.moddingframework.medical";
        private static bool installed;

        public static event Action<MedicalTreatedArgs> Treated;

        public static bool IsAvailable
        {
            get
            {
                return installed && !CorePatcher.IsDisabled(Owner);
            }
        }

        public static List<MedicalTreatmentInfo> Treatments()
        {
            List<MedicalTreatmentInfo> result = new List<MedicalTreatmentInfo>();
            try
            {
                foreach (MedicalSlot slot in MedicalTreatments.Slots)
                {
                    MedicalOffer offer = MedicalTreatments.Find(slot.Kind, null);
                    if (offer == null) continue;
                    result.Add(new MedicalTreatmentInfo(slot.Kind, offer.Treatment.price, Cash.FromGame(offer.Treatment.currency), Offered(offer)));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Treatments", e);
                return new List<MedicalTreatmentInfo>();
            }
        }

        public static bool CanTreat(MedicalTreatment kind)
        {
            CheckKind(kind);
            try
            {
                return Ready(kind) != null;
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "CanTreat", e);
                return false;
            }
        }

        public static bool Treat(MedicalTreatment kind)
        {
            CheckKind(kind);
            if (SaveGate.Busy) return false;
            try
            {
                MedicalOffer offer = Ready(kind);
                return offer != null && offer.Selection.BuyTreatment(offer.Treatment);
            }
            catch (Exception e)
            {
                ApiLog.Once(Owner, "Treat", e);
                return false;
            }
        }

        internal static void Init()
        {
            installed = MedicalPatches.Install();
            CoreLog.For(Owner).Info(installed ? "medical events installed" : "medical events not installed");
        }

        internal static bool HasHandlers
        {
            get
            {
                return Treated != null;
            }
        }

        internal static void RaiseTreated(MedicalTreatedArgs args)
        {
            Events.Raise(Treated, "Medical.Treated", h => h(args));
        }

        private static MedicalOffer Ready(MedicalTreatment kind)
        {
            if (!MenuOpen()) return null;
            MedicalOffer offer = MedicalTreatments.Find(kind, DoctorController.instance.currentDoctor);
            if (offer == null || !Offered(offer)) return null;
            return Cash.Read(offer.Treatment.currency) >= offer.Treatment.price ? offer : null;
        }

        private static bool MenuOpen()
        {
            GameUIController ui = GameUIController.instance;
            DoctorController controller = DoctorController.instance;
            DoctorPanel panel = DoctorPanel.instance;
            return ui != null && ui.IsMenuVisible && ui.currentPanel == MenuPanels.Doctor
                && controller != null && controller.currentDoctor != null
                && panel != null && panel.isActiveAndEnabled
                && BlackCanvas.instance != null && Inventory.instance != null && PlayerStats.instance != null;
        }

        private static bool Offered(MedicalOffer offer)
        {
            return offer.Selection.GetAvailableTreatments().Contains(offer.Treatment);
        }

        private static void CheckKind(MedicalTreatment kind)
        {
            if (!Enum.IsDefined(typeof(MedicalTreatment), kind)) throw new ArgumentException("unknown treatment", "kind");
        }
    }
}
