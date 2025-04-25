using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BeginButton : MonoBehaviour
{
    //Make sure to attach these Buttons in the Inspector
    public Button m_YourFirstButton;
    public Button m_Your2Button;

    void Start()
    {
        //Calls the TaskOnClick/TaskWithParameters/ButtonClicked method when you click the Button
        m_YourFirstButton.onClick.AddListener(TaskOnClick);
        m_Your2Button.onClick.AddListener(TaskOnClick2);
    }

    void TaskOnClick()
    {
        //Output this to console when Button1 or Button3 is clicked
        SceneManager.LoadScene("SampleScene");
    }
    void TaskOnClick2()
    {
        //Output this to console when Button1 or Button3 is clicked
        SceneManager.LoadScene("controls");
    }
}