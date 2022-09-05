using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NaAtom : MonoBehaviour
{
    public GameObject AtomImage;
    private float distanceReverse;
    // Start is called before the first frame update
    void Start()
    {
        //Na.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if(AtomImage != null && distanceReverse >= 0.2f){
            Debug.Log("sssssssssssssssss");

        }
    }

private void OnTriggerEnter(Collider other)
    {
        string _name = other.name;

        if (_name == "ImageTarget Cl")
        {
            AtomImage = other.gameObject;
            distanceReverse = Vector3.Distance(AtomImage.transform.position, this.transform.position);
        }
       
    }

    private void OnTriggerExit(Collider other) 
    {
        string _name = other.name;

        if (_name == "ImageTarget Cl")
        {

        }
       
    }



}
