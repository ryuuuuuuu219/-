using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public struct VertexInfo
{
    public int index;          // 点列中のインデックス
    public Vector2 position;   // 座標
    public float angle;        // 内角（度）
    public float signedAngle;  // 符号付き角度（-180～180）
}
public struct PointInfo
{
    public int index;          // 点列中のインデックス
    public Vector2 position;   // 座標
}
public struct LineInfo
{
    public enum category
    {
        start = 0,
        step,
        crosspoint,
        virtualcp,
        vertex,
        end
    }
    public int index;          // 点列中のインデックス
    public Vector2 position;   // 座標
    public VertexInfo? vertexInfo;
    public category categoryinfo;
    public bool isxminP, isxmaxP, isyminP, isymaxP;
}

public struct Linedata
{
    public List<LineInfo> lines;
    public Vector2 center;
    public float width,height;
}

[RequireComponent(typeof(UI))]
public class analyzer : MonoBehaviour
{
    public GameObject compareobj;

    public bool isplayer = false;
    [Header("Sampling")]
    List<PointInfo> cp0;
    List<VertexInfo> vertexs;
    List<PointInfo> simplified;
    List<LineInfo> analyzedinfo;

    public int cp, v, step;
    public float difficulty;

    UI ui;
    public Linedata info;

    [SerializeField, Range(0.1f, 1f)] 
    float epsilon;
    [SerializeField, Range(15f, 90f)]
    float angleThresholdDeg;
    //メモ　enemでstep crosspoint virtualcpを定義
    //Vector2とenumの構造体の配列と交点数、仮想の交点数を記録する


    private void Awake()
    {
        ui = GetComponent<UI>();
        analyzedinfo = new List<LineInfo>();
    }

    public void analyze(List<StrokeSample> samples, bool _isplayer = false)
    {
        this.isplayer = _isplayer;

        List<Vector2> pts = samples.ToPositions();

        cp0 = crossingpoint(pts);
        cp0.Sort((a, b) => a.index.CompareTo(b.index));

        vertexs = DetectVertices(pts, angleThresholdDeg);
        vertexs.Sort((a, b) => a.index.CompareTo(b.index));

        simplified = Simplify(pts, cp0, vertexs, epsilon);

        analyzedinfo = MargeArrays(simplified, cp0, vertexs);
        info = ApplyBounds(analyzedinfo, 0, analyzedinfo.Count-1);

        ui.stroke = samples;
        ui.sample = info;
        ui.isplayer = isplayer;
        if (!isplayer)
        {
            var c = transform.AddComponent<requiment>();
            c.setting(info, cp, v, step, difficulty);
        }
        compareobj.GetComponent<compare>().conteinofTask(info, isplayer);

        ui.pts = pts;

        ui.UIEnable();
    }

    float boundstolerance = 0.01f;
    Linedata ApplyBounds(List<LineInfo> infos, int start, int end)
    {
        float xmin = float.PositiveInfinity;
        float xmax = float.NegativeInfinity;
        float ymin = float.PositiveInfinity;
        float ymax = float.NegativeInfinity;

        List<int> ixMin=new(), ixMax = new(), iyMin = new(), iyMax = new();

        for (int i = start; i <= end; i++)
        {
            Vector2 p = infos[i].position;

            if (p.x < xmin) { xmin = p.x; ixMin.Clear(); ixMin.Add(i); }
            else if (Mathf.Abs(p.x - xmin) < boundstolerance) { ixMin.Add(i); }
            if (p.x > xmax) { xmax = p.x; ixMax.Clear(); ixMax.Add(i); }
            else if (Mathf.Abs(p.x - xmax) < boundstolerance) { ixMax.Add(i); }
            if (p.y < ymin) { ymin = p.y; iyMin.Clear(); iyMin.Add(i); }
            else if (Mathf.Abs(p.y - ymin) < boundstolerance) { iyMin.Add(i); }
            if (p.y > ymax) { ymax = p.y; iyMax.Clear(); iyMax.Add(i); }
            else if (Mathf.Abs(p.y - ymax) < boundstolerance) { iyMax.Add(i); }

        }

        float width = xmax - xmin;  
        float height = ymax - ymin;
        Vector2 center = new Vector2(xmin+width / 2, ymin+height / 2);

        var xminSet = new HashSet<int>(ixMin);
        var xmaxSet = new HashSet<int>(ixMax);
        var yminSet = new HashSet<int>(iyMin);
        var ymaxSet = new HashSet<int>(iyMax);

        for (int i = start; i <= end; i++)
        {
            var li = infos[i];
            li.isxminP = xminSet.Contains(i);
            li.isxmaxP = xmaxSet.Contains(i);
            li.isyminP = yminSet.Contains(i);
            li.isymaxP = ymaxSet.Contains(i);
            infos[i] = li;
        }

        var ret = new Linedata
        {
            lines = infos,
            center = center,
            width = width,
            height = height,

        };
        return ret;

    }


