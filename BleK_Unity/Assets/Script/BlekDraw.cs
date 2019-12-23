using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class BlekDraw : MonoBehaviour
{
    public MeshFilter curMeshFilter;
    public Camera curCamera;
    public Mesh curMesh;
    public static float LineWidth = 8;
    // Start is called before the first frame update
    void Start()
    {
        curMesh = new Mesh();
    }
    const float DelayTime = 1.618f;
    static float curdelayTime = 1;
    static float SystemTime = 0;
    bool lastHaveMouse = false;
    bool startautodraw = false;
    float startautodrawTime = 0;
    float startautooffsetTime = 0;
    int startautodrawindex = 0;
    int startautodrawcount = 0;
    // Update is called once per frame
    void Update()
    {
        SystemTime = Time.time;
        if (curPosList.Count > 1)
        {
            if (SystemTime - curPosList[0].time >= curdelayTime)
            {
                HidePosQueue.Enqueue(curPosList[0]);
                curPosList.RemoveAt(0);
            }
        }
        if (Input.GetMouseButton(0))
        {
            if (lastHaveMouse == false)
            {
                startautodraw = false;
                for (int i = 0; i < DrawPosList.Count; i++)
                {
                    HidePosQueue.Enqueue(DrawPosList[i]);
                }
                DrawPosList.Clear();
                for (int i = 0; i < curPosList.Count; i++)
                {
                    HidePosQueue.Enqueue(curPosList[i]);
                }
                curPosList.Clear();
                curdelayTime = DelayTime;
            }
            lastHaveMouse = true;
            Vector3 pos = curCamera.ScreenToWorldPoint(Input.mousePosition);
            pos.z = 0;
            if (curPosList.Count > 0)
            {
                pos = Vector3.Lerp(curPosList[curPosList.Count - 1].Pos, pos, Time.deltaTime * 30);

                Vector2 dir = pos - curPosList[curPosList.Count - 1].Pos;
                if (dir.sqrMagnitude != 0)
                {
                    curPosList[curPosList.Count - 1].Dir = dir;
                    curPosList.Add(GetPosCatch().Set(pos, SystemTime));
                    DrawPosList.Add(GetPosCatch().Set(dir, SystemTime));
                }
            }
            else
            {
                curPosList.Add(GetPosCatch().Set(pos, SystemTime));
            }
        }
        else
        {
            if (lastHaveMouse == true && DrawPosList.Count > 1)
            {
                startautodraw = true;
                startautodrawindex = 0;
                startautodrawTime = curPosList[curPosList.Count - 1].time;
                startautooffsetTime = SystemTime - curPosList[curPosList.Count - 1].time;
                startautodrawcount = curPosList.Count;
                curdelayTime = Mathf.Clamp(DrawPosList[DrawPosList.Count - 1].time - DrawPosList[0].time + 0.1f, DelayTime * 0.1f, DelayTime);
            }
            lastHaveMouse = false;
        }
        if (startautodraw)
        {
            if (startautodrawindex > DrawPosList.Count - 1)
            {
                startautodrawindex -= DrawPosList.Count;
                startautodrawTime = SystemTime;
            }
            PosCatch pc = DrawPosList[startautodrawindex];
            float offsettime = pc.time - DrawPosList[0].time + startautodrawTime + startautooffsetTime;
            if (SystemTime >= offsettime)
            {
                Vector3 pos = (Vector2)curPosList[curPosList.Count - 1].Pos + pc.Dir;

                Vector2 view = curCamera.WorldToViewportPoint(pos);
                if (view.y > 1 || view.y < 0)
                {
                    foreach (PosCatch p in DrawPosList)
                    {
                        p.Dir.y = -p.Dir.y;
                    }
                }
                PosCatch next = GetPosCatch().Set(curPosList[curPosList.Count - 1].Pos + (Vector3)pc.Dir, SystemTime);
                if (view.x > 1)
                {
                    view.x -= 1;
                    Vector3 worldpos = curCamera.ViewportToWorldPoint(view);
                    next.Pos.x = worldpos.x;
                }
                else if (view.x < 0)
                {
                    view.x += 1;
                    Vector3 worldpos = curCamera.ViewportToWorldPoint(view);
                    next.Pos.x = worldpos.x;
                }
                curPosList[curPosList.Count - 1].Dir = pc.Dir;
                curPosList.Add(next);
                startautodrawindex++;
            }
        }
        Draw();
    }

    public class PosCatch
    {
        public Vector3 Pos;
        public Vector2 Dir;
        public float time;
        public float width
        {
            get
            {
                return 4 + 4 * (1 - Math.Abs((BlekDraw.SystemTime - time) * 2 / BlekDraw.curdelayTime - 1));
            }
        }
        public PosCatch Set(Vector3 pos, float t)
        {
            Pos = pos;
            time = t;
            return this;
        }
        public PosCatch Set(Vector2 dir, float t)
        {
            Dir = dir;
            time = t;
            return this;
        }
        public PosCatch Set(Vector3 pos, Vector2 dir, float t)
        {
            Pos = pos;
            Dir = dir;
            time = t;
            return this;
        }
    }
    PosCatch GetPosCatch()
    {
        if (HidePosQueue.Count > 0)
            return HidePosQueue.Dequeue();
        return new PosCatch();
    }
    Queue<PosCatch> HidePosQueue = new Queue<PosCatch>(2048);
    List<PosCatch> DrawPosList = new List<PosCatch>(512);
    List<PosCatch> curPosList = new List<PosCatch>(1024);
    Vector3[] veclist = new Vector3[1024];
    int[] trilist = new int[3096];
    int lastDrawFrame = 0;
    void Draw()
    {
        if (Time.frameCount == lastDrawFrame)
            return;
        lastDrawFrame = Time.frameCount;
        if (curPosList.Count > 1)
        {
            int veccount = curPosList.Count * 2;
            int tricount = veccount * 3 - 6;
            if (veclist.Length < veccount)
            {
                veclist = new Vector3[veccount];
                trilist = new int[tricount];
            }
            Vector3 right = Vector3.zero;
            Vector3 forward = Vector3.forward;
            PosCatch curPosCatch;
            Vector2 temp;
            float width;
            int count = curPosList.Count - 1;
            for (int i = 0; i < count; i++)
            {
                curPosCatch = curPosList[i];
                right.x = curPosCatch.Dir.x;
                right.y = curPosCatch.Dir.y;
                right = Vector3.Cross(right, forward);
                temp.x = right.x;
                temp.y = right.y;
                width = (float)Math.Sqrt(temp.x * temp.x + temp.y * temp.y);
                temp.x = temp.x / width;
                temp.y = temp.y / width;
                right.x = temp.x;
                right.y = temp.y;
                right.z = 0;
                width = curPosCatch.width;
                temp.x = temp.x * width;
                temp.y = temp.y * width;
                veclist[i * 2].x = curPosCatch.Pos.x - temp.x;
                veclist[i * 2].y = curPosCatch.Pos.y - temp.y;
                veclist[i * 2 + 1].x = curPosCatch.Pos.x + temp.x;
                veclist[i * 2 + 1].y = curPosCatch.Pos.y + temp.y;

                trilist[3 * i * 2] = i * 2;
                trilist[3 * i * 2 + 1] = (i + 1) * 2 + 1;
                trilist[3 * i * 2 + 2] = i * 2 + 1;

                trilist[(i * 2 + 1) * 3] = i * 2;
                trilist[(i * 2 + 1) * 3 + 1] = (i + 1) * 2;
                trilist[(i * 2 + 1) * 3 + 2] = (i + 1) * 2 + 1;
            }
            curPosCatch = curPosList[curPosList.Count - 1];
            width = curPosCatch.width;
            temp.x = right.x * width;
            temp.y = right.y * width;
            veclist[(curPosList.Count - 1) * 2].x = curPosCatch.Pos.x - temp.x;
            veclist[(curPosList.Count - 1) * 2].y = curPosCatch.Pos.y - temp.y;
            veclist[(curPosList.Count - 1) * 2 + 1].x = curPosCatch.Pos.x + temp.x;
            veclist[(curPosList.Count - 1) * 2 + 1].y = curPosCatch.Pos.y + temp.y;

            curMesh.Clear();
            curMesh.SetVertices(veclist, 0, veccount);
            curMesh.SetTriangles(trilist, 0, tricount, 0);
            curMesh.RecalculateNormals();
            curMeshFilter.mesh = curMesh;
        }
        else
        {
            curMeshFilter.mesh = null;
        }
    }
}
