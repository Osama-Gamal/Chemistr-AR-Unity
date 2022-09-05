using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using TMPro;

public class InteractAtom : MonoBehaviour
{
   
    private GameObject ThisAtom,FusionCard,interActModel;
    public TextMeshPro chemicalTxt,InteractTxt;
    public float InteractSpeed = 0.08f;
    public float InteractStopDistance = 5f;
    public string MainText,NewText;
    public List<GameObject> Atoms = new List<GameObject>();
    //private GameObject Atom,Atom2;

    private float distance,distance2;
    private bool isInteracting;
    // Start is called before the first frame update
    void Start()
    {
        interActModel = this.transform.Find("My3DModel").gameObject;
        FusionCard = this.transform.Find("FusionQuad").gameObject;
        ThisAtom = this.transform.Find("Atom").gameObject;
        FusionCard.SetActive(false);
        interActModel.SetActive(false);

    }

    // Update is called once per frame
    private float distanceReverse;
    private bool outRange;
    void Update()
    {


            foreach(GameObject myAtom in Atoms){
                distanceReverse = Vector3.Distance(myAtom.transform.position, this.transform.position);
                if(distanceReverse >= 0.2f){
                    outRange = true;
                    break;
                }else{
                    outRange = false;
                }
            }

            if(outRange){
          
                foreach(GameObject myAtom in Atoms){
                    myAtom.transform.Find("Atom").transform.parent = myAtom.transform;
                    myAtom.transform.Find("Atom").transform.position = Vector3.MoveTowards(myAtom.transform.Find("Atom").transform.position,
                                                            myAtom.transform.position + new Vector3(0,0.1f,0) , InteractSpeed * Time.deltaTime);
                
                }
            
                    if(fadeCardIn){
                        StartCoroutine(FadeOutAndInCoroutine(chemicalTxt,MainText,0.5f));
                        chemicalTxt.text.Replace("\\n","\n");
                        InteractTxt.text = "Diffusion";
                        fadeCardIn = false;

                        foreach(GameObject myAtom in Atoms){
                           myAtom.transform.Find("Atom").transform.parent = myAtom.transform;
                           ScaleObject(myAtom.transform.Find("Atom").transform.Find("Atomic Orbit").gameObject,1f,1);
                        }

                        ScaleObject(this.transform.Find("Atom").gameObject.transform.Find("Atomic Orbit").gameObject,1f,1);
                        CancelInvoke();
                        ResetdisplayModel();                              
                    }

            }else{
                foreach(GameObject myAtom in Atoms){
                    distance = Vector3.Distance (myAtom.transform.Find("Atom").transform.position, ThisAtom.transform.position);
                    myAtom.transform.Find("Atom").transform.parent = myAtom.transform;
                    
                    if(distance >= InteractStopDistance / 200)
                    myAtom.transform.Find("Atom").transform.position = Vector3.MoveTowards(myAtom.transform.Find("Atom").transform.position,
                                                                    ThisAtom.transform.position , InteractSpeed * Time.deltaTime);
                    }

                    if(!fadeCardIn){
                        StartCoroutine(FadeOutAndInCoroutine(chemicalTxt,NewText,0.5f));
                        InteractTxt.text = "Fusion";
                        fadeCardIn = true;
                        foreach(GameObject myAtom in Atoms){
                           ScaleObject(myAtom.transform.Find("Atom").transform.Find("Atomic Orbit").gameObject,-1f,1);
                        }
                        ScaleObject(this.transform.Find("Atom").gameObject.transform.Find("Atomic Orbit").gameObject,-1f,1);
                        InvokeRepeating("displayModel",2.0f,2.0f);
                                
                        }   
                 
            }

           



    }
    
private bool IsdisplayModel;
private void displayModel(){

if(!IsdisplayModel){
    IsdisplayModel = true;
    foreach(GameObject myAtom in Atoms){
        myAtom.transform.Find("Atom").gameObject.SetActive(false);
    }
    this.transform.Find("Atom").gameObject.SetActive(false);
    interActModel.SetActive(true);
}else{
    IsdisplayModel = false;
    foreach(GameObject myAtom in Atoms){
        myAtom.transform.Find("Atom").gameObject.SetActive(true);
    }
    this.transform.Find("Atom").gameObject.SetActive(true);
    interActModel.SetActive(false);
}

}

private void ResetdisplayModel(){
    IsdisplayModel = false;
    foreach(GameObject myAtom in Atoms){
        myAtom.transform.Find("Atom").gameObject.SetActive(true);
    }
    this.transform.Find("Atom").gameObject.SetActive(true);
    interActModel.SetActive(false);
}

private bool fadeCardIn;


IEnumerator FadeInCoroutine(TextMeshPro text,string GotText,float fadeTime)
{
    //waitTime = 0;
    //Debug.Log("Hiiiiiiiiii");
    chemicalTxt.text = GotText.Replace("\\n","\n");

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

    

 

private void OnTriggerEnter(Collider other)
    {
        string _name = other.name;

        /*if(Atoms.IndexOf(other.gameObject) >= 0){
            distance = Vector3.Distance (Atoms[].transform.position, O2.transform.position);
        }
        if (other.gameObject == H1)
        {
            
            Atom = H1.transform.Find("Atom").gameObject;
            distance = Vector3.Distance (Atom.transform.position, O2.transform.position);

        }

        if (other.gameObject == H2)
        {
            
            Atom2 = H2.transform.Find("Atom").gameObject;
            distance2 = Vector3.Distance (Atom2.transform.position, O2.transform.position);

        }*/

       
    }

    private void OnTriggerExit(Collider other) 
    {
        string _name = other.name;
    
        
       
    }


 


}


