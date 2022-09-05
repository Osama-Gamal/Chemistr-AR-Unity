using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateInPosition : MonoBehaviour
{
    public float rotationSpeed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0,rotationSpeed,0) * Time.deltaTime);
    }
}
