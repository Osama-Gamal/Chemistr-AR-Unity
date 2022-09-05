using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{
    public float rotationSpeed;
    public GameObject targetObject;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.RotateAround(targetObject.transform.position,new Vector3(0,1,0),rotationSpeed * Time.deltaTime);
        
    }
}
