//
// A Visual Tracker Script to record what the user looks at
// TODO:
//      Multiple Raycasts  to create a visual field
//      Add an offset transform to recast tracking over an object
//     




using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    // save the dictionary to lists
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    // load dictionary from lists
    public void OnAfterDeserialize()
    {
        this.Clear();

        if (keys.Count != values.Count)
            throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));

        for (int i = 0; i < keys.Count; i++)
            this.Add(keys[i], values[i]);
    }
}

// https://answers.unity.com/questions/460727/how-to-serialize-dictionary-with-unity-serializati.html
[System.Serializable] public class SDictionary : SerializableDictionary<Vector3, Voxel> { }


// Nested Serialisable dicts

[System.Serializable] public class V3Dictionary : SerializableDictionary<string, float> { }
[System.Serializable] public class V2Dictionary : SerializableDictionary<Vector3, V3Dictionary> { }
[System.Serializable] public class V1Dictionary : SerializableDictionary<Vector3, V2Dictionary> { }




[System.Serializable]
public class Voxel
{
    public string name;

    public Vector3 position;

    public Vector3 gridInterval;

    public int uniqueViews;
    public float timeViewed;
    public float lastViewedAt;
    
    public GameObject boundryBox;
}




public class RayCast : MonoBehaviour {

    public GameObject marker;
    public GameObject viewportMarker;
    public GameObject viewport;
    public GameObject VisualisationOffset;

    public Vector3 gridInterval;

    // Used to record a viewport position at a ganularity
    public bool trackLocation = true;
    public Vector3 viewportGridInterval;


    public bool exportData = false;
    public float exportTime = 60.0f;
    public float timeLimit;

    SDictionary voxelDict;
    SDictionary viewportVoxelDict;

    V1Dictionary connectionDict;


    Voxel currentVoxel;
    Voxel currentViewportVoxel;

    Vector3 previousIndex;
    Vector3 previousViewportIndex;

    private float nextActionTime = 0.0f;
    public float period = 0.1f;

    // Use this for initialization
    void Start () {

        // A voxel dictionary to store all the voxel viewing information
        voxelDict = new SDictionary();
        viewportVoxelDict = new SDictionary();
        connectionDict = new V1Dictionary();

      

        // Start with a dummy voxel.
        currentVoxel = new Voxel();
        previousIndex = new Vector3(0, 0, 0);
        previousViewportIndex = new Vector3(0, 0, 0);

        // Start with a dummy viewport
        currentViewportVoxel = new Voxel();


        // Scale the marker transform once at the beginning of the run.
        marker.transform.localScale = gridInterval;

        // Scale the viewport marker transform once at the beginning of the run.
       viewportMarker.transform.localScale = viewportGridInterval;

        // Set up the export timer 
        timeLimit = exportTime;

    }



    void ExportData()
    {
        string json = JsonUtility.ToJson(voxelDict);
        System.IO.File.WriteAllText("voxelDict.json", json);

        json = JsonUtility.ToJson(viewportVoxelDict);
        System.IO.File.WriteAllText("viewportVoxelDict.json", json);

        json = JsonUtility.ToJson(connectionDict);
        System.IO.File.WriteAllText("connectionDict.json", json);
    }
	
