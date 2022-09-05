using System.Net.Mime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    // Start is called before the first frame update
    public void changeScene(int SceneID){
        SceneManager.LoadScene(SceneID);
    }
    public void QuitGame(){
        Application.Quit();
    }
}
