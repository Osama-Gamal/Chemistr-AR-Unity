using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class FusionButton : MonoBehaviour
{
    
    public VirtualButtonBehaviour vb;
    public GameObject test;
    // Start is called before the first frame update
    void Start()
    {
        vb.RegisterOnButtonPressed(OnButtonPressed);
        vb.RegisterOnButtonReleased(OnButtonReleased);
        test.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public  void OnButtonPressed(VirtualButtonBehaviour vb){
        test.SetActive(true);
        Debug.Log("Worrrrrrk");
    }
    public  void OnButtonReleased(VirtualButtonBehaviour vb){
        test.SetActive(false);
        Debug.Log("NoooootWorrrrrrk");

    }


}
