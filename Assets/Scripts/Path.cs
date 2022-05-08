using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System.Runtime;
using System.Drawing;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

[System.Serializable]
public class Path
{
    [SerializeField, HideInInspector]
    List<Vector2> points;

    [SerializeField, HideInInspector]
    bool isClosed;

    [SerializeField, HideInInspector]
    bool autoSetControlPoints;
    public int numPoints { get { return points.Count; } }
    public int numSegments { get { return points.Count / 3; } }

    public Vector2 this[int i] { get { return points[i]; } }
    public bool AutoSetControlPoints
    {
        get
        {
            return autoSetControlPoints;
        }
        set
        {
            if (autoSetControlPoints != value)
            {
                autoSetControlPoints = value;
                if (autoSetControlPoints)
                {
                    AutoSetAllControlPoints();
                }
            }
        }
    }
    public Path(Vector2 center)
    {
        points = new List<Vector2>{
            center+Vector2.left,
            center+(Vector2.left+Vector2.up)*.5f,
            center+(Vector2.right+Vector2.down)*.5f,
            center + Vector2.right
        };
    }

    public void AddSegment(Vector2 anchorPos)
    {
        points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
        points.Add((points[points.Count - 1] + anchorPos) * .5f);
        points.Add(anchorPos);
        if (autoSetControlPoints)
        {
            AutoSetAllAffectedControlPoints(points.Count - 1);

        }
    }

    public Vector2[] getPointsInSegment(int i)
    {
        return new Vector2[] { points[i * 3], points[i * 3 + 1], points[i * 3 + 2], points[loopIndex(i * 3 + 3)] };
    }

    public void MovePoints(int i, Vector2 pos)
    {
        Vector2 deltaMove = pos - points[i];

        if(i%3==0||!autoSetControlPoints){
            points[i] = pos;
            if (autoSetControlPoints)
            {
                AutoSetAllAffectedControlPoints(i);
            }
            else
            {
                if (i % 3 == 0)
                {
                    if (i + 1 < points.Count || isClosed)
                    {
                        points[loopIndex(i + 1)] += deltaMove;
                    }
                    if (i - 1 >= 0 || isClosed)
                    {
                        points[loopIndex(i - 1)] += deltaMove;
                    }
                }
                else
                {
                    bool nextPointIsAnchor = (i + 1) % 3 == 0;
                    int correspondingIndex = (nextPointIsAnchor) ? i + 2 : i - 2;
                    int anchorIndex = (nextPointIsAnchor) ? i + 1 : i - 1;
                    if (correspondingIndex >= 0 && correspondingIndex < points.Count || isClosed)
                    {
                        float dist = (points[loopIndex(anchorIndex)] - points[loopIndex(correspondingIndex)]).magnitude;
                        Vector2 dir = (points[loopIndex(anchorIndex)] - pos).normalized;
                        points[loopIndex(correspondingIndex)] = points[loopIndex(anchorIndex)] + dir * dist;
                    }
                }
            }
        }
    }



    void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
    {
        for (int i = updatedAnchorIndex - 3; i <= updatedAnchorIndex + 3; i += 3)
        {
            if (i >= 0 && i < points.Count || isClosed)
            {
                AutoSetAnchorControlPoints(loopIndex(i));
            }
        }
        AutoSetStartAndEndControls();
    }
    void AutoSetAllControlPoints()
    {
        for (int i = 0; i < points.Count; i++)
        {
            AutoSetAnchorControlPoints(i);
        }
        AutoSetStartAndEndControls();
    }
    void AutoSetAnchorControlPoints(int anchorIndex)
    {
        Vector2 anchorPos = points[anchorIndex];
        Vector2 dir = Vector2.zero;
        float[] neighbourDistances = new float[2];

        if (anchorIndex - 3 >= 0 || isClosed)
        {
            Vector2 offset = points[loopIndex(anchorIndex - 3)] - anchorPos;
            dir += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }
        if (anchorIndex + 3 >= 0 || isClosed)
        {
            Vector2 offset = points[loopIndex(anchorIndex + 3)] - anchorPos;
            dir -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }
        dir.Normalize();
        for (int i = 0; i < 2; i++)
        {
            int controlIndex = anchorIndex + i * 2 - 1;
            if (controlIndex >= 0 && controlIndex < points.Count || isClosed)
            {
                points[loopIndex(controlIndex)] = anchorPos + dir * neighbourDistances[i] * .5f;

            }
        }
    }
    void AutoSetStartAndEndControls()
    {
        if (!isClosed)
        {
            points[1] = (points[0] + points[2]) * .5f;
            points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * .5f;

        }
    }
    public void ToggleClosed()
    {
        isClosed = !isClosed;
        if (isClosed)
        {
            points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
            points.Add(points[0] * 2 - points[1]);
            if (autoSetControlPoints)
            {
                AutoSetAnchorControlPoints(0);
                AutoSetAnchorControlPoints(points.Count - 3);
            }
        }
        else
        {
            points.RemoveRange(points.Count - 2, 2);
            AutoSetStartAndEndControls();
        }
    }
    int loopIndex(int i)
    {
        return (i + points.Count) % points.Count;
    }
}
