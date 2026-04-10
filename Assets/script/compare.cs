using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static requiment;


public class compare : MonoBehaviour
{
    Linedata playerinput;
    combatsys system;

    public TextMeshProUGUI uitext;
    int matchcount = 0,enemycount;

    private void Start()
    {
        system = GetComponent<combatsys>();
        uitext.text = "一致タスク数: " + matchcount + "\n";
    }

    private void Update()
    {
        enemycount = system.visibleenemys.Count;
        uitext.text = "一致タスク数: " + matchcount + "\n" +
            "タスク数:" + enemycount;

    }

    public void conteinofTask(Linedata task, bool isplayer = false)
    {
        if (isplayer)
        {
            playerinput = task;
        }
    }

    List<(GameObject obj, Linedata ld, requiment p)> enemytasks;
    //public string debuglog = "";
    int count = 0;
    //比較関数
    public void compareTasks()
    {
        if (playerinput.lines == null)
        {
            //debuglog += "プレイヤー入力が未設定のため比較を中断\n";
            return;
        }

        enemytasks = new();

        foreach (var e in system.visibleenemys)
        {
            if (!e.ismatch)
            {
                e.obj.TryGetComponent<analyzer>(out analyzer analyzercomp);
                e.obj.TryGetComponent<requiment>(out requiment requimentcomp);
                if (analyzercomp != null &&
                    requimentcomp != null)
                {
                    enemytasks.Add((e.obj, analyzercomp.info, requimentcomp));
                }
            }
        }

        //debuglog += "比較を開始:\n " + "試行回数: " + count + "\n";

        count++;
        if (enemytasks.Count== 0)
        {
            //debuglog += "比較対象の敵タスクが存在しません。\n";
            return;
        }
        foreach (var t in enemytasks)
        {
            int index = enemytasks.IndexOf(t);
            if(t.p == null)
            {
                //debuglog += "タスクの比較パターンが未設定です。インデックス: " + index + "\n";
                continue;
            }
            if (compare_main(t.ld, t.p.p, playerinput))
            {
                matchcount++;
                //debuglog += "タスク一致（〇）インデックス: " + index + "\n";
                system.SetMatch(t.obj, true);
            }
            else
            {
                //debuglog += "タスク不一致（×）インデックス: " + index + "\n";
                system.SetMatch(t.obj, false);
            }
            //debuglog += t.obj.name + "のデバッグログ:\n" + t.p.debuglog + "\n";
        }
        system.dropouting();
    }

    
    public struct Pointdetail
    {
        public bool equalstart,
        equalend,
        equalvertex_n,
        betweenstart_end,
        betweenstart_vertex,
        betweenvertex_end,
        betweenvertex_vertex;

    }

    bool compare_main(Linedata task, ComparePattern comparePattern, Linedata player)
    {

        if (task.lines == null)
        {
            //debuglog += "・比較対象が null です（VertexNum）\n";
            return false;
        }
        if (player.lines == null)
        {
            //debuglog += "・lines が null です（VertexNum）\n";
            return false;
        }



        bool result = true;
        // 1. 頂点数
        if (comparePattern.requireVertexnum)
        {
            var tolerance = comparePattern.vtolerance;
            if (!CompareVertexNum(task, player, tolerance))
            {
                //debuglog += "・頂点数が一致しません。\n";
                result = false;
            }
        }

        // 2. 交差数
        if (comparePattern.requireCrosspointnum)
        {
            var tolerance = comparePattern.cptolerance;
            if (!CompareCrossNum(task, player, tolerance))
            {
                //debuglog += "・交点数が一致しません。\n";
                result = false;
            }
        }

        // 3. 主要点（xmin/xmax/ymin/ymax）
        if (comparePattern.requirepointcordinate_relation)
        {
            var tolerance = comparePattern.prtolerance;
            if (!CompareExtremePattern(task, player, tolerance))
            {
                //debuglog += "・極値パターン（xmin/xmax/ymin/ymax）の構造的な位置関係が一致しません。\n";
                result = false;
            }
        }

        return result;

    }

    #region BasicComparison
    bool CompareVertexNum(Linedata a, Linedata b, int tolerance)
    {
        int va = a.lines.FindAll(l => l.categoryinfo == LineInfo.category.vertex).Count;
        int vb = b.lines.FindAll(l => l.categoryinfo == LineInfo.category.vertex).Count;

        return Mathf.Abs(va - vb) <= tolerance;
    }
    bool CompareCrossNum(Linedata a, Linedata b, int tolerance)
    {
        int ca = a.lines.FindAll(l => l.categoryinfo == LineInfo.category.crosspoint).Count;
        int cb = b.lines.FindAll(l => l.categoryinfo == LineInfo.category.crosspoint).Count;

        return Mathf.Abs(ca - cb) <= tolerance;
    }

