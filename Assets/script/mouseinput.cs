using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct StrokeSample
{
    public float time, actualtime;
    public Vector2 pos;

    public StrokeSample(float time, Vector2 pos)
    {
        this.actualtime = Time.time;
        this.time = time;
        this.pos = pos;
    }
}

public static class StrokeSampleExtensions
{
    public static List<Vector2> ToPositions(this List<StrokeSample> samples)
    {
        var positions = new List<Vector2>(samples.Count);
        for (int i = 0; i < samples.Count; i++)
        {
            positions.Add(samples[i].pos);
        }
        return positions;
    }
}
[RequireComponent(typeof(LineRenderer))]
public class mouseinput : MonoBehaviour
{
    public GameObject compareobj;
    compare compare;

    [Header("Sampling")]
    [SerializeField] float minDistance = 0.01f; // 記録する最小移動距離
    [SerializeField] Camera targetCamera;

    [Header("Debug")]
    public List<StrokeSample> samples = new();

    LineRenderer lr;
    bool isRecording = false;
    Vector2 lastRecordedPos;

    float timer = 0f;

    Mouse mouse;
    UI ui;

    void Awake()
    {
        ui = GetComponent<UI>();
        analyzer = GetComponent<analyzer>();
        
        lr = transform.GetComponent<LineRenderer>();
        lr.positionCount = 0;
        lr.useWorldSpace = true;
        lr.enabled = true;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.numCapVertices = 10;
        lr.numCornerVertices = 10;
        lr.startWidth = 0.3f;
        lr.endWidth = 0.3f;

        compare = compareobj.GetComponent<compare>();

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        mouse = Mouse.current;

        if (mouse == null) 
        { 
            Debug.LogError("No mouse detected");
            return; 
        }

        // クリック押下開始
        if (mouse.leftButton.wasPressedThisFrame)
        {
            BeginStroke();
        }

        // 押下中
        if (isRecording && mouse.leftButton.isPressed)
        {
            RecordPoint();
        }

        // クリック離した
        if (isRecording && mouse.leftButton.wasReleasedThisFrame)
        {
            EndStroke();
            compare.compareTasks();
        }
    }
    void LateUpdate()
    {
    }

    void BeginStroke()
    {
        isRecording = true;
        timer = 0f;
        samples.Clear();

        lr.positionCount = 0;

        Vector2 pos = GetMouseWorldPos();
        lastRecordedPos = pos;

        ui.UIInit();
        AddSample(pos);
    }

    void RecordPoint()
    {
        timer += Time.deltaTime;

        Vector2 pos = GetMouseWorldPos();
        var velocity = Vector2.Distance(pos, lastRecordedPos);
        if (velocity < minDistance)
            return;


        AddSample(pos);
        lastRecordedPos = pos;
    }

    public analyzer analyzer;
    void EndStroke()
    {
        isRecording = false;
        // ここで解析処理に渡す
        analyzer.analyze(samples, true);
    }

    void AddSample(Vector2 pos)
    {
        samples.Add(new StrokeSample(timer, pos));

        lr.positionCount++;
        lr.SetPosition(lr.positionCount - 1, pos);
    }


    Vector2 GetMouseWorldPos()
    {
        Vector2 screenPos = mouse.position.ReadValue();
        Vector3 p = new Vector3(screenPos.x, screenPos.y,
            Mathf.Abs(targetCamera.transform.position.z));
        return targetCamera.ScreenToWorldPoint(p);
    }
}
