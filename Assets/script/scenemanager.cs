using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using unityroom.Api;

public class scenemanager : MonoBehaviour
{
    public GameObject textobj;
    TextMeshProUGUI text;

    public int miss = 0;
    public float score = 0f;
    public float lifetime = 0f;

    private void Start()
    {
        text = textobj.GetComponent<TextMeshProUGUI>();
    }

    bool isSent = false;
    void Update()
    {
        lifetime += Time.deltaTime;
        if (miss > 0 && !isSent)
        {
            isSent = true;
            StartCoroutine(SendAndMove());
        }
            text.text = "ミス数: " + miss.ToString() + "\n" +
                "スコア: " + score.ToString("F2") + "\n" +
                "生存時間: " + lifetime.ToString("F2") + "秒";
    }
    
    IEnumerator SendAndMove()
    {
        UnityroomApiClient.Instance.SendScore(1, score, ScoreboardWriteMode.Always);
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("result");
    }
}
