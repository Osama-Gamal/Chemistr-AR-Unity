
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Playables;



public class Quiz : MonoBehaviour
{
    public TextMeshProUGUI answerBtn1,answerBtn2,answerBtn3,ScoreText;
    public Button Btn1,Btn2,Btn3,StartBtn;
    private int Score = 0;
    private List<int> NumValues = new List<int>();
    public PlayableDirector CorrectTimeline,WrongTimeline,QuestionTimeline,ReverseQuestionTimeline,TestInteract
    ,FadeOutStateTimeLine,FinishTestTimeLine,ReverseFinishTestTimeLine;
    public Image CorrectImage,WrongImage;
    public GameObject BackgroundLayer,ChemicalLayer;
    public GameObject ScorePanel;
    public GameObject[] AtomFire;
    private float FadeRate = 0.5f;
    
    
    private int CorrectAnswer;
    // Start is called before the first frame update
    void Start()
    {
        Btn1.onClick.AddListener(Btn1Clicked);
        Btn2.onClick.AddListener(Btn2Clicked);
        Btn3.onClick.AddListener(Btn3Clicked);
        StartBtn.onClick.AddListener(StartBtnClicked);


    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void StartBtnClicked(){
        Invoke("getQuestion", 0.00001f);
        Score = 0;
        ScoreText.text = Score.ToString()+"/6";
        ReverseFinishTestTimeLine.Play();
        /*Vector3 pos = ScorePanel.transform.position;
        ScorePanel.transform.position = new Vector3(pos.x, 598, pos.z);*/
    }
    void Btn1Clicked(){
        QuestionTimeline.Play();
		if(CorrectAnswer == 1){
            CorrectTimeline.Play();
            WrongTimeline.Stop();
            Score++;
            ScoreText.text = Score.ToString()+"/6";
        }else{
            CorrectTimeline.Stop();
            WrongTimeline.Play();
        }
         Invoke("getQuestion", 3);
	}
    void Btn2Clicked(){
        QuestionTimeline.Play();
		if(CorrectAnswer == 2){
            CorrectTimeline.Play();
            WrongTimeline.Stop();
            Score++;
            ScoreText.text = Score.ToString()+"/6";
        }else{
            CorrectTimeline.Stop();
            WrongTimeline.Play();
        }
         Invoke("getQuestion", 3);
	}
    void Btn3Clicked(){
        QuestionTimeline.Play();
		if(CorrectAnswer == 3){
            CorrectTimeline.Play();
            WrongTimeline.Stop();
            Score++;
            ScoreText.text = Score.ToString()+"/6";
        }else{
            CorrectTimeline.Stop();
            WrongTimeline.Play();
        }
         Invoke("getQuestion", 3);
	}

    public void getQuestion(){

        
        if(NumValues.Count >= 6){
            FinishTestTimeLine.Play();
            NumValues.Clear();
            
            //QuestionTimeline.Play();
        }else{

        GetQuestionNumber();
        TestInteract.Play();
        ReverseQuestionTimeline.Play();
        for(int i = 0; i <= 5; i++){
            AtomFire[i].SetActive(false);
        }
        AtomFire[questionNum].SetActive(true);
        switch (questionNum)
        {
            case 0:
            answerBtn1.text = "Strontium Chloride";
            answerBtn2.text = "Calcium Chloride";
            answerBtn3.text = "Copper sulfate";
            CorrectAnswer = 2;
            break;
            case 1:
            answerBtn1.text = "Potassium chloride";
            answerBtn2.text = "Sodium chloride";
            answerBtn3.text = "Barium Sulfate";
            CorrectAnswer = 3;
            break;
            case 2:
            answerBtn1.text = "Strontium Chloride";
            answerBtn2.text = "Calcium Chloride";
            answerBtn3.text = "Barium Sulfate";
            CorrectAnswer = 1;
            break;
            case 3:
            answerBtn1.text = "Barium Sulfate";
            answerBtn2.text = "Potassium chloride";
            answerBtn3.text = "Copper sulfate";
            CorrectAnswer = 3;
            break;
            case 4:
            answerBtn1.text = "Barium Sulfate";
            answerBtn2.text = "Potassium chloride";
            answerBtn3.text = "Copper sulfate";
            CorrectAnswer = 2;
            break;
            case 5:
            answerBtn1.text = "Sodium chloride";
            answerBtn2.text = "Calcium Chloride";
            answerBtn3.text = "Copper sulfate";
            CorrectAnswer = 1;
            break;

            default:
            answerBtn1.text = "Sodium chloride";
            answerBtn2.text = "Calcium Chloride";
            answerBtn3.text = "Copper sulfate";
            CorrectAnswer = 1;
            break;
        }

        }
    }
    private int questionNum;
    private void GetQuestionNumber(){

        questionNum = Random.Range(0, 6);
         if(NumValues.IndexOf(questionNum) < 0){
            NumValues.Add(questionNum);
        }else{
            GetQuestionNumber();
            Debug.Log("Recursuin Method");
        }

    }

}
