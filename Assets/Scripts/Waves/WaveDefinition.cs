using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave_", menuName = "Scriptable Objects/WaveDefinition")]
public class WaveDefinition : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public EnemyDefinition enemy;
        public int count = 5;
        [Range(0f, 1f)] public float weight = 1f; // influences random pick among entries
    }

    [Header("Wave Entries (count-based)")]
    public List<Entry> entries = new List<Entry>();

    [Header("Spawn Controls")]
    [Min(0.1f)] public float spawnIntervalSeconds = 1.0f;
    [Min(1)] public int concurrentAliveCap = 15;         // active enemies limit

    [Header("Scaling for this wave")]
    public float healthMultiplier = 1.0f;
    public float speedMultiplier = 1.0f;
    public float damageMultiplier = 1.0f; // if you add enemy attacks later

    [Header("Intermission after this wave")]
    [Min(0f)] public float intermissionSeconds = 15f;
}
