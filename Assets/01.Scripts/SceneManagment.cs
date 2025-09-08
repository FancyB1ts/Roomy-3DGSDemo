using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneManagment : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Image img;
    void Start()
    {
        
    }
    public void GoToGameScene()
    {
       var loaded= SceneManager.LoadSceneAsync(1);
        while(loaded != null && loaded.progress<1f)
        {
            if(img!=null)
                img.fillAmount = loaded.progress;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
