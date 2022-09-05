using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
private void OnTriggerEnter(Collider other) {
    Debug.Log("fdgfsdg");
    
}
private void OnCollisionEnter(Collision other) {
    Debug.Log("Collision");
    
}
    // Update is called once per frame
    void Update()
    {
        
    }
}
