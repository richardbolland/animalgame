using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartAscent : MonoBehaviour
{
    [UsedImplicitly]
    public void StartAscentButtonPressed()
    {
        SceneManager.LoadScene(1);
    }
    
}