    List<LineInfo> MargeArrays(
    List<PointInfo> pts,
    List<PointInfo> cp,
    List<VertexInfo> vertexInfos)
    {
        var result = new List<LineInfo>();

        // 高速参照用マップ
        var vertexMap = new Dictionary<int, VertexInfo>();
        foreach (var v in vertexInfos)
            vertexMap[v.index] = v;

        var crossMap = new Dictionary<int, List<PointInfo>>();
        foreach (var c in cp)
        {
            if (!crossMap.ContainsKey(c.index))
                crossMap[c.index] = new List<PointInfo>();
            crossMap[c.index].Add(c);
        }

        int n = pts.Count;

        for (int i = 0; i < n; i++)
        {
            int srcIdx = pts[i].index;   // ← 元 pts index

            bool isVertex = vertexMap.ContainsKey(srcIdx);
            bool hasCross = crossMap.ContainsKey(srcIdx);

            LineInfo.category cat;
            if (i == 0) cat = LineInfo.category.start;
            else if (i == n - 1) cat = LineInfo.category.end;
            else if (isVertex) cat = LineInfo.category.vertex;
            else if (hasCross) cat = LineInfo.category.crosspoint; // ★step を作らない
            else cat = LineInfo.category.step;

            if (cat != LineInfo.category.crosspoint)
            {
                var info = new LineInfo
                {
                    index = srcIdx,                // ← 元 index を保持
                    position = pts[i].position,
                    categoryinfo = cat,
                    vertexInfo = isVertex ? vertexMap[srcIdx] : (VertexInfo?)null
                };
                result.Add(info);
            }

            if (hasCross)
            {
                foreach (var c in crossMap[srcIdx])
                {
                    result.Add(new LineInfo
                    {
                        index = srcIdx,
                        position = c.position,
                        categoryinfo = LineInfo.category.crosspoint,
                        vertexInfo = null
                    });
                }
            }
        }

        return result;
    }

    public float vertixeps = 0.1f;
    // ===== DetectVertices v2 : signed-angle hills + integration + radius =====

    // 角度ノイズを切る（deg）
    [SerializeField, Range(0.1f, 5f)]
    float angleNoiseDeg = 1.0f;

    // 「山」全体の総屈折角の閾値（deg）: これ未満は頂点扱いしない
    // ※ angleThresholdDeg を「内角」ではなく「総屈折角」として使うなら、ここは angleThresholdDeg を流用してOK
    // 今回は引数 angleThresholdDeg を総屈折角閾値として使う設計にします。

    // 半径閾値（小さいほど鋭い）
    // pts のスケールが「点間距離=1」なら 0.5～3 あたりが目安。ゲーム内スケールなら要調整。
    [SerializeField, Range(0.01f, 20f)]
    float sharpRadiusThreshold = 0.2f;

    // 半径推定に使う「極大点からの距離」(k)。
    // 2～6が安定。点間距離が1なら 3 が扱いやすい。
    [SerializeField, Range(1, 12)]
    int radiusFitK = 3;

