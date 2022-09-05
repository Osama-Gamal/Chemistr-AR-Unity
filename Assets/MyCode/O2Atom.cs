using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using TMPro;

public class O2Atom : MonoBehaviour
{
    public GameObject O2,H1,H2,interActModel,FusionCard;
    public TextMeshPro chemicalTxt,InteractTxt;
    public float InteractSpeed = 0.08f;
    public float InteractStopDistance = 5f;
    public string MainText,NewText;
    private GameObject Atom,Atom2;

    private float distance,distance2;
    private bool isInteracting;
    // Start is called before the first frame update
    void Start()
    {
        FusionCard.SetActive(false);
        interActModel.SetActive(false);

    }

    // Update is called once per frame
    private float distanceReverse,distanceReverse2;
    void Update()
    {

    

        if(Atom != null && Atom2 != null){
            distanceReverse = Vector3.Distance(H1.transform.position, this.transform.position);
            distanceReverse2 = Vector3.Distance(H2.transform.position, this.transform.position);

            if(Atom != null && Atom2 != null && (distanceReverse2 >= 0.2f || distanceReverse >= 0.2f)){
                //Debug.Log(Vector3.Distance (AtomImage.transform.position, this.transform.position));
                Atom.transform.parent = H1.transform;
                Atom2.transform.parent = H2.transform;

                Atom.transform.position = Vector3.MoveTowards(Atom.transform.position,
                                                            H1.transform.position + new Vector3(0,0.1f,0) , InteractSpeed * Time.deltaTime);
                Atom2.transform.position = Vector3.MoveTowards(Atom2.transform.position,
                                                            H2.transform.position + new Vector3(0,0.1f,0) , InteractSpeed * Time.deltaTime);
                    if(fadeCardIn){
                        StartCoroutine(FadeOutAndInCoroutine(chemicalTxt,MainText,0.5f));
                        chemicalTxt.text.Replace("\\n","\n");
                        InteractTxt.text = "Diffusion";
                        fadeCardIn = false;
                        Atom.transform.parent = H1.transform;
                        Atom2.transform.parent = H2.transform;
                        
                        ScaleObject(Atom.transform.Find("Atomic Orbit").gameObject,1f,1);
                        ScaleObject(Atom2.transform.Find("Atomic Orbit").gameObject,1f,1);
                        ScaleObject(this.transform.Find("Atom").gameObject.transform.Find("Atomic Orbit").gameObject,1f,1);
                        CancelInvoke();
                        ResetdisplayModel();                              

                        
                    }
                        
                if(Atom.transform.position == H1.transform.position){
                  Atom = null;
                }
                if(Atom2.transform.position == H2.transform.position){
                  Atom2 = null;
                }
            }else{
                if(Atom != null && Atom2 != null && distance >= InteractStopDistance / 200 || distance2 >= InteractStopDistance / 200){
                    Atom.transform.parent = O2.transform;
                    Atom2.transform.parent = O2.transform;
                    if(distance >= InteractStopDistance / 200)
                    Atom.transform.position = Vector3.MoveTowards(Atom.transform.position,
                                                                    O2.transform.position , InteractSpeed * Time.deltaTime);
                    if(distance2 >= InteractStopDistance / 200)
                    Atom2.transform.position = Vector3.MoveTowards(Atom2.transform.position,
                                                                    O2.transform.position , InteractSpeed * Time.deltaTime);                                                
                    distance = Vector3.Distance (Atom.transform.position, O2.transform.position);
                    distance2 = Vector3.Distance (Atom2.transform.position, O2.transform.position);

                            
                            if(!fadeCardIn){
                                StartCoroutine(FadeOutAndInCoroutine(chemicalTxt,NewText,0.5f));
                                InteractTxt.text = "Fusion";
                                fadeCardIn = true;

                                ScaleObject(Atom.transform.Find("Atomic Orbit").gameObject,-1f,1);
                                ScaleObject(Atom2.transform.Find("Atomic Orbit").gameObject,-1f,1);
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
    Atom2.SetActive(false);
    this.transform.Find("Atom").gameObject.SetActive(false);
    interActModel.SetActive(true);
}else{
    IsdisplayModel = false;
    Atom.SetActive(true);
    Atom2.SetActive(true);
    this.transform.Find("Atom").gameObject.SetActive(true);
    interActModel.SetActive(false);
}

}

private void ResetdisplayModel(){
    IsdisplayModel = false;
    Atom.SetActive(true);
    Atom2.SetActive(true);
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

        if (other.gameObject == H1)
        {
            
            Atom = H1.transform.Find("Atom").gameObject;
            distance = Vector3.Distance (Atom.transform.position, O2.transform.position);

        }

        if (other.gameObject == H2)
        {
            
            Atom2 = H2.transform.Find("Atom").gameObject;
            distance2 = Vector3.Distance (Atom2.transform.position, O2.transform.position);

        }

       
    }

    private void OnTriggerExit(Collider other) 
    {
        string _name = other.name;
    
        if (other.gameObject == H1)
        {


            
            /*Cl.SetActive(true);
            NaCl.SetActive(false);*/
        }
       
    }


 


}


