using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor
{
    PathCreator creator;
    Path path;

    const float segmentSelectDistanceThreshold = .1f;
    int selectedSegmentIndex = -1;


    private void OnSceneGUI()
    {
        Input();
        Draw();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUI.BeginChangeCheck();
        if (GUILayout.Button("Create new"))
        {
            Undo.RecordObject(creator, "Create new");
            creator.CreatePath();
            path = creator.path;
        }

        bool isClosed = GUILayout.Toggle(path.IsClosed, "Closed");
        if (isClosed != path.IsClosed)
        {
            Undo.RecordObject(creator, "Roggle closed");
            path.IsClosed = isClosed;
        }

        bool autoSetControlPoints = GUILayout.Toggle(path.AutoSetControlPoints, "Auto Set Control Points");
        if (autoSetControlPoints != path.AutoSetControlPoints)
        {
            Undo.RecordObject(creator, "Toggle auto set controls");
            path.AutoSetControlPoints = autoSetControlPoints;
        }
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
    }

    void Input()
    {
        Event guiEvent = Event.current;
        Vector2 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;
        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
        {
            if (selectedSegmentIndex != -1)
            {
                Undo.RecordObject(creator, "Split Segment");
                path.SplitSegment(mousePos, selectedSegmentIndex);
            }
            else if (!path.IsClosed)
            {
                Undo.RecordObject(creator, "Add Segment");
                path.AddSegment(mousePos);
            }
        }

        if (guiEvent.type == EventType.MouseUp && guiEvent.button == 1)
        {
            float minDistToAnchor = .05f;
            int closestAnchorIndex = -1;
            for (int i = 0; i < path.numPoints; i += 3)
            {
                float dst = Vector2.Distance(mousePos, path[i]);
                if (dst < minDistToAnchor)
                {
                    minDistToAnchor = dst;
                    closestAnchorIndex = i;
                }
            }
            if (closestAnchorIndex != -1)
            {
                Undo.RecordObject(creator, "Delete Segment");
                path.DeleteSegment(closestAnchorIndex);
            }
        }
        if (guiEvent.type == EventType.MouseMove)
        {
            float minDistToSegment = segmentSelectDistanceThreshold;
            int newSelectedSegmentIndex = -1;
            for (int i = 0; i < path.numSegments; i++)
            {
                Vector2[] points = path.getPointsInSegment(i);
                float dist = HandleUtility.DistancePointBezier(mousePos, points[0], points[3], points[1], points[2]);
                if (dist < minDistToSegment)
                {
                    minDistToSegment = dist; newSelectedSegmentIndex = i;
                }
            }
            if (newSelectedSegmentIndex != selectedSegmentIndex)
            {
                selectedSegmentIndex = newSelectedSegmentIndex;
                HandleUtility.Repaint();
            }
        }

    }
    void Draw()
    {

        for (int i = 0; i < path.numSegments; i++)
        {
            Vector2[] points = path.getPointsInSegment(i);
            Handles.color = Color.white;
            Handles.DrawLine(points[1], points[0]);
            Handles.DrawLine(points[3], points[2]);
            Color segmentColor = (i == selectedSegmentIndex && Event.current.shift) ? Color.red : Color.green;
            Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentColor, null, 2);
        }
        Handles.color = Color.red;
        for (int i = 0; i < path.numPoints; i++)
        {
            Vector2 newPos = Handles.FreeMoveHandle(path[i], Quaternion.identity, .1f, Vector2.zero, Handles.CylinderHandleCap);
            if (path[i] != newPos)
            {
                Undo.RecordObject(creator, "Move Point");
                path.MovePoints(i, newPos);
            }
        }
    }
    private void OnEnable()
    {
        creator = (PathCreator)target;
        if (creator.path == null)
        {
            creator.CreatePath();
        }
        path = creator.path;
    }
}