    // 半径推定で探索する幅（極大点±rangeで最小Rを探す）
    // k固定だとサンプル間隔の影響を受けるので、少し探索して最小を採用すると安定。
    [SerializeField, Range(0, 12)]
    int radiusSearchRange = 2;

    // 90度以上の「急折れ」検出。これを満たすなら半径0扱い（無限曲率に近い）
    [SerializeField, Range(30f, 179f)]
    float snapCornerDeg = 90f;

    List<VertexInfo> DetectVertices(List<Vector2> pts, float angleThresholdDeg)
    {
        var vertices = new List<VertexInfo>();
        int n = pts.Count;
        if (n < 3)
        {
            v = 0;
            return vertices;
        }

        // 1) 符号付き相対角 dAng を作る（各 i は「点 i を中心」にした角度）
        // dAng[i] は i=1..n-2 が有効
        float[] dAng = new float[n];
        for (int i = 1; i <= n - 2; i++)
        {
            Vector2 a = pts[i] - pts[i - 1];
            Vector2 b = pts[i + 1] - pts[i];

            if (a.sqrMagnitude < 1e-12f || b.sqrMagnitude < 1e-12f)
            {
                dAng[i] = 0f;
                continue;
            }

            a.Normalize();
            b.Normalize();

            float signed = Mathf.Atan2(
                a.x * b.y - a.y * b.x,
                Vector2.Dot(a, b)
            ) * Mathf.Rad2Deg;

            // ノイズカット
            if (Mathf.Abs(signed) < angleNoiseDeg) signed = 0f;

            dAng[i] = signed;
        }

        // 2) 「山」（符号連続領域）を抽出して評価
        int iPtr = 1;
        while (iPtr <= n - 2)
        {
            // 0はスキップ
            if (Mathf.Abs(dAng[iPtr]) < 1e-6f)
            {
                iPtr++;
                continue;
            }

            int sign = dAng[iPtr] > 0 ? 1 : -1;

            int start = iPtr;
            int end = iPtr;

            float sum = 0f;
            float peakAbs = 0f;
            int peakIdx = iPtr;

            // 符号が同じ（かつ0でない）間伸ばす
            while (end <= n - 2)
            {
                float a = dAng[end];
                if (Mathf.Abs(a) < 1e-6f) break;         // 0で区切る
                if ((a > 0 ? 1 : -1) != sign) break;     // 符号反転で区切る

                sum += a;

                float absA = Mathf.Abs(a);
                if (absA >= peakAbs)
                {
                    peakAbs = absA;
                    peakIdx = end;
                }

                end++;
            }

            // 区間は [start, end-1]
            int segStart = start;
            int segEnd = end - 1;

            // 次の探索開始
            iPtr = end;

            float absSum = Mathf.Abs(sum);

            // 山の総屈折角がしきい値未満なら捨てる
            if (absSum < angleThresholdDeg) continue;

            // 近接頂点（同じ場所）を抑止
            if (HasNearbyVertex(vertices, pts, peakIdx, vertixeps))
                continue;

            // 連続で頂点が並ぶのを抑止（元のロジック踏襲）
            int lastIdx = vertices.Count > 0 ? vertices[^1].index : -1000;
            if (peakIdx - lastIdx <= 1) continue;

            // 3) 鋭さ（半径）評価
            // 3-1) 1点で急折れ（|dAng| >= snapCornerDeg）なら「鋭い」扱い（半径0相当）
            bool snapCorner = peakAbs >= snapCornerDeg;

            float radius = snapCorner ? 0f : EstimateMinRadiusAroundPeak(pts, peakIdx, radiusFitK, radiusSearchRange);

            // 半径が推定不能（collinearなど）は Mathf.Infinity を返す
            bool isSharp = snapCorner || (radius <= sharpRadiusThreshold);

            // 「鋭さ」条件を満たさないなら頂点扱いしない（＝なだらかなカーブ）
            if (!isSharp) continue;

            // 内角も入れておく（表示/デバッグ用）
            // signedAngle は「山の総和」を入れる方が“屈折量”として便利。
            // angle は “内角” として 0..180 を入れる（デバッグ用）
            float innerAngle = 180f - Mathf.Clamp(absSum, 0f, 180f); // 参考値（総和が大きいほど内角は小さい）

            vertices.Add(new VertexInfo
            {
                index = peakIdx,
                position = pts[peakIdx],
                angle = innerAngle,
                signedAngle = sum
            });
        }

        v = vertices.Count;
        return vertices;
    }