    #endregion
    #region ExtremePattern

    List<(int idx, int state)> ExtractExtremePattern(Linedata d)
    {
        var list = new List<(int, int)>();
        for (int i = 0; i < d.lines.Count; i++)
        {
            var l = d.lines[i];
            int state = 0;
            if (l.isxminP) state |= 1 << 0;
            if (l.isxmaxP) state |= 1 << 1;
            if (l.isyminP) state |= 1 << 2;
            if (l.isymaxP) state |= 1 << 3;
            if (state != 0) list.Add((i, state));
            
            //複合している場合→xmin&yminの場合は1<<0 |+ 1<<2 = 5 = 2(0101) 
            //比較する場合は0001(xminの比較)&0101(5)&0111(7) = 0001(xminあり) 
            //入力と出力が一致する
        }
        return list;
    }

    bool CompareExtremePattern(Linedata a, Linedata b, int tolerance)
    {
        var pa = ExtractExtremePattern(a);
        var pb = ExtractExtremePattern(b);

        int tolerance_Extreme = tolerance;
        int matchedKeys = 0; // 4ビットのうち何個「構造込みで一致」したか

        for (int keyBit = 0; keyBit < 4; keyBit++)
        {
            int key = 1 << keyBit;
            bool foundMatchForKey = false;

            for (int ia = 0; ia < pa.Count && !foundMatchForKey; ia++)
            {
                var (idxA, stateA) = pa[ia];
                if ((stateA & key) == 0) continue;

                for (int ib = 0; ib < pb.Count; ib++)
                {
                    var (idxB, stateB) = pb[ib];
                    if ((stateB & key) == 0) continue;

                    // key を持つ点同士で、相対構造が一致するか？
                    var detailA = returnpointdetail(a, idxA);
                    var detailB = returnpointdetail(b, idxB);

                    if (comparedetail(detailA, detailB))
                    {
                        foundMatchForKey = true;
                        break;
                    }
                }
            }

            if (foundMatchForKey) matchedKeys++;
            //else debuglog += $"・・・↑：{(keyBit == 0 ? "xmin" : keyBit == 1 ? "xmax" : keyBit == 2 ? "ymin" : "ymax")}\n";
        }

        int mismatches = 4 - matchedKeys;
        return mismatches <= tolerance_Extreme;
    }

#endregion
    #region RelativeStructure

    Pointdetail returnpointdetail(Linedata d, int index)
    {
        List<Pointdetail> ret = new ();
        int start = 0;
        int end = d.lines.Count - 1;
        List<int> vertexes = new List<int>();
        for(int i = 0; i < end; i++)
        {
            if (d.lines[i].categoryinfo == LineInfo.category.vertex)
            {
                vertexes.Add(i);
            }
        }

        var l = d.lines[index];
        Pointdetail pd = new Pointdetail();
        if (l.isxminP || l.isxmaxP || l.isyminP || l.isymaxP)
        {
            pd.equalstart = index == start;
            pd.equalend = index == end;
            pd.equalvertex_n = vertexes.Contains(index);
            pd.betweenstart_end = (index > start && index < end);
            pd.betweenstart_vertex = (index > start && vertexes.Exists(v => v > start && v < index));
            pd.betweenvertex_end = (index < end && vertexes.Exists(v => v > index && v < end));
            pd.betweenvertex_vertex = (vertexes.Exists(v => v < index) && vertexes.Exists(v => v > index));

        }
        return pd;
    }

    bool comparedetail(Pointdetail detaA, Pointdetail detaB)
    {
        if ((detaB.equalstart == detaA.equalstart) &&
            (detaB.equalend == detaA.equalend) &&
            (detaB.equalvertex_n == detaA.equalvertex_n) &&
            (detaB.betweenstart_end == detaA.betweenstart_end) &&
            (detaB.betweenstart_vertex == detaA.betweenstart_vertex) &&
            (detaB.betweenvertex_end == detaA.betweenvertex_end) &&
            (detaB.betweenvertex_vertex == detaA.betweenvertex_vertex))
        {
            /*debuglog += "・・・・構造不一致詳細:\n" +
                "A: " + returnPointdetail(detaA) + "\n" +
                "B: " + returnPointdetail(detaB) + "\n";*/
            return true;
        }

        return false;
    }
    #endregion

}
