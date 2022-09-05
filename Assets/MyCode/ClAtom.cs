using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using TMPro;

public class ClAtom : MonoBehaviour
{
    public GameObject Cl,Na,interActModel,FusionCard;
    public TextMeshPro chemicalTxt,InteractTxt;
    private GameObject Atom;
    private Vector3 NaAtomOrbitPos;
    public float InteractSpeed = 0.08f;
    public float InteractStopDistance = 5f;
    private float distance;
    private bool isInteracting;
    // Start is called before the first frame update
    void Start()
    {
        FusionCard.SetActive(false);
        interActModel.SetActive(false);


    }

    // Update is called once per frame
    private float distanceReverse;
    void Update()
    {

        if(Atom != null){
            distanceReverse = Vector3.Distance(Na.transform.position, this.transform.position);

            if(Atom != null && distanceReverse >= 0.2f){
                //Debug.Log(Vector3.Distance (AtomImage.transform.position, this.transform.position));
                Atom.transform.parent = Na.transform;
                
                //Atom.transform.position = Vector3.MoveTowards(Atom.transform.position,
                //                                           Na.transform.position + new Vector3(0,Na.transform.position.y + 0.05f,0) , InteractSpeed * Time.deltaTime);
                    Atom.transform.position = Vector3.MoveTowards(Atom.transform.position,
                                                           Na.transform.position + new Vector3(0,0.1f,0) , InteractSpeed * Time.deltaTime);
                    
                    if(fadeCardIn){
                        StartCoroutine(FadeOutAndInCoroutine(chemicalTxt,"17\nCl\nChlorine",0.5f));
                        InteractTxt.text = "Diffusion";
                        fadeCardIn = false;
                        Atom.transform.parent = Na.transform;
                        
                        ScaleObject(Atom.transform.Find("Atomic Orbit").gameObject,1f,1);
                        ScaleObject(this.transform.Find("Atom").gameObject.transform.Find("Atomic Orbit").gameObject,1f,1);
                        CancelInvoke();
                        ResetdisplayModel();                              

                        
                    }
                        
                if(Atom.transform.position == Na.transform.position){
                  Atom = null;
                }
            }else{
                if(Atom != null && distance >= InteractStopDistance / 200){
                    Atom.transform.parent = Cl.transform;
                    Atom.transform.position = Vector3.MoveTowards(Atom.transform.position,
                                                                    Cl.transform.position , InteractSpeed * Time.deltaTime);
                    distance = Vector3.Distance (Atom.transform.position, Cl.transform.position);
                            
                            if(!fadeCardIn){
                                StartCoroutine(FadeOutAndInCoroutine(chemicalTxt,"NaCl\nSodium Chloride",0.5f));
                                InteractTxt.text = "Fusion";
                                fadeCardIn = true;

                                ScaleObject(Atom.transform.Find("Atomic Orbit").gameObject,-1f,1);
                                ScaleObject(this.transform.Find("Atom").gameObject.transform.Find("Atomic Orbit").gameObject,-1f,1);
                                InvokeRepeating("displayModel",2.0f,2.0f);
                                
                            }   
                            
                        

                }
            }

           

        }
      


    }
    
private bool IsdisplayModel;
private void displayModel(){

if(!IsdisplayModel){
    IsdisplayModel = true;
    Atom.SetActive(false);
    this.transform.Find("Atom").gameObject.SetActive(false);
    interActModel.SetActive(true);
}else{
    IsdisplayModel = false;
    Atom.SetActive(true);
    this.transform.Find("Atom").gameObject.SetActive(true);
    interActModel.SetActive(false);
}

}

private void ResetdisplayModel(){
    IsdisplayModel = false;
    Atom.SetActive(true);
    this.transform.Find("Atom").gameObject.SetActive(true);
    interActModel.SetActive(false);
}

private bool fadeCardIn;


IEnumerator FadeInCoroutine(TextMeshPro text,string newText,float fadeTime)
{
    //waitTime = 0;
    Debug.Log("Hiiiiiiiiii");
    chemicalTxt.text = newText;
    while (waitTime < 0.8)
    {
    text.alpha = waitTime;
    yield return null;
    waitTime += Time.deltaTime / fadeTime;
        
    }

}

   private float waitTime = 0.8f;
 
IEnumerator FadeOutAndInCoroutine(TextMeshPro text,string newText,float fadeTime)
{
    //float waitTime = 0.8f;
    while (waitTime > 0)
    {
    text.alpha = waitTime;
    yield return null;
    waitTime -= Time.deltaTime / fadeTime;
    
    }
     yield return FadeInCoroutine(chemicalTxt,newText,1);
     yield return FadeInObject(FusionCard,0.5f);

                                
     
}


   IEnumerator FadeInObject(GameObject objectToFade,float fadeSpeed){
   
    Renderer rend = objectToFade.transform.GetComponent<Renderer>();
    Color matColor = rend.material.color;
    float alphaValue = rend.material.color.a;
    objectToFade.SetActive(true);

    if(fadeCardIn){
    while (rend.material.color.a < 0.8f)
    {
        alphaValue += Time.deltaTime / fadeSpeed;
        rend.material.color = new Color(matColor.r, matColor.g, matColor.b, alphaValue);
        yield return null;
    }
    rend.material.color = new Color(matColor.r, matColor.g, matColor.b, 0.8f);
    }else{if(!fadeCardIn){
        while (rend.material.color.a > 0f)
        {
        alphaValue -= Time.deltaTime / fadeSpeed;
        rend.material.color = new Color(matColor.r, matColor.g, matColor.b, alphaValue);
        yield return null;
        }
        rend.material.color = new Color(matColor.r, matColor.g, matColor.b, 0f);
        objectToFade.SetActive(false);
        }
    }
   }
    
    private Vector3 startScale,endScale;
    private void ScaleObject(GameObject myObj ,float ScaleValue ,float Scaletime){

        float duration = Scaletime;
        if(ScaleValue == 1){
            startScale = new Vector3(0.008f,0.008f,0.008f);
            endScale = new Vector3(0.05262f,0.05262f,0.05262f);
        }else{
            startScale = new Vector3(0.05262f,0.05262f,0.05262f);
            endScale = new Vector3(0.008f,0.008f,0.008f);
        }
        this.AnimateComponent<Transform> (duration, (t, time) =>
            {
                myObj.transform.localScale = Vector3.Lerp (startScale, endScale, time);
            });

    }

    

 
private Vector3 mainAtomPosition;

private void OnTriggerEnter(Collider other)
    {
        string _name = other.name;

        if (other.gameObject == Na)
        {
            
            Atom = Na.transform.Find("Atom").gameObject;
            mainAtomPosition = Atom.transform.position;
            Atom.transform.parent = Cl.transform;
            //Atom.transform.parent = null;
            distance = Vector3.Distance (Atom.transform.position, Cl.transform.position);

        }
       
    }

    private void OnTriggerExit(Collider other) 
    {
        string _name = other.name;
    
        if (other.gameObject == Na)
        {
            /*Cl.SetActive(true);
            NaCl.SetActive(false);*/
        }
       
    }


 


}



