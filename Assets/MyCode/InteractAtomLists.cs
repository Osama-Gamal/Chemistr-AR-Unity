using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using TMPro;

public class InteractAtomLists : MonoBehaviour
{
   
    private GameObject ThisAtom,FusionCard,interActModel;
    public TextMeshPro chemicalTxt,InteractTxt;
    public float InteractSpeed = 0.08f;
    public float InteractStopDistance = 5f;
    public string MainText;
    private string NewText;
    public List<GameObject> Atoms = new List<GameObject>();
    private List<GameObject> RealAtoms = new List<GameObject>();


[System.Serializable]
    public class ChemicalItemsList{
        public List<GameObject> ListAtoms = new List<GameObject>();
        public GameObject InteractModel;
        public string NewText;

    }
    public List<ChemicalItemsList> ListOfListAtoms = new List<ChemicalItemsList>();

    private float distance,distance2;
    private bool isInteracting;
    // Start is called before the first frame update
    void Start()
    {
        //interActModel = this.transform.Find("My3DModel").gameObject;
        FusionCard = this.transform.Find("FusionQuad").gameObject;
        ThisAtom = this.transform.Find("Atom").gameObject;
        FusionCard.SetActive(false);
        //interActModel.SetActive(false);
    }

    // Update is called once per frame
    private float distanceReverse;
    private bool outRange,AtomsReversed;
    void Update()
    {
         foreach(ChemicalItemsList listAtomsClass in ListOfListAtoms){

            foreach(GameObject myAtom in listAtomsClass.ListAtoms){
                distanceReverse = Vector3.Distance(myAtom.transform.position, this.transform.position);
                if(distanceReverse >= 0.2f){
                    outRange = true;
                    break;
                }else{
                    outRange = false;
                    if(Atoms.IndexOf(myAtom) >= 0){
                    }else{
                        Atoms.Add(myAtom);//listAtomsClass.ListAtoms
                        RealAtoms.Add(myAtom.transform.Find("Atom").transform.gameObject);
                        NewText = listAtomsClass.NewText;
                        interActModel =listAtomsClass.InteractModel;
                    }
                }
            }
                
            if(!outRange)
                    break;
                    

        }

           
            if(outRange){
          
                foreach(GameObject myAtom in RealAtoms){
                    //myAtom.transform.Find("Atom").transform.parent = myAtom.transform;
                    myAtom.transform.parent = Atoms[RealAtoms.IndexOf(myAtom)].transform;
                    
                    myAtom.transform.position = Vector3.MoveTowards(myAtom.transform.position,
                                                            Atoms[RealAtoms.IndexOf(myAtom)].transform.position + new Vector3(0,0.1f,0) , InteractSpeed * Time.deltaTime);
                    
                    if(myAtom.transform.position == Atoms[RealAtoms.IndexOf(myAtom)].transform.position + new Vector3(0,0.1f,0)){
                        Atoms.Remove(Atoms[RealAtoms.IndexOf(myAtom)]);
                        RealAtoms.Remove(myAtom);
                    }
                }
                    if(fadeCardIn){
                        StartCoroutine(FadeOutAndInCoroutine(chemicalTxt,MainText,0.5f));
                        chemicalTxt.text.Replace("\\n","\n");
                        InteractTxt.text = "Diffusion";
                        fadeCardIn = false;

                        foreach(GameObject myAtom in RealAtoms){
                           ScaleObject(myAtom.transform.Find("Atomic Orbit").gameObject,1f,1);
                        }

                        ScaleObject(this.transform.Find("Atom").gameObject.transform.Find("Atomic Orbit").gameObject,1f,1);
                        CancelInvoke();
                        ResetdisplayModel();                              
                    }

            }else{
                
                foreach(GameObject myAtom in RealAtoms){
                    //distance = Vector3.Distance (myAtom.transform.Find("Atom").transform.position, ThisAtom.transform.position);
                    //myAtom.transform.Find("Atom").transform.parent = ThisAtom.transform;

                    distance = Vector3.Distance (myAtom.transform.position, ThisAtom.transform.position);
                    myAtom.transform.parent = ThisAtom.transform;
                    
                    
                    if(distance >= InteractStopDistance / 200)
                    myAtom.transform.position = Vector3.MoveTowards(myAtom.transform.position,
                                                                    ThisAtom.transform.position , InteractSpeed * Time.deltaTime);
                    
                    
                    }  

                    if(!fadeCardIn){
                        StartCoroutine(FadeOutAndInCoroutine(chemicalTxt,NewText,0.5f));
                        InteractTxt.text = "Fusion";
                        fadeCardIn = true;
                        foreach(GameObject myAtom in RealAtoms){
                           //ScaleObject(myAtom.transform.Find("Atom").transform.Find("Atomic Orbit").gameObject,-1f,1);
                           ScaleObject(myAtom.transform.Find("Atomic Orbit").gameObject,-1f,1);
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
    foreach(GameObject myAtom in RealAtoms){
        myAtom.SetActive(false);
    }
    this.transform.Find("Atom").gameObject.SetActive(false);
    interActModel.SetActive(true);
}else{
    IsdisplayModel = false;
    foreach(GameObject myAtom in RealAtoms){
        myAtom.SetActive(true);
    }
    this.transform.Find("Atom").gameObject.SetActive(true);
    interActModel.SetActive(false);
}

}

private void ResetdisplayModel(){
    IsdisplayModel = false;
    foreach(GameObject myAtom in RealAtoms){
        myAtom.SetActive(true);
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


public static class LerpHelpExtensions
    {
        public static Coroutine LerpCoroutine(this GameObject gameObj, float time, System.Action<float> block, bool includeZero = false)
        {
            MonoBehaviour behaviour = gameObj.GetComponent<MonoBehaviour> ();
            if ( behaviour == null )
                return null;
           
            return behaviour.LerpCoroutine (time, block, includeZero);
        }
       
       
        public static Coroutine LerpCoroutine(this MonoBehaviour behaviour, float time, System.Action<float> block, bool includeZero = false)
        {
            return behaviour.StartCoroutine (_LerpCoroutine (time, block, includeZero));
        }
       
        static IEnumerator _LerpCoroutine(float time, System.Action<float> block, bool includeZero = false)
        {
            if ( time <= 0f )
            {
                block(1f);
                yield break;
            }
           
            float timer = 0f;
            if ( includeZero )
            {
                block(0f);
                yield return null;
            }
           
            while ( timer < time )
            {
                timer += Time.deltaTime;
                block(Mathf.Lerp(0f, 1f, timer/time));
                yield return null;
            }
        }
       
        public static Coroutine AnimateComponent<T>(this MonoBehaviour behaviour, float time, System.Action<T,float> block) where T : Component
        {
            if ( block == null )
                return null;
           
            T component = behaviour.GetComponent<T> ();
            if ( component == null || !behaviour.gameObject.activeInHierarchy)
                return null;
           
            return behaviour.StartCoroutine (_LerpCoroutine(time, (timer)=>
            {
                block(component, timer);
            }));
           
        }
    }