    // peakIdx を中心に「3点円」で半径を推定し、最小半径を返す（鋭い＝小さい）
    float EstimateMinRadiusAroundPeak(List<Vector2> pts, int peakIdx, int k, int searchRange)
    {
        int n = pts.Count;
        float best = float.PositiveInfinity;

        // k を固定せず多少動かして最小Rを取る（安定化）
        int kMin = Mathf.Max(1, k - searchRange);
        int kMax = k + searchRange;

        for (int kk = kMin; kk <= kMax; kk++)
        {
            int i0 = peakIdx - kk;
            int i1 = peakIdx;
            int i2 = peakIdx + kk;

            if (i0 < 0 || i2 >= n) continue;

            float r = CircumRadius(pts[i0], pts[i1], pts[i2]);
            if (r < best) best = r;
        }

        return best;
    }

    // 3点A,B,Cが作る外接円半径（曲率半径の推定）
    // collinear なら Infinity
    float CircumRadius(Vector2 A, Vector2 B, Vector2 C)
    {
        float a = Vector2.Distance(B, C);
        float b = Vector2.Distance(C, A);
        float c = Vector2.Distance(A, B);

        // 三角形面積 = |cross(AB, AC)| / 2
        Vector2 AB = B - A;
        Vector2 AC = C - A;
        float cross = AB.x * AC.y - AB.y * AC.x;
        float area2 = Mathf.Abs(cross); // = 2*area

        if (area2 < 1e-6f) return float.PositiveInfinity;

        // R = abc / (4A) = abc / (2*area2)
        float R = (a * b * c) / (2f * area2);
        return R;
    }

    bool HasNearbyVertex(
    List<VertexInfo> vertices,
    List<Vector2> pts,
    int center,
    float eps)
    {
        Vector2 cur = pts[center];
        float eps2 = eps * eps;

        foreach (var v in vertices)
        {
            if ((pts[v.index] - cur).sqrMagnitude <= eps2)
                return true;
        }
        return false;
    }

