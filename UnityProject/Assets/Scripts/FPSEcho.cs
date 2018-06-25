using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSEcho : MonoBehaviour {

    public float echoTime;
    public float timeLimit;

    // Use this for initialization
    void Start () {

        timeLimit = echoTime;
	}
	
	// Update is called once per frame
	void Update () {
        // translate object for 10 seconds.
        
        // translate object for 10 seconds.
        if (timeLimit < 0)
        {
    
            timeLimit = echoTime;
            Debug.Log("FPS : " + 1 / Time.deltaTime);
        }
        else
        {
            timeLimit -= Time.deltaTime;
        }
        
    }
}