	// Update is called once per frame
	void Update () {

        if (Time.time > nextActionTime)
        {
            nextActionTime += period;

            // Layer 9 is TrackedVisual layer
            int layerMask = 1 << 9;
            RaycastHit hit;
            Physics.Raycast(viewport.transform.position, viewport.transform.forward, out hit, 20.0F, layerMask);
            Debug.DrawRay(viewport.transform.position, viewport.transform.forward);

            bool indexChanged = false;
            bool viewportChanged = false;

            if (hit.collider)
            {
                // Calculate Voxel position in unrotated space
                Vector3 vectorFromCenter = hit.point - gameObject.transform.position ;
                Vector3 rotatedVectorFromCenter = Quaternion.Inverse(gameObject.transform.rotation) * vectorFromCenter;

                Vector3 index = new Vector3(Mathf.Round((1 / gridInterval[0]) * rotatedVectorFromCenter[0]) / (1 / gridInterval[0]), Mathf.Round((1 / gridInterval[1]) * rotatedVectorFromCenter[1]) / (1 / gridInterval[1]), Mathf.Round((1 / gridInterval[2]) * rotatedVectorFromCenter[2]) / (1 / gridInterval[2]));


                // Has the cube changed
                /*
                 * Update the time spent at the previous cube 
                 * 
                 * Create a new cube if required
                 */

                if (index != currentVoxel.position)
                {
                    indexChanged = true;

                    // Finish previous instance
                    float elapsedTime = Time.time - currentVoxel.lastViewedAt;
                    currentVoxel.timeViewed += elapsedTime;
                    currentVoxel.uniqueViews += 1;
                    currentVoxel.lastViewedAt = Time.time;

                    // Save results back to the previous Voxel
                    voxelDict[currentVoxel.position] = currentVoxel;

                    // Retrieve or Start a new instance
                    if (voxelDict.ContainsKey(index))
                    {
                        currentVoxel = voxelDict[index];
                    }
                    else
                    {
                        currentVoxel = new Voxel();
                        // set initial values
                        currentVoxel.name = hit.transform.gameObject.name;
                        currentVoxel.position = index;
                        currentVoxel.uniqueViews = 0;
                        currentVoxel.timeViewed = 0.0f;
                        currentVoxel.lastViewedAt = Time.time;
                        currentVoxel.gridInterval = gridInterval;
                        if (marker != null)
                        {
                            if (VisualisationOffset != null)
                            {
                                currentVoxel.boundryBox = Instantiate(marker, index+VisualisationOffset.transform.position, Quaternion.identity, VisualisationOffset.transform);
                                currentVoxel.boundryBox.layer = 3;
                                Debug.DrawLine(previousIndex+VisualisationOffset.transform.position, index + VisualisationOffset.transform.position, Color.red, 100.0f);

                            }
                            else
                            {
                                currentVoxel.boundryBox = Instantiate(marker, index, Quaternion.identity);
                                Debug.DrawLine(previousIndex, index, Color.red, 100.0f);

                            }
                        }
                    }
                }


                // Save a copy of the last voxel index for easy look ups
                previousIndex = index;

                // If we are tracking camera location, we can do the whole shebang again

                if (trackLocation)
                {

                    if (hit.collider)
                    {
                        // Calculate Voxel position in unrotated space
                        vectorFromCenter = viewport.transform.position - gameObject.transform.position  ;
                        rotatedVectorFromCenter = Quaternion.Inverse(gameObject.transform.rotation) * vectorFromCenter;
                        //Vector3 viewportIndex = new Vector3(rotatedVectorFromCenter[0] - (rotatedVectorFromCenter[0] % viewportGridInterval) + viewportGridInterval / 2, rotatedVectorFromCenter[1] - (rotatedVectorFromCenter[1] % viewportGridInterval) + viewportGridInterval / 2, rotatedVectorFromCenter[2] - (rotatedVectorFromCenter[2] % viewportGridInterval) + viewportGridInterval / 2);
                        //Vector3 viewportIndex = new Vector3(Mathf.Round((1 / viewportGridInterval) * rotatedVectorFromCenter[0]) / (1 / viewportGridInterval) + (viewportGridInterval / 2), Mathf.Round((1 / viewportGridInterval) * rotatedVectorFromCenter[1]) / (1 / viewportGridInterval) + (viewportGridInterval / 2), Mathf.Round((1 / viewportGridInterval) * rotatedVectorFromCenter[2]) / (1 / viewportGridInterval) + (viewportGridInterval / 2));
                        Vector3 viewportIndex = new Vector3(Mathf.Round((1 / viewportGridInterval[0]) * rotatedVectorFromCenter[0]) / (1 / viewportGridInterval[0]) , Mathf.Round((1 / viewportGridInterval[1]) * rotatedVectorFromCenter[1]) / (1 / viewportGridInterval[1]) , Mathf.Round((1 / viewportGridInterval[2]) * rotatedVectorFromCenter[2]) / (1 / viewportGridInterval[2]));


                        // Has the cube changed
                        /*
                         * Update the time spent at the previous cube 
                         * 
                         * Create a new cube if required
                         */

                        if (viewportIndex != currentViewportVoxel.position)
                        {
                            viewportChanged = true;

                            // Finish previous instance
                            float elapsedTime = Time.time - currentViewportVoxel.lastViewedAt;
                            currentViewportVoxel.timeViewed += elapsedTime;
                            currentViewportVoxel.uniqueViews += 1;
                            currentViewportVoxel.lastViewedAt = Time.time;
                            currentViewportVoxel.gridInterval = viewportGridInterval;

                            // Save results back to the previous Voxel
                            viewportVoxelDict[currentViewportVoxel.position] = currentViewportVoxel;

                            // Retrieve or Start a new instance
                            if (voxelDict.ContainsKey(viewportIndex))
                            {
                                currentViewportVoxel = viewportVoxelDict[viewportIndex];
                            }
                            else
                            {
                                currentViewportVoxel = new Voxel();
                                // set initial values
                                currentViewportVoxel.name = "Camera";
                                currentViewportVoxel.position = viewportIndex;
                                currentViewportVoxel.uniqueViews = 0;
                                currentViewportVoxel.timeViewed = 0.0f;
                                currentViewportVoxel.lastViewedAt = Time.time;


                                if (marker != null)
                                {
                                    if (VisualisationOffset != null)
                                    {
                                        currentViewportVoxel.boundryBox = Instantiate(viewportMarker, viewportIndex + VisualisationOffset.transform.position, Quaternion.identity, VisualisationOffset.transform);
                                        currentViewportVoxel.boundryBox.layer = 3;

                                        Debug.DrawLine(previousViewportIndex + VisualisationOffset.transform.position, viewportIndex+VisualisationOffset.transform.position, Color.green, 100.0f);
                                    } else { 
                                        currentViewportVoxel.boundryBox = Instantiate(viewportMarker, viewportIndex, Quaternion.identity);
                                        Debug.DrawLine(previousViewportIndex, viewportIndex, Color.green, 100.0f);
                                    }
                                }
                            }
                        }

                        previousViewportIndex = viewportIndex;

                        // A horribly messy solution to record the connections
                        /* 
                         * A three depth nested dictionary
                         * {Vector3  : { Vector3 : { string    : float }}}
                         * {Object : { Viewport  : { statistic : value }}}
                         * If either the viewportIndex or the Index has changed, we need to add a connection.
                         */

                        if (indexChanged || viewportChanged)
                        {
                            // Test if the index has already been index
                            if (!connectionDict.ContainsKey(index))
                            {
                                // Create the statistic dictionary
                                V3Dictionary statDict = new V3Dictionary();
                                statDict.Add("uniqueViews", 1.0f);
                                statDict.Add("distance", hit.distance);

                                V2Dictionary viewportDict = new V2Dictionary();
                                viewportDict.Add(viewportIndex, statDict);

                                connectionDict.Add(index, viewportDict);

                            }
                            else
                            {
                                if (!connectionDict[index].ContainsKey(viewportIndex))
                                {
                                    // Create the statistic dictionary
                                    V3Dictionary statDict = new V3Dictionary();
                                    statDict.Add("uniqueViews", 1.0f);
                                    statDict.Add("distance", hit.distance);

                                    V2Dictionary viewportDict = connectionDict[index];
                                    viewportDict.Add(viewportIndex, statDict);
                                    connectionDict[index] = viewportDict;
                                }
                                else
                                {
                                    V3Dictionary statDict = connectionDict[index][viewportIndex];
                                    statDict["uniqueViews"] += 1.0f;
                                    statDict["distance"] += hit.distance;
                                    connectionDict[index][viewportIndex] = statDict;
                                }
                            }

                           
                            if (VisualisationOffset != null)
                            {
                                Debug.DrawLine(index+VisualisationOffset.transform.position, viewportIndex+ VisualisationOffset.transform.position, Color.blue, 100.0f);
                            }
                            else
                            {
                                Debug.DrawLine(index, viewportIndex, Color.blue, 100.0f);
                            }
                        }

                    }
                }

            }

            // translate object for 10 seconds.
            if (exportData)
            {
                // translate object for 10 seconds.
                if (timeLimit < 0)
                {

                    Debug.Log("Time to Export");
                    ExportData();
                    timeLimit = exportTime;
                }
                else
                {
                    timeLimit -= Time.deltaTime;
                }
            }
        }
    }
}


