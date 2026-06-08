using UnityEngine;
using System.Collections.Generic;

namespace SoftFluidPuzzle.LevelEditor
{
    [System.Serializable]
    public class EditorLevelData
    {
        public string levelName;
        public string levelId;
        public string description;
        public List<PlacedObjectData> placedObjects = new List<PlacedObjectData>();
        public Vector3 playerSpawnPoint;
        public Vector3 goalPosition;
        public float timeLimit;
        public int targetStars;
    }

    [System.Serializable]
    public class PlacedObjectData
    {
        public string objectId;
        public string objectType;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public List<KeyValuePair[] properties;
    }

    [System.Serializable]
    public struct SerializableKeyValuePair
    {
        public string key;
        public string value;
    }

    [System.Serializable]
    public class PlaceableObject
    {
        public string id;
        public string displayName;
        public string category;
        public string description;
        public GameObject prefab;
        public Sprite icon;
        public bool isEditable = true;
        public EditorProperty[] editableProperties;
    }

    [System.Serializable]
    public class EditorProperty
    {
        public string propertyName;
        public string displayName;
        public PropertyType type;
        public float floatValue;
        public string stringValue;
        public bool boolValue;
        public Vector3 vector3Value;
    }

    public enum PropertyType
    {
        Float,
        Int,
        String,
        Bool,
        Vector3,
        Color,
        Enum
    }

    public enum EditorTool
    {
        Select,
        Move,
        Rotate,
        Scale,
        Place,
        Delete
    }

    public enum PlaceableCategory
    {
        Basic,
        Physics,
        Fluid,
        Mechanisms,
        Destructibles,
        Decorations,
        Triggers
    }
}
