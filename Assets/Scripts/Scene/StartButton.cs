using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButton : MonoBehaviour
{
    [Tooltip("게임 시작 버튼을 눌렀을 때 이동할 인게임 씬 이름입니다.")]
    [SerializeField] private string inGameSceneName = "InGameScene";

    public void StartGame()
    {
        GameSessionManager.Instance?.BeginNewRun();
        SceneManager.LoadScene(inGameSceneName);
    }
}
