using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CrosshairType
{
    Default,
    CanAttack,
    Interact,
    canBuy,
    cannotBuy,
    PlaceBlock
}
public class Crosshairs : MonoBehaviour
{
    //[SerializeField] private List<Sprite> crosshairSprites;
    //// Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
        
    //}

    //// Update is called once per frame
    //void Update()
    //{
        
    //}

    //private CrosshairType GetCrosshairType()
    //{
    //    Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
    //    //Debug.DrawRay(ray.origin, ray.direction * 10);
    //    //Camera.main.ScreenPointToRay()
    //    Physics.Raycast(ray, out RaycastHit hit, 5f);

    //    if (hit.collider == null) return CrosshairType.Default;

    //    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy")) return CrosshairType.CanAttack;
        
    //    //if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Interactable")) return CrosshairType.Interact;

    //    if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Players") || hit.collider.gameObject.layer != LayerMask.NameToLayer("Enemies")) return CrosshairType.PlaceBlock;

    //    return default;
    //}
    //private void ChangeCrosshair(CrosshairType type)
    //{

    //}
}
