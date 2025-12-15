#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DhRobot))]
public class DhRobotEditor : Editor
{
    float stepLinear = 0.01f;   // 1 cm
    float stepAngularDeg = 5f;  // 5 grados
    float gain = 1.0f;
    float damping = 1e-4f;

    public override void OnInspectorGUI()
    {
        DhRobot robot = (DhRobot)target;
        robot.Init();

        GUILayout.Label("Calibración de juntas", EditorStyles.boldLabel);
        if (GUILayout.Button("Recalibrar cero DESDE escena actual"))
        {
            robot.RecalibrateZeroFromCurrentTransforms();
            MarkDirty(robot);
        }

        GUILayout.Space(10);

        // ----- HOME -----
        EditorGUILayout.LabelField("Posición HOME (grados)", EditorStyles.boldLabel);

        for (int i = 0; i < robot.homeDegrees.Length; i++)
        {
            robot.homeDegrees[i] = EditorGUILayout.FloatField($"J{i+1}", robot.homeDegrees[i]);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Guardar HOME (desde grados)"))
        {
            // aquí NO miramos la pose actual, solo los campos homeDegrees
            robot.SaveHomeFromDegrees();
            MarkDirty(robot);
        }

        if (GUILayout.Button("Guardar HOME (desde pose actual)"))
        {
            robot.SaveHomeFromCurrentQ();  // ya hace SyncQFromTransforms dentro
            MarkDirty(robot);
        }



        if (GUILayout.Button("Ir a HOME"))
        {
            robot.GoToHome();
            MarkDirty(robot);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);


        // ----- El resto del inspector normal (DH, joints, etc.) -----
        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("Jog cartesiano (Editor)", EditorStyles.boldLabel);

        stepLinear      = EditorGUILayout.FloatField("Paso lineal [m]", stepLinear);
        stepAngularDeg  = EditorGUILayout.FloatField("Paso angular [deg]", stepAngularDeg);
        gain            = EditorGUILayout.FloatField("Ganancia IK", gain);
        damping         = EditorGUILayout.FloatField("Amortiguación (lambda)", damping);

        GUILayout.Space(5);

        // ----- TRASLACIONES -----
        GUILayout.BeginHorizontal();
        // X debe actuar como tu eje X real
        if (GUILayout.Button("+X")) Jog(robot, new Vector3(0f, 0f, +stepLinear), Vector3.zero);
        if (GUILayout.Button("-X")) Jog(robot, new Vector3(0f, 0f, -stepLinear), Vector3.zero);
        // Y debe actuar como tu eje Y real
        if (GUILayout.Button("+Y")) Jog(robot, new Vector3(0f, +stepLinear, 0f), Vector3.zero);
        if (GUILayout.Button("-Y")) Jog(robot, new Vector3(0f, -stepLinear, 0f), Vector3.zero);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        // Z debe actuar como tu eje Z real
        if (GUILayout.Button("+Z")) Jog(robot, new Vector3(+stepLinear, 0f, 0f), Vector3.zero);
        if (GUILayout.Button("-Z")) Jog(robot, new Vector3(-stepLinear, 0f, 0f), Vector3.zero);
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // ----- ROTACIONES -----
        float stepAngRad = stepAngularDeg * Mathf.Deg2Rad;

        GUILayout.BeginHorizontal();
        // rX → rotación alrededor de tu eje X real
        if (GUILayout.Button("+rX")) Jog(robot, Vector3.zero, new Vector3(0f, 0f, +stepAngRad));
        if (GUILayout.Button("-rX")) Jog(robot, Vector3.zero, new Vector3(0f, 0f, -stepAngRad));
        // rY → rotación alrededor de tu eje Y real
        if (GUILayout.Button("+rY")) Jog(robot, Vector3.zero, new Vector3(0f, +stepAngRad, 0f));
        if (GUILayout.Button("-rY")) Jog(robot, Vector3.zero, new Vector3(0f, -stepAngRad, 0f));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        // rZ → rotación alrededor de tu eje Z real
        if (GUILayout.Button("+rZ")) Jog(robot, Vector3.zero, new Vector3(+stepAngRad, 0f, 0f));
        if (GUILayout.Button("-rZ")) Jog(robot, Vector3.zero, new Vector3(-stepAngRad, 0f, 0f));
        GUILayout.EndHorizontal();
    }

    void Jog(DhRobot robot, Vector3 dPosWorld, Vector3 dRotWorld)
    {
        if (robot == null) return;

        robot.SyncQFromTransforms();                      // base = lo que veo
        robot.StepCartesianTwistWorld(dPosWorld, dRotWorld, gain, damping);

        MarkDirty(robot);
    }


    void MarkDirty(DhRobot robot)
    {
        EditorUtility.SetDirty(robot);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            robot.gameObject.scene);
    }
}
#endif