    List<PointInfo> crossingpoint(List<Vector2> pts)
    {
        var ret = new List<PointInfo>();

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector2 a1 = pts[i];
            Vector2 a2 = pts[i + 1];
            Vector2 b1;
            Vector2 b2;

            for (int j = 0; j < i - 1; j++)
            {
                b1 = pts[j];
                b2 = pts[j + 1];

                if (SegmentIntersect(a1, a2, b1, b2, out var ip))
                {
                    if (!IsNear(ret, ip))
                    {
                        var adddata = new PointInfo()
                        {
                            index = i+1,
                            position = ip
                        };
                        ret.Add(adddata);
                    }
                }
            }
        }
        cp = ret.Count;
        return ret;
    }

    const float mergeEps = 0.01f;

    bool IsNear(List<PointInfo> list, Vector2 p)
    {
        foreach (var q in list)
            if (Vector2.Distance(p, q.position) < mergeEps)
                return true;
        return false;
    }

    bool SegmentIntersect(
    Vector2 A, Vector2 B,
    Vector2 C, Vector2 D,
    out Vector2 intersection)
    {
        intersection = Vector2.zero;

        Vector2 r = B - A;
        Vector2 s = D - C;
        float denom = r.x * s.y - r.y * s.x;

        if (Mathf.Abs(denom) < 1e-6f)
            return false; // 平行

        Vector2 diff = C - A;
        float t = (diff.x * s.y - diff.y * s.x) / denom;
        float u = (diff.x * r.y - diff.y * r.x) / denom;

        if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
        {
            intersection = A + t * r;
            return true;
        }
        return false;
    }


    List<PointInfo> Simplify(List<Vector2> pts, List<PointInfo> crosspts, List<VertexInfo>vertexs, float epsilon)
    {
        var ptsinfo = new List<PointInfo>();
        int n = pts.Count;
        for (int i = 0; i < n; i++) {
            var info = new PointInfo()
            {
                index = i,
                position = pts[i]
            };
            ptsinfo.Add(info);
        }
        if (n < 2)
        {
            step = ptsinfo.Count;
            return ptsinfo;
        }

        var result = new List<PointInfo>();

        if (crosspts.Count == 0 && vertexs.Count == 0)
        {
            bool[] keep = new bool[n];
            keep[0] = keep[n - 1] = true;

            DouglasPeucker(ptsinfo, 0, n - 1, epsilon, keep);

            for (int i = 0; i < n; i++)
            {
                if (!keep[i]) continue;
                result.Add(ptsinfo[i]);
            }
            step = result.Count;
            return result;
        }
        else
        {
            var total = new List<PointInfo>();
            var idxTotal = new List<int>(); // 交点/頂点が total のどこに入ったか（区切り用）

            total.Add(new PointInfo() { index=0,position=pts[0] });

            int j = 0;
            int k = 0;
            for (int i = 1; i < n; i++)
            {
                total.Add(ptsinfo[i]);

                // idx[j] は「pts の i 番目の点まで来たら交点 j を入れる」扱い
                while (j < crosspts.Count && crosspts[j].index == i)
                {
                    total.Add(new PointInfo
                    {
                        index = crosspts[j].index,   // 「属している線分の元 index」
                        position = crosspts[j].position
                    });
                    idxTotal.Add(total.Count - 1);
                    j++;
                }

                while (k < vertexs.Count && vertexs[k].index == i)
                {
                    idxTotal.Add(total.Count - 1);
                    k++;
                }

            }

            int n2 = total.Count;
            if (n2 < 2)
            {
                for (int i = 0; i < n2; i++)
                {
                    result.Add(ptsinfo[i]);
                }
                return result;
            }

            bool[] keep = new bool[n2];
            keep[0] = keep[n2 - 1] = true;

            // 交点位置で区間ごとに DP（交点が境界になるイメージ）
            int p1 = 0;
            for (int m = 0; m < idxTotal.Count; m++)
            {
                int p2 = idxTotal[m];

                // 無効区間を弾く
                if (p2 <= p1) continue;

                // ★境界点を必ず残す（これがないと交点/頂点が落ちる）
                keep[p1] = true;
                keep[p2] = true;

                DouglasPeucker(total, p1, p2, epsilon, keep);
                p1 = p2;
            }
            // 最終区間
            if (p1 < n2 - 1)
            {
                keep[p1] = true;
                keep[n2 - 1] = true;
                DouglasPeucker(total, p1, n2 - 1, epsilon, keep);
            }

            // total 基準で拾う
            for (int i = 0; i < n2; i++)
            {
                if (keep[i])
                {
                    result.Add(total[i]);
                }
            }
        }
        step = result.Count;
        return result;
    }


    void DouglasPeucker(
    List<PointInfo> points,
    int start, int end,
    float epsilon,
    bool[] keep)
    {
        if (end <= start + 1) return;

        float maxDist = 0f;
        int index = -1;

        for (int i = start + 1; i < end; i++)
        {
            float d = DistancePointToLine(
                points[i].position,
                points[start].position,
                points[end].position
            );

            if (d > maxDist)
            {
                maxDist = d;
                index = i;
            }
        }

        if (maxDist > epsilon)
        {
            keep[index] = true;
            DouglasPeucker(points, start, index, epsilon, keep);
            DouglasPeucker(points, index, end, epsilon, keep);
        }
    }
    float DistancePointToLine(Vector2 p, Vector2 a, Vector2 b)
    {
        if (a == b) return Vector2.Distance(p, a);

        Vector2 ap = p - a;
        Vector2 ab = b - a;
        float t = Vector2.Dot(ap, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        Vector2 closest = a + ab * t;
        return Vector2.Distance(p, closest);
    }


}
