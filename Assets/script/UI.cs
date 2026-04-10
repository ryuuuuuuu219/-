using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class UI : MonoBehaviour
{
    public bool isplayer = false;
    public List<StrokeSample> stroke;
    public Linedata sample;
    public LineRenderer originlr;

    public Canvas drawArea; // 表示枠（Canvas内）
    public float margin = 0.1f;

    [Header("Sampling")]
    public List<GameObject> UIobj;

    public List<Vector2> pts;
    public void UIEnable()
    {
        debugUI();
    }
    public void SetVisible(bool isview)
    {
        originlr.enabled = isview;
        foreach (var i in UIobj)
        {
            i.TryGetComponent<LineRenderer>(out LineRenderer lr);
            if (lr != null)
            {
                lr.enabled = isview;
            }
            i.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI text);
            if (text != null)
            {
                text.enabled = isview;
            }
        }
    }

    public void UIInit()
    {
        for (int i = UIobj.Count - 1; i >= 0; i--)
        {
            Destroy(UIobj[i]);
        }
    }

    Vector2 Convert(Vector2 pos)
    {
        RectTransform rt = drawArea.GetComponent<RectTransform>();
        Vector2 size = rt.rect.size;

        float drawableW = size.x * (1f - margin);
        float drawableH = size.y * (1f - margin);

        float w = Mathf.Max(sample.width, 1e-5f);
        float h = Mathf.Max(sample.height, 1e-5f);

        float scale = Mathf.Min(drawableW / w, drawableH / h);

        Vector2 local =
            (pos - sample.center) * scale;

        // RectTransform 中心 = (0,0)
        return local;
    }

    void debugUI()
    {
        // 元の線を表示 初期設定
        if (!isplayer)
        {
            originlr.transform.SetParent(drawArea.transform, false);
            originlr.useWorldSpace = false;
            originlr.enabled = true;
            originlr.material = new Material(Shader.Find("Sprites/Default"));
            originlr.numCapVertices = 10;
            originlr.numCornerVertices = 10;
            originlr.startWidth = 0.1f;
            originlr.endWidth = 0.5f;
            originlr.startColor = new Color(0.6f, 0.6f, 0.6f, 0.4f);
            originlr.endColor = new Color(1f, 1f, 1f, 0.5f);

            originlr.positionCount = 0;

            for (int i = 0; i < stroke.Count; i++)
            {
                Vector2 pos = Convert(stroke[i].pos);
                originlr.positionCount += 1;
                originlr.SetPosition(i, new Vector3(pos.x, pos.y, 0f));
            }

        }

        var line = sample.lines;
        int size = line.Count;
        for (int i = 0; i < size; i++)
        {

            Color c; string s;
            var pos = line[i].position;

            if (!isplayer)
            {
                pos = Convert(pos);
            }


            #region 要素確認用UI
            switch (line[i].categoryinfo)
            {
                case LineInfo.category.start:
                    s = "start" + i.ToString();
                    c = Color.white; break;
                case LineInfo.category.end:
                    s = "end" + i.ToString();
                    c = Color.white; break;
                case LineInfo.category.crosspoint:
                    s = "cp" + i.ToString();
                    c = Color.blue; break;
                case LineInfo.category.virtualcp:
                    s = "vcp" + i.ToString();
                    c = Color.pink; break;
                case LineInfo.category.vertex:
                    s = "vertex" + i.ToString();
                    c = Color.green; break;
                case LineInfo.category.step:
                default:
                    s = "step" + i.ToString();
                    c = Color.cyan; break;
            }
            c = new Color(c.r, c.g, c.b, 0.5f);
            if (line[i].isxminP) s += "\nxmin";
            if (line[i].isxmaxP) s += "\nxmax";
            if (line[i].isyminP) s += "\nymin";
            if (line[i].isymaxP) s += "\nymax";


            var ui = new GameObject(gameObject.name + "ui" + (i));
            if (originlr != null)
            {
                ui.transform.SetParent(drawArea.transform, false);
                ui.transform.localPosition = new Vector3(pos.x - 100f, pos.y, 0);
            }
            else
            {
                ui.transform.SetParent(this.transform, false);
                ui.transform.position = pos;
                var cpos = ui.transform.localPosition;
                ui.transform.localPosition = new Vector3(cpos.x - 100f, cpos.y, 0);
            }
            UIobj.Add(ui);
            ui.transform.localScale = Vector3.one;
            var rect = ui.AddComponent<RectTransform>();
            {
                rect.pivot = new Vector2(1, 0.5f);
            }
            var textui = ui.AddComponent<TextMeshProUGUI>();
            textui.text = s;
            textui.fontSize = (size < 50) ? 20 : (size < 100) ? 14 : 8;
            textui.alignment = TextAlignmentOptions.Right;
            textui.color = c;
            var lr = ui.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = false;
            lr.SetPosition(0, new Vector3(100f, 0, 0f));
            lr.SetPosition(1, new Vector3(0, 0, 0f));
            lr.startWidth = 0.05f;
            lr.endWidth = 0.01f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = c;
            lr.endColor = c;
            #endregion 要素確認用UI
        }
    }
}
