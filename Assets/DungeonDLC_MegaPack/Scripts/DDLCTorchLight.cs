using UnityEngine;
namespace DungeonDLC.MegaPack
{
    [RequireComponent(typeof(Light))]
    public sealed class DDLCTorchLight : MonoBehaviour
    {
        [Min(0)] public float baseIntensity = 2.2f;
        [Range(0, .5f)] public float variation = .13f;
        Light lamp;
        float offset;
        void OnEnable() { lamp = GetComponent<Light>(); offset = transform.position.sqrMagnitude * .17f; }
        void Update() { lamp.intensity = baseIntensity * (1 + (Mathf.PerlinNoise(offset, Time.time * 5) - .5f) * variation * 2); }
    }
}
