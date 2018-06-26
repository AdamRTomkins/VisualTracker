using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViveControllerInput : MonoBehaviour
{
    Camera cam;
    public LayerMask fullMask;
    public LayerMask partialMask;

    // 1
    private SteamVR_TrackedObject trackedObj;
    // 2
    private SteamVR_Controller.Device Controller
    {
        get { return SteamVR_Controller.Input((int)trackedObj.index); }
    }

    void Awake()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
        cam = Camera.main;
        fullMask = cam.cullingMask;
        partialMask = ~LayerMask.NameToLayer("TrackedPoints");
        cam.cullingMask = partialMask;
    }

    // Update is called once per frame
    void Update () {

        // 2
        if (Controller.GetHairTriggerDown())
        {
            Debug.Log(gameObject.name + " Trigger Press");

            cam.cullingMask = fullMask;
        }

        // 3
        if (Controller.GetHairTriggerUp())
        {
            Debug.Log(gameObject.name + " Trigger Release");
            cam.cullingMask = partialMask;
        }

    }
}
