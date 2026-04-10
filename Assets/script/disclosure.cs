using System;
using System.Collections.Generic;
using System.Xml.Schema;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;


public class disclosure : MonoBehaviour
{
    Vector2 mousepoint;

    public float radial = 0.5f;
    public int vertexcount = 16;

    public bool isdisclosure = false, 
        iscleared = false;
    public GameObject entity;

    public GameObject scenemanager;
    scenemanager sm;

    public enum State
    {
        move,
        disappeared,
        destroyready
    }
    public State state = State.move;

    LineRenderer lr;
    Color color;
    TextMeshProUGUI text;

    public void cleared()
    {
        if (state == State.move)
        {
            state = State.disappeared;
            isdisclosure = false;
            iscleared = true;
            sm.score += currentscore;

            color = new Color(0f, 1f, 0f, 0.5f);
        }
    }

    private void Start()
    {
        isdisclosure = false;
        cam = Camera.main;
        mouse = Mouse.current;

        GameObject textobj = new GameObject("text", typeof(RectTransform));
        textobj.transform.SetParent(this.transform, false);
        textobj.GetComponent<RectTransform>().localScale = Vector3.one * 0.01f;
        text = textobj.AddComponent<TextMeshProUGUI>();

        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 16;
        text.color = Color.white;


        lr = transform.GetComponent<LineRenderer>();
        lr.startWidth = radial * 0.03f;
        lr.endWidth = radial * 0.03f;
        lr.positionCount = vertexcount;
        lr.useWorldSpace = false;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.loop = true;

        color = new Color(1f, 0f, 0f, 0.5f);
        Setcircle(1f, color);

        int cp, v, step;
        cp = entity?.GetComponent<analyzer>()?.cp ?? 1;
        v = entity?.GetComponent<analyzer>()?.v ?? 1;
        step = entity?.GetComponent<analyzer>()?.step ?? 1;
        float dist = transform.position.magnitude;

        speed = (dist * step)/(cp + v + 1) * 0.001f;
        speed = Mathf.Max(speed, 0.01f);

        rb = transform.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        col = transform.AddComponent<CircleCollider2D>();
        col.radius = radial;

        sm = scenemanager.GetComponent<scenemanager>();
    }

    CircleCollider2D col;
    Rigidbody2D rb;

    void Setcircle(float r, Color c)
    {
        lr = transform.GetComponent<LineRenderer>();
        lr.startColor = c;
        lr.endColor = c;

        for (int i = 0; i < vertexcount; i++)
        {
            float angle = i * Mathf.PI * 2f / vertexcount;
            float x = Mathf.Cos(angle) * radial;
            float y = Mathf.Sin(angle) * radial;
            lr.SetPosition(i, new Vector3(x, y, 0) * r);
        }
    }

    public float speed;
    Camera cam; Mouse mouse;

    float currentscore;
    void FixedUpdate()
    {
        var Velocity = transform.position.normalized;
        if (rb != null)
        {
            rb.linearVelocity = -Velocity * speed;
        }
 
        Vector2 screenPos = mouse.position.ReadValue();
        Vector3 p = new Vector3(screenPos.x, screenPos.y,
            Mathf.Abs(cam.transform.position.z));
        mousepoint = cam.ScreenToWorldPoint(p);


        Vector2 objpos = this.transform.position;
        float dist = Vector2.Distance(mousepoint, objpos);
        if (dist < radial&&
            state == State.move)
        {
            isdisclosure = true;
        }
        else
        {
            isdisclosure = false;
        }


        float disttoCenter = Vector2.Distance(Vector2.zero, objpos);

        currentscore = Mathf.Max(0f, (disttoCenter - radial * 2) / speed);
        text.text = gameObject.name + "\n"+
            currentscore.ToString("F2");
        if (disttoCenter < radial*2  &&
            state == State.move)
        {
            state = State.disappeared;
            isdisclosure = false;
            sm.miss++;
        }
        if (state == State.disappeared)
        {
            isdisclosure = false;

            timer += Time.deltaTime;
            timer = Mathf.Clamp(timer, 0f, 1f);

            color.a = 0.5f * (1f - timer);
            Setcircle(1f - timer, color);
            col.radius = radial * (1f - timer);
            if (timer >= 1f)
            {
                state = State.destroyready;
            }
        }

        if (state == State.destroyready)
        {
            //消滅準備
            Destroy(this.gameObject);
        }
    }

    float timer = 0f;

}
