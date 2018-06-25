//
// A Visual Tracker Script to record what the user looks at
// TODO:
//      Children objects and Parents
//      Output the file into a digestable format


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayCast : MonoBehaviour {

    public GameObject marker;
    public float gridInterval;


    Hashtable hashtable;
    Vector3 previous;

    // Use this for initialization
    void Start () {
         hashtable = new Hashtable();
         previous = new Vector3(0, 0, 0);

        marker.transform.localScale = new Vector3(gridInterval, gridInterval, gridInterval);

    }
	
	// Update is called once per frame
	void Update () {

        // Layer 9 is TrackedVisual layer
        int layerMask = 1 << 9;
        RaycastHit hit;

        Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, 20.0F,layerMask);

        Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.forward);

        if (hit.collider)
        {
             
            Vector3 vectorFromCenter = gameObject.transform.position - hit.point;
            Vector3 rotatedVectorFromCenter = Quaternion.Inverse(gameObject.transform.rotation) * vectorFromCenter;

            Vector3 index = new Vector3(rotatedVectorFromCenter[0] - (rotatedVectorFromCenter[0] % gridInterval) + gridInterval/2, rotatedVectorFromCenter[1] - (rotatedVectorFromCenter[1] % gridInterval) + gridInterval / 2, rotatedVectorFromCenter[2] - (rotatedVectorFromCenter[2] % gridInterval) + gridInterval / 2);

            if (hashtable.ContainsKey(index))
            {
                hashtable[index] = (int)hashtable[index] +1;
            } else
            {
                hashtable.Add(index, 1);

                if (marker != null) { Instantiate(marker, index, Quaternion.identity); }

                Debug.DrawLine(previous, index, Color.red, 100.0f);
                previous = index;

            }
                
            
        }
    }
    
}



