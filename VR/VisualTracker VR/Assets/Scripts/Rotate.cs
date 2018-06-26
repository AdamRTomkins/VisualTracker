using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour {

    public Vector3 RotateAmount;  // degrees per second to rotate in each axis. Set in inspector.

    // Update is called once per frame
    void Update()
    {

        // RotateAmount = RotateAmount + noise;
        transform.Rotate(RotateAmount * Time.deltaTime);

    }
}
