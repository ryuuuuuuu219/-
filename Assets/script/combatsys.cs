using System;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(compare))]
public class combatsys : MonoBehaviour
{
    public List<(GameObject obj, GameObject lr, bool ismatch)> visibleenemys = new();

    private void Update()
    {
        for (int i = visibleenemys.Count - 1; i >= 0; i--)
        {
            var e = visibleenemys[i];

            if (e.lr == null || e.obj == null)
            {
                visibleenemys.RemoveAt(i);
                continue;
            }

            var disclosure = e.lr.GetComponent<disclosure>();
            bool active = disclosure != null && disclosure.isdisclosure;

            var ui = e.obj.GetComponent<UI>();
            if (ui != null)
                ui.SetVisible(active);

            bool isdisappear = disclosure != null && disclosure.state == disclosure.State.disappeared;
            if (isdisappear)
            {
                e.obj.TryGetComponent<UI>(out UI ui_comp);
                foreach (var line in ui_comp.UIobj)
                {
                    Destroy(line);
                }

                Destroy(e.obj);
                visibleenemys.RemoveAt(i);
            }
        }
    }


    public void sysListClear()
    {
        visibleenemys.Clear();
    }

    public void SetMatch(GameObject taskobj, bool match)
    {
        for (int i = 0; i < visibleenemys.Count; i++)
        {
            if (visibleenemys[i].obj == taskobj)
            {
                var e = visibleenemys[i];
                visibleenemys[i] = (e.obj, e.lr, match);
                return;
            }
        }
    }


    [Tooltip("シーンマネージャーを指定しておく")]
    public GameObject scenemanager;

    public void idListAdd(GameObject taskobj, Vector2 Initpos, int ID)
    {
        if (!visibleenemys.Exists(x => x.obj == taskobj))
        {
            GameObject ui = new GameObject(ID.ToString(), typeof(RectTransform));
            ui.transform.SetParent(this.transform, false);
            ui.transform.position = Initpos;

            ui.AddComponent<LineRenderer>();

            var dc = ui.AddComponent<disclosure>();
            dc.entity = taskobj;
            dc.scenemanager = scenemanager;

            //現状taskobjの座標を動かすと手本用の線が画面中央からずれるので、手本用の線を動かさないようにする
            //よって別のオブジェクト(ui)によって敵として表示する
            //表示はlrによる円を予定
            visibleenemys.Add((taskobj, ui, false));
        }
    }

    public void dropouting()
    {
        visibleenemys.RemoveAll(x =>
        {
            if (!x.ismatch) return false;
            
            x.lr.TryGetComponent<disclosure>(out disclosure disclosurecomp);
            disclosurecomp.cleared();

            var parent = x.obj;
            Destroy(parent);

            var tasklist = x.obj.GetComponent<UI>().UIobj;
            foreach (var line in tasklist)
            {
                Destroy(line);
            }
            return true;
        });

    }

}
