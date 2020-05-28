﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SpaceAge
{
    class VesselRecord
    {
        /// <summary>
        /// Vessel's Guid
        /// </summary>
        public string Id { get; protected set; }

        public Guid Guid
        {
            get => new Guid(Id);
            protected set => Id = value.ToString();
        }

        /// <summary>
        /// Vessel's user-readable name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Universal Time of launch
        /// </summary>
        public double LaunchTime { get; set; }

        /// <summary>
        /// Reference to the vessel (may be null if the vessel is destroyed)
        /// </summary>
        public Vessel Vessel
        {
            get => FlightGlobals.FindVessel(Guid);
            set
            {
                if (value != null)
                {
                    Guid = value.id;
                    Name = value.vesselName;
                    LaunchTime = value.launchTime;
                }
            }
        }

        /// <summary>
        /// Time since vessel's launch (may or may not be equal to MissionTime)
        /// </summary>
        public double Age => Planetarium.GetUniversalTime() - LaunchTime;

        public ConfigNode ConfigNode
        {
            get
            {
                if (Id == null)
                    return null;
                ConfigNode node = new ConfigNode("VESSEL");
                node.AddValue("id", Id);
                node.AddValue("name", Name);
                node.AddValue("launchTime", LaunchTime);
                return node;
            }
            set
            {
                Id = Core.GetString(value, "id");
                Name = Core.GetString(value, "name");
                LaunchTime = Core.GetDouble(value, "launchTime", Planetarium.GetUniversalTime());
                if (string.IsNullOrEmpty(Id))
                {
                    Core.Log("Incorrect vessel id in node: " + value, Core.LogLevel.Error);
                    return;
                }
            }
        }

        public VesselRecord(ConfigNode node) => ConfigNode = node;

        public VesselRecord(Vessel vessel) => Vessel = vessel;

        public VesselRecord(ProtoVessel protoVessel)
        {
            Guid = protoVessel.vesselID;
            Name = protoVessel.vesselName;
            LaunchTime = protoVessel.launchTime;
        }
    }
}
