using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
public class CircleFormator : MonoBehaviour
{
    public GameObject prefab;
    public int ElectronNum;
    public float ElectronDistance = 0.05f;
    public bool Interactable;
    public float InteractSpeed = 0.08f;
    public float InteractStopDistance = 5f;
    private GameObject Atom;
    private float distance;
    // Start is called before the first frame update
    void Start()
    {
        CreateElectronsAroundPoint(ElectronNum,this.transform.position,ElectronDistance);
    }

    // Update is called once per frame
    void Update()
    {
        /*if(Atom != null && distance >= InteractStopDistance / 200){
            Atom.transform.position = Vector3.MoveTowards(Atom.transform.position,
                                                            this.transform.position , InteractSpeed * Time.deltaTime);
            distance = Vector3.Distance (Atom.transform.position, this.transform.position);
            //Debug.Log(distance);
        }*/
    }

 /* private void OnTriggerEnter(Collider other) {
      if(Interactable == true)
        if(other.name == "atomic orbit"){
            Atom = other.gameObject.transform.parent.gameObject;
            distance = Vector3.Distance (Atom.transform.position, this.transform.position);
            Debug.Log(distance);
            TrackerManager.Instance.GetTracker<ObjectTracker>().Stop();
        }
    
    
}*/
public void CreateElectronsAroundPoint (int num, Vector3 point, float radius){

    
     for (int i = 0; i < num; i++){
         
         /* Distance around the circle */  
         var radians = 2 * Mathf.PI / num * i;
         
         /* Get the vector direction */ 
         var vertrical = Mathf.Sin(radians);
         var horizontal = Mathf.Cos(radians); 
         
         var spawnDir = new Vector3 (horizontal, 0, vertrical);
         
         /* Get the spawn position */ 
         var spawnPos = point + spawnDir * radius; // Radius is just the distance away from the point
         
         /* Now spawn */
         var electron = Instantiate (prefab, spawnPos, Quaternion.identity,this.transform) as GameObject;
         
         /* Rotate the enemy to face towards player */
         electron.transform.LookAt(point);
         
         /* Adjust height */
         //enemy.transform.Translate (new Vector3 (0, enemy.transform.localScale.y / 2, 0));
     }
}

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ElectronDistance);
    }

}
