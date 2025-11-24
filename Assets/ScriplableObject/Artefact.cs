using System.ComponentModel;
using UnityEngine;
using UnityEngine.Serialization;

namespace ScriplableObject
{
    [CreateAssetMenu(fileName = "ArtefactScriptableObject", menuName = "ArtefactScriptableObject")]
    public class Artefact : ScriptableObject
    {
        [FormerlySerializedAs("artefactPrefab")] public GameObject prefab;
        public string name;
        [Description("Leave at -1 to ignore field")]
        [Range(-1f, 1f)]
        public float minSpawnHeight = -1f;
        [Range(-1f, 1f)]
        public float maxSpawnHeight = -1f;
        [Range(-1f, 1f)]
        public float minSlopeAngle = -1f;
        [Range(-1f, 1f)]
        public float maxSlopeAngle = -1f;
    }
}
