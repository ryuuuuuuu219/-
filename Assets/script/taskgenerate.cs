using System.Collections.Generic;
using UnityEngine;

public enum MotionState
{
    Straight = 1,
    Refract = 2,
    Curve = 3,
}

public class taskgenerate : MonoBehaviour
{
    List<StrokeSample> task;

    [Header("Sampling")]
    public int initrays = 8;                 // 8 → 45°刻み（最終スナップ）
    public int steps = 80;
    public float stepslength = 0.1f;
    public int seed = 12345;
    public List<int> statedice = new List<int>() { 1, 1, 1, 2, 2, 3, 3 };

    [Header("Refract")]
    public float refractMaxDeg = 135f;
    public float refractMinDeg = 90f;

    [Header("Curve")]
    public float curveMaxDeg = 270f;
    public float curveMinDeg = 45f;
    public int curvestepcount = 10;
    public float curvesteplength = 0.5f;

    [Header("Curve Smoothness")]
    [Tooltip("カーブ中の1ステップ角度変化の上限（度）。45以下推奨。")]
    public float maxCurveDeltaPerStepDeg = 15f;

    System.Random rng;

    // --- 離散方向管理（Straight/Refract の基準） ---
    int dirIndex;
    float classAngle; // = 360 / initrays

    MotionState state = MotionState.Straight;
    int stateRemain = 0;

    // Refract 用（方向インデックス差分）
    int refractStep = 0;

    // Curve 用（連続角度）
    float currentDeg;          // カーブ中の連続角度（度）
    float curveDeltaDeg;       // 1ステップで増える角度（度）。符号込み
    int curvecounter = 0;

    analyzer analyzer;
    public string debugstr = "";

    void Start()
    {
        rng = new System.Random(seed);

        classAngle = 360f / initrays;
        dirIndex = rng.Next(initrays);

        task = taskGen();

        analyzer = GetComponent<analyzer>();
        analyzer.analyze(task);
    }

    List<StrokeSample> taskGen()
    {
        var ret = new List<StrokeSample>();

        var p0 = new StrokeSample { actualtime = 0, time = 0, pos = Vector2.zero };
        ret.Add(p0);

        // 1点目
        Vector2 dir = DegToDir(dirIndex * classAngle);
        var p1 = new StrokeSample { actualtime = 0, time = 0, pos = dir * stepslength };
        ret.Add(p1);

        var prev = p1;

        state = MotionState.Straight;
        stateRemain = 20 - rng.Next(5);

        for (int i = 2; i < steps; i++)
        {
            StateChange();

            var next = new StrokeSample
            {
                actualtime = 0,
                time = 0,
                pos = NextPos(prev.pos)
            };

            debugstr += $"Step {i}: State={state}, Pos={next.pos}, DirIndex={dirIndex}, stateRemain={stateRemain}, currentDeg={currentDeg}\n";  

            ret.Add(next);
            prev = next;
        }

        return ret;
    }

    MotionState RollState()
    {
        stepslength = 0.1f + ((float)rng.NextDouble() * 0.05f);
        curvestepcount = 15 + rng.Next(10);
        curvesteplength = 0.5f + ((float)rng.NextDouble() * 0.1f);
        return (MotionState)statedice[rng.Next(statedice.Count)];
    }

    Vector2 NextPos(Vector2 pos)
    {
        switch (state)
        {
            case MotionState.Straight: return Straight(pos);
            case MotionState.Refract: return Refract(pos);
            case MotionState.Curve: return Curve(pos);
            default: return Straight(pos);
        }
    }

    void StateChange()
    {
        if (stateRemain > 0)
        {
            stateRemain--;
            return;
        }

        state = RollState();
        stateRemain = 20 - rng.Next(5);

        switch (state)
        {
            case MotionState.Straight:
                // 何もしない
                break;

            case MotionState.Refract:
            {
                int minStep = Mathf.RoundToInt(refractMinDeg / classAngle);
                int maxStep = Mathf.RoundToInt(refractMaxDeg / classAngle);

                int step = rng.Next(minStep, maxStep + 1);
                refractStep = rng.Next(2) == 0 ? step : -step;
                break;
            }

            case MotionState.Curve:
            {
                // カーブは「連続角度」で滑らかに回す
                curvecounter = 0;
                stateRemain = curvestepcount;

                // 総回転量を決める（度）
                float totalAbs = curveMinDeg + (float)rng.NextDouble() * (curveMaxDeg - curveMinDeg);
                float total = (rng.Next(2) == 0) ? totalAbs : -totalAbs;

                // 1ステップ当たりの回転量（度）
                float idealDelta = total / curvestepcount;

                // 曲率（角度変化）を maxCurveDeltaPerStepDeg 以下に制限
                float clamped = Mathf.Clamp(idealDelta, -maxCurveDeltaPerStepDeg, maxCurveDeltaPerStepDeg);

                // ただし 0 になってカーブしないのは困るので最低1度は回す
                if (Mathf.Abs(clamped) < 1e-3f)
                    clamped = (total >= 0f) ? 1f : -1f;

                curveDeltaDeg = clamped;
                break;
            }
        }
    }

    Vector2 Straight(Vector2 pos)
    {
        currentDeg = dirIndex * classAngle; // ★同期
        Vector2 dir = DegToDir(dirIndex * classAngle);
        return pos + dir * stepslength;
    }

    Vector2 Refract(Vector2 pos)
    {
        if (stateRemain == 1)
        {
            dirIndex = Mod(dirIndex + refractStep, initrays);
        }
        currentDeg = dirIndex * classAngle; // ★同期
        Vector2 dir = DegToDir(dirIndex * classAngle);
        return pos + dir * stepslength;
    }

    Vector2 Curve(Vector2 pos)
    {
        // 連続角度で滑らかに回す
        currentDeg += curveDeltaDeg;
        curvecounter++;

        // カーブ終了 → 8方向にスナップして直線へ
        if (curvecounter >= curvestepcount)
        {
            dirIndex = Mod(Mathf.RoundToInt(currentDeg / classAngle), initrays);

            curvecounter = 0;
            state = MotionState.Straight;
            stateRemain = 3;
        }

        Vector2 dir = DegToDir(currentDeg);
        return pos + dir * curvesteplength;
    }

    static Vector2 DegToDir(float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    static int Mod(int x, int m)
    {
        return (x % m + m) % m;
    }
}
