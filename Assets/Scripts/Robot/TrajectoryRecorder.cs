using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda puntos de trayectoria en el frame de la BASE del robot.
/// Cada punto tiene posición local y orientación local del efector final.
/// </summary>
[ExecuteAlways]
public class TrajectoryRecorder : MonoBehaviour
{
    [Serializable]
    public struct PoseSample
    {
        public Vector3 localPosition;   // respecto a robot.transform
        public Quaternion localRotation;   // orientación local, en quaternion
        public bool isTrigger;
    }

    [Header("Disparador")]
    [Tooltip("Si está activo, los nuevos puntos se marcan como disparador.")]
    public bool markNewPointsAsTrigger = false;

    [Header("Referencias")]
    public DhRobot robot;

    [Header("Puntos (frame base del robot)")]
    public List<PoseSample> points = new List<PoseSample>();

    [Header("Visualización")]
    public Color gizmoColor = Color.cyan;
    public float gizmoRadius = 0.02f;

    // --- API llamada desde el botón "Añadir punto (efector actual)" ---

    public void AddCurrentEndEffectorPose()
    {
        if (robot == null)
        {
            Debug.LogWarning("[TrajectoryRecorder] Robot no asignado.");
            return;
        }

        robot.Init();

        Transform baseTf = robot.transform;

        Vector3 eePosWorld = robot.GetEndEffectorPosition();
        Quaternion eeRotWorld = robot.GetEndEffectorRotation();

        Vector3 localPos = baseTf.InverseTransformPoint(eePosWorld);
        Quaternion localRot = Quaternion.Inverse(baseTf.rotation) * eeRotWorld;

        PoseSample s = new PoseSample
        {
            localPosition = localPos,
            localRotation = localRot,
            isTrigger = markNewPointsAsTrigger   //si es disparador
        };

        points.Add(s);

        Debug.Log($"[TrajectoryRecorder] Punto añadido. Trigger={s.isTrigger}");
    }



    public void ClearPoints()
    {
        points.Clear();
    }

    // --- Gizmos en escena ---

    void OnDrawGizmos()
    {
        if (robot == null || points == null || points.Count == 0)
            return;

        Gizmos.color = gizmoColor;

        Transform baseTf = robot.transform;

        // Dibujamos esferas y segmentos en MUNDO
        Vector3 prevWorld = baseTf.TransformPoint(points[0].localPosition);
        Gizmos.DrawSphere(prevWorld, gizmoRadius);

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 pWorld = baseTf.TransformPoint(points[i].localPosition);
            Gizmos.DrawSphere(pWorld, gizmoRadius);
            Gizmos.DrawLine(prevWorld, pWorld);
            prevWorld = pWorld;
        }
    }
}
