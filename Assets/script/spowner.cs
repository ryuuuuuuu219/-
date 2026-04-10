using System;
using System.Collections.Generic;
using UnityEngine;


public class spowner : MonoBehaviour
{
    [SerializeField] Canvas canvas;
    [SerializeField] GameObject compareobj;
    [SerializeField] GameObject taskprefub;
    [SerializeField, Range(0f, 5f)] float spownradius;

    combatsys system;

    [SerializeField, Range(1, 20000)] int seed;
    System.Random rng;

    Linedata playerinput;
    public List<Linedata> enemytasks = new ();

    private void Start()
    {
        system = compareobj.GetComponent<combatsys>();
        rng = new System.Random(seed);
        taskcount = rng.Next(3, 7);
        for (int i = 0; i < taskcount; i++)
        {
            taskInitialate(i);
        }
        threshold = (1f+ difficulty) * Mathf.Pow(2, difficulty - 1f +(float)rng.NextDouble());
    }

    [SerializeField, Range(0f, 2f)] float difficulty = 2f;
    int taskcount;
    [SerializeField]float counter = 0f, threshold = 10f;
    private void Update()
    {
        counter += Time.deltaTime;
        if (counter > threshold)
        {
            counter = 0f;
            taskInitialate(taskcount);
            taskcount++;
        threshold = (1f + difficulty) * Mathf.Pow(2, difficulty - 1f + (float)rng.NextDouble());
        }
    }

    void taskInitialate(int id)
    {
        var task = Instantiate(taskprefub, Vector2.zero, Quaternion.identity);

        task.TryGetComponent<LineRenderer>(out LineRenderer lr);
        lr.enabled = false;

        task.name = "enemytask_" + id.ToString("D2")+ "(diff:" + difficulty.ToString("F2") + ")";
        task.transform.SetParent(this.transform, false);
        task.TryGetComponent<UI>(out UI uicomp);
        task.TryGetComponent<analyzer>(out analyzer analyzercomp);
        task.TryGetComponent<taskgenerate>(out taskgenerate taskDOTScomp);

        uicomp.drawArea = canvas;
        analyzercomp.compareobj = this.compareobj;
        analyzercomp.difficulty = difficulty;    

        taskDOTScomp.seed = rng.Next(seed);
        taskDOTScomp.steps = rng.Next(50, 151);
        taskDOTScomp.stepslength = (float)rng.NextDouble() * 0.2f + 0.05f;

        Vector2 spownpos = RandomInsideUnitCircle(rng) * Mathf.Min(spownradius, 6f);
        system.idListAdd(task, spownpos, id);
    }
    Vector2 RandomInsideUnitCircle(System.Random rng)
    {
        double angle = rng.NextDouble() * Math.PI * 2.0;
        return new Vector2(
            (float)(Math.Cos(angle)),
            (float)(Math.Sin(angle))
        );
    }
}
