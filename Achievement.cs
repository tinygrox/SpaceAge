﻿using KSP.Localization;

namespace SpaceAge
{
    public class Achievement
    {
        ProtoAchievement proto;
        string body = null;
        long time = -1;
        double value = 0;
        string hero;

        public ProtoAchievement Proto
        {
            get => proto;
            set
            {
                proto = value;
                if (value == null)
                    Valid = false;
            }
        }

        public string Body
        {
            get => Valid ? (Proto.IsBodySpecific ? body : null) : Localizer.Format("#SpaceAge_Invalid");
            set => body = value;
        }

        public long Time
        {
            get => (Valid && Proto.HasTime) ? time : -1;
            set => time = value;
        }

        public double Value
        {
            get => (Valid && Proto.HasValue) ? value : 0;
            set => this.value = value;
        }

        public string Hero
        {
            get => Valid ? hero : null;
            set => hero = value;
        }

        public string Ids { get; set; } = "";

        public string ShortDisplayValue
        {
            get
            {
                if (!Valid)
                    return Localizer.Format("#SpaceAge_Invalid");
                if (!Proto.HasValue)
                    return "";
                switch (Proto.ValueType)
                {
                    case ValueType.Funds:
                    case ValueType.Cost:
                        return Localizer.Format("#SpaceAge_Unit_Funds", Value.ToString("N0"));

                    case ValueType.Mass:
                        return Localizer.Format("#SpaceAge_Unit_Mass", Value.ToString("N2"));

                    case ValueType.PartsCount:
                        return Localizer.Format("#SpaceAge_Unit_Parts", Value.ToString("N0"));
                }
                return Value.ToString("N0");
            }
        }

        public string FullDisplayValue
        {
            get
            {
                if (!Valid)
                    return Localizer.Format("#SpaceAge_Invalid");
                string shortValue = ShortDisplayValue;
                if (shortValue.Length == 0)
                    return Hero ?? "";
                if (Hero == null)
                    return shortValue;
                return $"{shortValue} ({Hero})";
            }
        }

        public string Title => Valid ? Localizer.Format(Proto.Title, Core.GetBodyDisplayName(Body)) : Localizer.Format("#SpaceAge_Invalid");

        public double BodyMultiplier
        {
            get
            {
                CelestialBody celestialBody = Body != null ? FlightGlobals.GetBodyByName(Body) : null;
                return (celestialBody != null) ? celestialBody.scienceValues.RecoveryValue : 1;
            }
        }

        public double Score => Proto.Score * BodyMultiplier * (Proto.HasValue ? Value : 1);

        public string FullName => Valid ? GetFullName(Proto.Name, Body) : Localizer.Format("#SpaceAge_Invalid");

        public ConfigNode ConfigNode
        {
            get
            {
                ConfigNode node = new ConfigNode("ACHIEVEMENT");
                if (!Valid)
                    return node;
                node.AddValue("name", Proto.Name);
                if (Proto.IsBodySpecific)
                    node.AddValue("body", Body);
                if (Proto.HasTime)
                    node.AddValue("time", Time);
                if (Proto.HasValue)
                    node.AddValue("value", Value);
                if (Hero != null)
                    node.AddValue("hero", Hero);
                if (Proto.Unique)
                    node.AddValue("ids", Ids);
                return node;
            }

            set
            {
                Core.Log($"Loading '{value.GetValue("name")}' achievement...");
                Proto = SpaceAgeScenario.FindProtoAchievement(value.GetValue("name"));
                if (!Valid)
                    return;
                if (Proto.IsBodySpecific)
                    Body = value.GetString("body", FlightGlobals.GetHomeBodyName());
                if (Proto.HasTime)
                    Time = value.GetLongOrDouble("time", -1);
                if (Proto.HasValue)
                    Value = value.GetDouble("value");
                Hero = value.GetString("hero");
                if (Proto.Unique)
                    Ids = value.GetString("ids", "");
            }
        }

        public bool Valid { get; protected set; } = true;

        public Achievement(ConfigNode node) => ConfigNode = node;

        public Achievement(ProtoAchievement proto, CelestialBody body = null, Vessel vessel = null, double value = 0, string hero = null)
        {
            Proto = proto;
            if (!Valid)
                return;

            if (body != null)
                Body = body.name;

            switch (Proto.Home)
            {
                case HomeConditionType.Only:
                    Valid = FlightGlobals.GetHomeBody() == body;
                    break;

                case HomeConditionType.Exclude:
                    Valid = FlightGlobals.GetHomeBody() != body;
                    break;
            }

            if (Proto.HasTime)
            {
                Time = (long)Planetarium.GetUniversalTime();
                if (proto.ValueType != ValueType.TotalAssignedCrew)
                    Hero = hero ?? vessel?.vesselName;
            }

            if (hero != null)
                AddId(hero);
            else if (vessel != null)
                AddId(vessel.id.ToString());

            if (Proto.HasValue)
                switch (Proto.ValueType)
                {
                    case ValueType.Cost:
                        Value = vessel.GetCost();
                        break;

                    case ValueType.Mass:
                        Value = vessel.totalMass;
                        break;

                    case ValueType.PartsCount:
                        Value = vessel.parts.Count;
                        break;

                    case ValueType.CrewCount:
                        Value = vessel.GetCrewCount();
                        break;

                    case ValueType.TotalAssignedCrew:
                        Value = HighLogic.fetch.currentGame.CrewRoster.GetAssignedCrewCount();
                        break;

                    case ValueType.Funds:
                        Value = value;
                        break;

                    default:
                        Value = 1;
                        break;
                }

            if (Proto.CrewedOnly && (vessel == null || vessel.GetCrewCount() == 0))
                Valid = false;
        }

        public static string GetFullName(string name, string body = null) => name + (body != null ? $"@{body}" : "");

        public override string ToString() =>
            $"{(Time >= 0 ? KSPUtil.PrintDateCompact(Time, true) : "")}\t{Title}{(Value != 0 ? $" ({Value})" : "")}";

        public bool Register(Achievement old)
        {
            Core.Log($"Registering candidate achievement: {this}.");

            if (!Valid)
            {
                Core.Log("This candidate achievement is invalid. Terminating.");
                return false;
            }

            if (old != null)
                Core.Log($"Old achievement: {old}.");
            else Core.Log("Old achievement of this type does not exist.");

            if (old != null && (old.Proto != Proto || old.Body != Body))
                return false;

            bool doRegister = false;
            switch (Proto.Type)
            {
                case AchievementType.Total:
                    Core.Log($"Unique: {Proto.Unique}. Id: {Ids}. Old achievement's ids: {(old?.Ids ?? "N/A")}");
                    if (Value > 0 && (old == null || !Proto.Unique || !old.Ids.Contains(Ids)))
                    {
                        if (old != null)
                        {
                            Value += old.Value;
                            if (Proto.Unique)
                                Ids += old.Ids;
                        }
                        doRegister = true;
                    }
                    else doRegister = false;
                    break;

                case AchievementType.Max:
                    if (Value > 0 && (old == null || Value > old.Value))
                        doRegister = true;
                    break;

                case AchievementType.First:
                    if (old == null || Time < old.Time)
                        doRegister = true;
                    break;
            }

            if (doRegister)
                Core.Log("Registration successful: achievement completed!");
            else Core.Log("Registration failed: this doesn't qualify as an achievement.");

            return doRegister;
        }

        void AddId(string id) => Ids += $"[{id}]";
    }
}
