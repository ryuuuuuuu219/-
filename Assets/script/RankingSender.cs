using System.Runtime.InteropServices;
using UnityEngine;

public class RankingSender : MonoBehaviour
{
    public static RankingSender Instance;

    [Header("Unityroom Ranking ID")]
    [SerializeField] int rankingID = 1;

    bool isSending = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void SendScoreToUnityroom(int id, int score);
#endif

    /// <summary>
    /// スコア送信（intのみ）
    /// </summary>
    public void SubmitScore(int score)
    {
        if (isSending) return;
        isSending = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        SendScoreToUnityroom(rankingID, score);
#else
        Debug.Log($"[RankingMock] ID:{rankingID} Score:{score}");
#endif
    }

    /// <summary>
    /// floatスコア用（自動変換）
    /// </summary>
    public void SubmitFloatScore(float score, float scale = 100f)
    {
        int converted = Mathf.RoundToInt(score * scale);
        SubmitScore(converted);
    }

    /// <summary>
    /// ランキング表示
    /// </summary>
    public void ShowLeaderboard()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ShowLeaderboardInternal(rankingID);
#else
        Debug.Log("[RankingMock] Show Leaderboard");
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ShowLeaderboardInternal(int id);
#endif
}
