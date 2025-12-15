using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrajectoryRecorder))]
public class TrajectoryRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TrajectoryRecorder recorder = (TrajectoryRecorder)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Añadir punto (efector actual)"))
        {
            recorder.AddCurrentEndEffectorPose();
        }

        if (GUILayout.Button("Borrar todos los puntos"))
        {
            recorder.ClearPoints();
        }
    }
}
