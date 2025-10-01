using UnityEngine;

public class BuildPhaseManager : MonoBehaviour
{
    [SerializeField] private bool isBuildPhase = true;
    public bool IsBuildPhase => isBuildPhase;

    public void SetBuildPhase(bool enabled) => isBuildPhase = enabled;
}
