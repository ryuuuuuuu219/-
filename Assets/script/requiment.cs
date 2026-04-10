using System.Collections.Generic;
using UnityEngine;

public class requiment : MonoBehaviour
{
    public string debuglog = "";

    Linedata taskdata;
    public struct ComparePattern
    {
        public bool requireCrosspointnum;
        public int cptolerance;
        public bool requireVertexnum;
        public int vtolerance;
        public bool requirepointcordinate_relation;
        public int prtolerance;

    }
    public bool issetup = false;
    public ComparePattern p;

    /// <summary>
    /// 複雑さによってタスクの要求を変える関数
    /// </summary>
    public void setting(Linedata taskdata,
    int cp, int v, int step,
    float difficulty)
    {
        p.requireVertexnum = true;
        p.cptolerance = Mathf.Max(0, Mathf.FloorToInt(Mathf.Log(cp * difficulty - 3,2)));
        p.requireCrosspointnum = true;
        p.vtolerance = Mathf.Max(0, Mathf.FloorToInt(Mathf.Log(v * difficulty - 5, 3)));
        p.requirepointcordinate_relation = true;
        p.prtolerance = Mathf.Max(0, Mathf.FloorToInt(difficulty*1.8f));

        debuglog = "要求設定:\n";
        if (p.requireVertexnum)
            debuglog += $"-頂点数:許容誤差 {p.vtolerance}\n";
        if (p.requireCrosspointnum)
            debuglog += $"-交差点数:許容誤差 {p.cptolerance}\n";
        if (p.requirepointcordinate_relation)
            debuglog += $"-主要点の位置関係:許容誤差 {p.prtolerance}\n";


        issetup = true;
    }
}
