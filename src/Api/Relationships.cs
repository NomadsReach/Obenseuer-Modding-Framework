using System;
using System.Collections.Generic;

namespace ObenseuerCore
{
    public sealed class RelationshipValue
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public float Disposition { get; private set; }
        public float Target { get; private set; }
        public bool Enabled { get; private set; }

        internal RelationshipValue(string id, string name, float disposition, float target, bool enabled)
        {
            Id = id;
            Name = name;
            Disposition = disposition;
            Target = target;
            Enabled = enabled;
        }
    }

    public sealed class RelationshipChange
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public float Previous { get; private set; }
        public float Current { get; private set; }

        internal RelationshipChange(string id, string name, float previous, float current)
        {
            Id = id;
            Name = name;
            Previous = previous;
            Current = current;
        }
    }

    public static class Relationships
    {
        public static event Action<RelationshipChange> RelationshipChanged;

        public static bool IsAvailable
        {
            get
            {
                return Npcs.IsAvailable;
            }
        }

        public static List<RelationshipValue> All()
        {
            try
            {
                List<RelationshipValue> result = new List<RelationshipValue>();
                if (RelationshipController.instance == null) return result;
                foreach (RelationshipController.RelationshipInfo info in RelationshipController.instance.relationships)
                {
                    if (info.relationship == null) continue;
                    result.Add(new RelationshipValue(info.relationship.Id, info.relationship.relationshipName, info.disposition, info.dispositionTarget, info.active));
                }
                return result;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Relationships.All", e);
                return new List<RelationshipValue>();
            }
        }

        public static float GetDisposition(string id)
        {
            ValidateId(id);
            try
            {
                if (RelationshipController.instance == null) return 0f;
                foreach (RelationshipController.RelationshipInfo info in RelationshipController.instance.relationships)
                {
                    if (info.relationship != null && info.relationship.Id == id) return info.disposition;
                }
                return 0f;
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Relationships.GetDisposition", e);
                return 0f;
            }
        }

        public static bool Has(string id)
        {
            ValidateId(id);
            try
            {
                return RelationshipController.instance != null && RelationshipController.instance.RelationshipIdActive(id);
            }
            catch (Exception e)
            {
                ApiLog.Once(Npcs.Owner, "Relationships.Has", e);
                return false;
            }
        }

        internal static void RaiseChanged(Relationship relationship, float previous, float current)
        {
            RelationshipChange change = new RelationshipChange(relationship.Id, relationship.relationshipName, previous, current);
            Events.Raise(RelationshipChanged, "Relationships.RelationshipChanged", h => h(change));
        }

        private static void ValidateId(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("id must be a non-empty string", "id");
        }
    }
}
