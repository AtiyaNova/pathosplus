﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
PathOS.cs 
PathOS (c) Nine Penguins (Samantha Stahlke) 2018
*/

namespace PathOS
{
    /* GAME ENTITIES */
    //This list is in flux based on the tagging system/typology review.
    //Right now the proof-of-concept just uses GOAL_OPTIONAL, HAZARD_ENEMY,
    //and POI.
    public enum EntityType
    {
        ET_NONE = 0,
        ET_GOAL_OPTIONAL = 100,
        ET_GOAL_MANDATORY = 110,
        ET_GOAL_COMPLETION = 120,
        ET_RESOURCE_ACHIEVEMENT = 150,
        ET_RESOURCE_PRESERVATION = 160,
        ET_HAZARD_ENEMY = 200,
        ET_HAZARD_ENVIRONMENT = 250,
        ET_POI = 300,
        ET_POI_NPC = 350
    };

    /* AGENT HEURISTICS */
    //Like the list of entities, this list is subject to change based on
    //the typology review (ongoing).
    public enum Heuristic
    {
        CURIOSITY = 0,
        ACHIEVEMENT = 10,
        COMPLETION = 15,
        AGGRESSION = 20,
        ADRENALINE = 25,
        CAUTION = 30,
        EFFICIENCY = 35
    };

    [System.Serializable]
    public class EntityWeight
    {
        public EntityType entype;
        public float weight;

        public EntityWeight(EntityType m_entype, float m_weight = 0.0f)
        {
            entype = m_entype;
            weight = m_weight;
        }
    }

    [System.Serializable]
    public class HeuristicWeightSet
    {
        public Heuristic heuristic;
        public List<EntityWeight> weights;

        public HeuristicWeightSet(Heuristic m_heuristic)
        {
            heuristic = m_heuristic;
            weights = new List<EntityWeight>();
        }
    }

    [System.Serializable]
    public class HeuristicScale
    {
        public Heuristic heuristic;
        public float scale;

        public HeuristicScale(Heuristic m_heuristic, float m_scale)
        {
            heuristic = m_heuristic;
            scale = m_scale;
        }
    }

    //Representation of entity objects defined in the PathOS manager.
    [System.Serializable]
    public class LevelEntity
    {
        public GameObject entityRef;
        public EntityType entityType;
        public Renderer rend;

        //Not used yet. Will be used to simulate compass/map availability.
        public bool omniscientDirection;
        public bool omniscientPosition;
    }

    /* PLAYER PERCEPTION */
    //How an entity is represented in the agent's world model.
    public class PerceivedEntity
    {
        public GameObject entityRef;
        //Used for identification/comparison.
        protected int instanceID;

        public EntityType entityType;
        public Vector3 pos;

        public PerceivedEntity(GameObject entityRef, EntityType entityType,
            Vector3 pos)
        {
            this.entityRef = entityRef;
            this.instanceID = entityRef.GetInstanceID();
            this.entityType = entityType;
            this.pos = pos;
        }

        //Equality operators are overriden to make array search/comparison easier.
        public static bool operator==(PerceivedEntity lhs, PerceivedEntity rhs)
        {
            if(object.ReferenceEquals(lhs, null))
                return object.ReferenceEquals(rhs, null);

            if (object.ReferenceEquals(rhs, null))
                return object.ReferenceEquals(lhs, null);

            return lhs.instanceID == rhs.instanceID;
        }

        public static bool operator!=(PerceivedEntity lhs, PerceivedEntity rhs)
        {
            if (object.ReferenceEquals(lhs, null))
                return !object.ReferenceEquals(rhs, null);

            if (object.ReferenceEquals(rhs, null))
                return object.ReferenceEquals(lhs, null);

            return lhs.instanceID != rhs.instanceID;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            PerceivedEntity objAsEntity = obj as PerceivedEntity;

            if (objAsEntity == null)
                return false;

            return this == objAsEntity;
        }

        public override int GetHashCode()
        {
            return instanceID;
        }

        public int GetInstanceID()
        {
            return instanceID;
        }
    }

    //How the memory of an object is represented in the agent's world model.
    public class EntityMemory : PerceivedEntity
    {
        public bool visited = false;
        public float impressionTime = 0.0f;

        public EntityMemory(GameObject entityRef, EntityType entityType,
            Vector3 pos) : base(entityRef, entityType, pos) { }

        public EntityMemory(PerceivedEntity data) : 
            base(data.entityRef, data.entityType, data.pos) { }
    }

    public class ExploreMemory
    {
        public static float posThreshold = 2.0f;
        public static float degThreshold = 5.0f;

        public float impressionTime = 0.0f;

        public Vector3 originPoint;
        public Vector3 direction;
        public float dEstimate;

        public ExploreMemory(Vector3 originPoint, Vector3 direction, float dEstimate)
        {
            this.originPoint = originPoint;
            this.direction = direction;
            this.dEstimate = dEstimate;
        }

        private bool EqualsSimilar(ExploreMemory rhs)
        {
            return (originPoint - rhs.originPoint).magnitude <= posThreshold
                && Vector3.Angle(direction, rhs.direction) <= degThreshold;
        }

        public static bool operator ==(ExploreMemory lhs, ExploreMemory rhs)
        {
            if (object.ReferenceEquals(lhs, null))
                return object.ReferenceEquals(rhs, null);

            if (object.ReferenceEquals(rhs, null))
                return object.ReferenceEquals(lhs, null);

            return lhs.EqualsSimilar(rhs);
        }

        public static bool operator !=(ExploreMemory lhs, ExploreMemory rhs)
        {
            if (object.ReferenceEquals(lhs, null))
                return !object.ReferenceEquals(rhs, null);

            if (object.ReferenceEquals(rhs, null))
                return object.ReferenceEquals(lhs, null);

            return !lhs.EqualsSimilar(rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            ExploreMemory objAsEntity = obj as ExploreMemory;

            if (objAsEntity == null)
                return false;

            return this == objAsEntity;
        }
    }

    public class PerceivedInfo
    {
        //What in-game objects are visible?
        public List<PerceivedEntity> entities;

        //Set of vectors representing directions the environment
        //will allow us to travel.
        //Not used yet.
        public List<Vector3> navDirections;

        public PerceivedInfo()
        {
            entities = new List<PerceivedEntity>();
            navDirections = new List<Vector3>();
        }
    }
}
