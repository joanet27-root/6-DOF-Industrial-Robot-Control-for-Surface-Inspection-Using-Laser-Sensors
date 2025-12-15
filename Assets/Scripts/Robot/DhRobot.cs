using System;
using UnityEngine;

/// <summary>
/// Robot descrito por parámetros DH estándar.
/// Incluye:
///  - Cinemática directa
///  - Jacobiano (3xN y 6xN)
///  - Control resolved-rate por posición
///  - Paso cartesiano completo (posición + rotación)
/// </summary>
public class DhRobot : MonoBehaviour
{
    [Header("Modelo DH (alpha, a, d, theta0)")]
    public float[] alpha = new float[6]
    {
        -Mathf.PI / 2f,
         0f,
         0f,
         Mathf.PI / 2f,
        -Mathf.PI / 2f,
         0f
    };

    public float[] a = new float[6]
    {
        0f,
        0.575f,
        0.575f,
        0f,
        0f,
        0f
    };

    public float[] d = new float[6]
    {
        0.330f,
        0f,
        0f,
        0.275f,
        0.221f,
        0.196f
    };

    // Theta fija (offset DH). En tu tabla: [0, -pi/2, 0, pi/2, 0, 0]
    public float[] theta0 = new float[6]
    {
        0f,
        -Mathf.PI / 2f,
        0f,
        Mathf.PI / 2f,
        0f,
        0f
    };

    [Header("Velocidad de trayectoria")]
    [Tooltip("Factor de velocidad para StepResolvedRateToPoseWorld (1 = normal).")]
    public float trajectorySpeed = 1f;


    [Tooltip("Tipos de junta: R o P, por ejemplo RRRRRR")]
    public string jointTypes = "RRRRRR";

    [Header("Juntas de Unity (en orden DH)")]
    public Transform[] jointTransforms = new Transform[6];

    [Tooltip("Ejes de rotación de cada junta, en el espacio LOCAL del Transform")]
    public Vector3[] jointAxes = new Vector3[6]
    {
        // 1: Y, 2: Z, 3: Z, 4: Z, 5: Y, 6: Z
        new Vector3(0,1,0),
        new Vector3(0,0,1),
        new Vector3(0,0,1),
        new Vector3(0,0,1),
        new Vector3(0,1,0),
        new Vector3(0,0,1)
    };

    [Header("Configuración inicial (q0 en rad/m)")]
    public float[] q0 = new float[6];

    [Header("Posición HOME (grados)")]
    [Tooltip("Ángulos de HOME en grados, uno por junta (R).")]
    public float[] homeDegrees = new float[6];

    // ====== Estado interno ======
    float[] q;                 // variables articulares actuales (rad)
    float[] homeQ;             // HOME en rad
    Quaternion[] jointInitialRot;
    bool initialized = false;

    public int DOF => q != null ? q.Length : 0;

    // -------- INICIALIZACIÓN COMÚN --------

    void Awake()
    {
        Init();
    }
    // void OnEnable() { Init(); }
    // void OnValidate() { Init(); }


    public void Init()
    {
        if (initialized)
            return;

        int n = theta0.Length;
        q = new float[n];
        Array.Copy(q0, q, n);

        jointInitialRot = new Quaternion[jointTransforms.Length];
        for (int i = 0; i < jointTransforms.Length; i++)
        {
            if (jointTransforms[i] != null)
                jointInitialRot[i] = jointTransforms[i].localRotation;
            else
                jointInitialRot[i] = Quaternion.identity;
        }

        // HOME por defecto = q0 (si no se ha definido otra cosa)
        if (homeQ == null || homeQ.Length != n)
        {
            homeQ = new float[n];
            for (int i = 0; i < n; i++)
                homeQ[i] = q0[i];
        }

        ApplyJointsToUnity();
        initialized = true;
    }

    // =====================================================================
    // API pública básica
    // =====================================================================

    public float[] GetQ()
    {
        float[] copy = new float[q.Length];
        Array.Copy(q, copy, q.Length);
        return copy;
    }

    public void SetQ(float[] newQ)
    {
        if (newQ == null || newQ.Length != q.Length)
        {
            Debug.LogError("DhRobot.SetQ: tamaño incorrecto");
            return;
        }
        Array.Copy(newQ, q, q.Length);
        ApplyJointsToUnity();
    }

    public Vector3 GetEndEffectorPosition()
    {
        // Usamos el último joint como efector
        if (jointTransforms != null &&
            jointTransforms.Length > 0 &&
            jointTransforms[jointTransforms.Length - 1] != null)
        {
            return jointTransforms[jointTransforms.Length - 1].position;
        }

        // Fallback: FK DH
        Matrix4x4 T0N = ForwardKinematicsLocal();
        Vector3 pLocal = ExtractPosition(T0N);
        return transform.TransformPoint(pLocal);
    }

    public Quaternion GetEndEffectorRotation()
    {
        if (jointTransforms != null &&
            jointTransforms.Length > 0 &&
            jointTransforms[jointTransforms.Length - 1] != null)
        {
            return jointTransforms[jointTransforms.Length - 1].rotation;
        }

        // Fallback usando la FK DH (por si algún día no tienes último joint)
        Matrix4x4 T0N = ForwardKinematicsLocal();

        // Z = eje forward, Y = up
        Vector3 z = new Vector3(T0N.m02, T0N.m12, T0N.m22);
        Vector3 y = new Vector3(T0N.m01, T0N.m11, T0N.m21);
        Quaternion localRot = Quaternion.LookRotation(z, y);

        return transform.rotation * localRot;
    }




    // ==== HOME ============================================================

    /// <summary>
    /// Guarda la posición HOME a partir de los valores homeDegrees (grados),
    /// actualiza homeQ (rad) y coloca el robot en HOME.
    /// </summary>
    public void SaveHomeFromDegrees()
    {
        Init();

        int n = q.Length;
        if (homeQ == null || homeQ.Length != n)
            homeQ = new float[n];

        for (int i = 0; i < n; i++)
            homeQ[i] = homeDegrees[i] * Mathf.Deg2Rad;

        Array.Copy(homeQ, q, n);
        ApplyJointsToUnity();
    }

    /// <summary>
    /// Define HOME como la configuración articular actual (q).
    /// </summary>
    public void SaveHomeFromCurrentQ()
    {
        Init();

        // MUY IMPORTANTE: q debe corresponder a lo que veo en la escena
        SyncQFromTransforms();

        int n = q.Length;
        if (homeQ == null || homeQ.Length != n)
            homeQ = new float[n];

        // HOME = q actual
        Array.Copy(q, homeQ, n);

        // Actualizamos los grados para que el inspector muestre lo mismo
        for (int i = 0; i < n; i++)
            homeDegrees[i] = homeQ[i] * Mathf.Rad2Deg;
    }


    /// <summary>
    /// Lleva el robot a la posición HOME almacenada.
    /// </summary>
    public void GoToHome()
    {
        Init();

        if (homeQ == null || homeQ.Length != q.Length)
        {
            // Si por lo que sea no está, la reconstruimos desde homeDegrees o q0
            int n = q.Length;
            homeQ = new float[n];
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                if (Mathf.Abs(homeDegrees[i]) > 1e-4f) any = true;
                homeQ[i] = homeDegrees[i] * Mathf.Deg2Rad;
            }
            if (!any)
            {
                for (int i = 0; i < n; i++)
                    homeQ[i] = q0[i];
            }
        }

        Array.Copy(homeQ, q, q.Length);
        ApplyJointsToUnity();
    }

    public void RecalibrateZeroFromCurrentTransforms()
    {
        Init();

        int n = Mathf.Min(jointTransforms.Length, q.Length);

        // 1) La pose actual de la escena pasa a ser el "cero" mecánico
        jointInitialRot = new Quaternion[n];
        for (int i = 0; i < n; i++)
        {
            if (jointTransforms[i] != null)
                jointInitialRot[i] = jointTransforms[i].localRotation;
            else
                jointInitialRot[i] = Quaternion.identity;
        }

        // 2) q y q0 a 0 (porque esta pose es el cero)
        for (int i = 0; i < n; i++)
        {
            q[i] = 0f;
            if (i < q0.Length) q0[i] = 0f;
        }

        // 3) HOME también en cero
        if (homeQ == null || homeQ.Length != n)
            homeQ = new float[n];

        for (int i = 0; i < n; i++)
        {
            homeQ[i] = 0f;
            if (i < homeDegrees.Length)
                homeDegrees[i] = 0f;
        }

        ApplyJointsToUnity();
    }


    /// <summary>
    /// Sincroniza q a partir de las rotaciones actuales de los Transforms
    /// (por si has movido las juntas a mano con el gizmo de Unity).
    /// </summary>
    public void SyncQFromTransforms()
    {
        Init();
        int n = Mathf.Min(q.Length, jointTransforms.Length);

        for (int i = 0; i < n; i++)
        {
            if (jointTransforms[i] == null) continue;

            char jt = char.ToUpper(jointTypes[i]);
            if (jt == 'R')
            {
                Vector3 axis = jointAxes[i].sqrMagnitude < 1e-6f
                    ? Vector3.forward
                    : jointAxes[i].normalized;

                // localRotation = jointInitialRot * rot(axis, q[i])
                Quaternion rel = Quaternion.Inverse(jointInitialRot[i]) * jointTransforms[i].localRotation;

                rel.ToAngleAxis(out float angleDeg, out Vector3 axisRel);

                if (angleDeg > 180f) angleDeg -= 360f;
                if (Vector3.Dot(axisRel, axis) < 0f)
                    angleDeg = -angleDeg;

                q[i] = angleDeg * Mathf.Deg2Rad;
            }
        }
    }



    // =====================================================================
    // Cinemática directa
    // =====================================================================

    public Matrix4x4 ForwardKinematicsLocal()
    {
        int n = q.Length;
        Matrix4x4 T = Matrix4x4.identity;

        for (int i = 0; i < n; i++)
        {
            float th = theta0[i];
            float di = d[i];

            char jt = char.ToUpper(jointTypes[i]);
            if (jt == 'R')
                th += q[i];
            else if (jt == 'P')
                di += q[i];

            Matrix4x4 A = DHToMatrix(alpha[i], a[i], di, th);
            T = T * A;
        }

        return T;
    }

    Matrix4x4 DHToMatrix(float alpha, float a, float d, float theta)
    {
        float ca = Mathf.Cos(alpha);
        float sa = Mathf.Sin(alpha);
        float ct = Mathf.Cos(theta);
        float st = Mathf.Sin(theta);

        Matrix4x4 T = Matrix4x4.identity;

        T.m00 = ct; T.m01 = -st * ca; T.m02 = st * sa; T.m03 = a * ct;
        T.m10 = st; T.m11 = ct * ca; T.m12 = -ct * sa; T.m13 = a * st;
        T.m20 = 0f; T.m21 = sa; T.m22 = ca; T.m23 = d;
        T.m30 = 0f; T.m31 = 0f; T.m32 = 0f; T.m33 = 1f;

        return T;
    }

    // =====================================================================
    // Jacobianos
    // =====================================================================

    // Jacobiano lineal (3xN) calculado a partir de la geometría REAL de Unity
    float[,] JacobianLinearFromUnity()
    {
        int n = q.Length;
        float[,] Jv = new float[3, n];

        if (jointTransforms == null || jointTransforms.Length == 0)
            return Jv;

        Transform baseTf = transform;
        Transform eeTf = jointTransforms[jointTransforms.Length - 1];
        if (eeTf == null) return Jv;

        Vector3 o_e_world = eeTf.position;

        for (int i = 0; i < n; i++)
        {
            Transform jt = jointTransforms[i];
            if (jt == null) continue;

            char jtType = char.ToUpper(jointTypes[i]);

            Vector3 o_i_world = jt.position;

            Vector3 axisLocal = jointAxes[i].sqrMagnitude < 1e-6f
                ? Vector3.forward
                : jointAxes[i].normalized;

            Vector3 z_i_world = jt.TransformDirection(axisLocal);

            if (jtType == 'R')
            {
                Vector3 lin_world = Vector3.Cross(z_i_world, o_e_world - o_i_world);
                Vector3 lin_local = baseTf.InverseTransformVector(lin_world);

                Jv[0, i] = lin_local.x;
                Jv[1, i] = lin_local.y;
                Jv[2, i] = lin_local.z;
            }
            else if (jtType == 'P')
            {
                Vector3 lin_local = baseTf.InverseTransformVector(z_i_world);
                Jv[0, i] = lin_local.x;
                Jv[1, i] = lin_local.y;
                Jv[2, i] = lin_local.z;
            }
        }

        return Jv;
    }


    float[,] JacobianLinear()
    {
        int n = q.Length;
        Matrix4x4[] T0i = new Matrix4x4[n + 1];
        T0i[0] = Matrix4x4.identity;

        for (int i = 0; i < n; i++)
        {
            float th = theta0[i];
            float di = d[i];
            char jt = char.ToUpper(jointTypes[i]);
            if (jt == 'R') th += q[i];
            else if (jt == 'P') di += q[i];

            Matrix4x4 A = DHToMatrix(alpha[i], a[i], di, th);
            T0i[i + 1] = T0i[i] * A;
        }

        Vector3 o_e = ExtractPosition(T0i[n]);
        float[,] Jv = new float[3, n];

        for (int i = 0; i < n; i++)
        {
            Matrix4x4 Ti = T0i[i];
            Vector3 o_im1 = ExtractPosition(Ti);
            Vector3 z_im1 = ExtractZAxis(Ti);

            char jt = char.ToUpper(jointTypes[i]);
            if (jt == 'R')
            {
                Vector3 cross = Vector3.Cross(z_im1, o_e - o_im1);
                Jv[0, i] = cross.x;
                Jv[1, i] = cross.y;
                Jv[2, i] = cross.z;
            }
            else if (jt == 'P')
            {
                Jv[0, i] = z_im1.x;
                Jv[1, i] = z_im1.y;
                Jv[2, i] = z_im1.z;
            }
        }

        return Jv;
    }

    float[,] Jacobian6()
    {
        int n = q.Length;
        Matrix4x4[] T0i = new Matrix4x4[n + 1];
        T0i[0] = Matrix4x4.identity;

        for (int i = 0; i < n; i++)
        {
            float th = theta0[i];
            float di = d[i];
            char jt = char.ToUpper(jointTypes[i]);
            if (jt == 'R') th += q[i];
            else if (jt == 'P') di += q[i];

            Matrix4x4 A = DHToMatrix(alpha[i], a[i], di, th);
            T0i[i + 1] = T0i[i] * A;
        }

        Vector3 o_e = ExtractPosition(T0i[n]);
        float[,] J = new float[6, n];

        for (int i = 0; i < n; i++)
        {
            Matrix4x4 Ti = T0i[i];
            Vector3 o_im1 = ExtractPosition(Ti);
            Vector3 z_im1 = ExtractZAxis(Ti);

            char jt = char.ToUpper(jointTypes[i]);
            if (jt == 'R')
            {
                Vector3 cross = Vector3.Cross(z_im1, o_e - o_im1);
                J[0, i] = cross.x;
                J[1, i] = cross.y;
                J[2, i] = cross.z;
                J[3, i] = z_im1.x;
                J[4, i] = z_im1.y;
                J[5, i] = z_im1.z;
            }
            else if (jt == 'P')
            {
                J[0, i] = z_im1.x;
                J[1, i] = z_im1.y;
                J[2, i] = z_im1.z;
            }
        }

        return J;
    }

    // Jacobiano lineal+angular (6xN) calculado a partir de la geometría REAL de Unity,
    // TODO en coordenadas de MUNDO.
    float[,] Jacobian6FromUnity()
    {
        int n = q.Length;
        float[,] J = new float[6, n];

        if (jointTransforms == null || jointTransforms.Length == 0)
            return J;

        // Efector final
        Transform eeTf = jointTransforms[jointTransforms.Length - 1];
        if (eeTf == null) return J;

        Vector3 o_e_world = eeTf.position;

        for (int i = 0; i < n; i++)
        {
            Transform jt = jointTransforms[i];
            if (jt == null) continue;

            char jtType = char.ToUpper(jointTypes[i]);

            // Origen de la junta y eje en MUNDO
            Vector3 o_i_world = jt.position;

            Vector3 axisLocal = jointAxes[i].sqrMagnitude < 1e-6f
                ? Vector3.forward
                : jointAxes[i].normalized;

            Vector3 z_world = jt.TransformDirection(axisLocal);  // eje z_i en mundo

            if (jtType == 'R')
            {
                // Parte lineal: z × (p_e - p_i) en MUNDO
                Vector3 lin_world = Vector3.Cross(z_world, o_e_world - o_i_world);

                J[0, i] = lin_world.x;
                J[1, i] = lin_world.y;
                J[2, i] = lin_world.z;

                // Parte angular: eje de la junta en MUNDO
                J[3, i] = z_world.x;
                J[4, i] = z_world.y;
                J[5, i] = z_world.z;
            }
            else if (jtType == 'P')
            {
                // (por si algún día usas prismáticas)
                J[0, i] = z_world.x;
                J[1, i] = z_world.y;
                J[2, i] = z_world.z;
                // J[3..5] = 0
            }
        }

        return J;
    }



    // =====================================================================
    // IK / Control resolved-rate
    // =====================================================================

    /// <summary>
    /// Paso de IK (posición + orientación) hacia una pose objetivo en MUNDO.
    /// targetPosWorld: posición objetivo del efector.
    /// targetRotWorld: orientación objetivo del efector.
    /// posGain / rotGain: ganancias para posición y orientación.
    /// </summary>
    public void StepResolvedRateToPoseWorld(Vector3 targetPosWorld,
                                        Quaternion targetRotWorld,
                                        float posGain,
                                        float rotGain,
                                        float dt,
                                        float damping = 1e-4f)
    {
        // Pose actual del efector en MUNDO
        Vector3 eePosWorld = GetEndEffectorPosition();
        Quaternion eeRotWorld = GetEndEffectorRotation();

        // --- Error de posición en MUNDO ---
        Vector3 ePosWorld = targetPosWorld - eePosWorld;

        // --- Error de orientación en MUNDO ---
        // Rotación que lleva la orientación actual a la deseada
        Quaternion qErr = Quaternion.Inverse(eeRotWorld) * targetRotWorld;
        qErr.ToAngleAxis(out float angleDeg, out Vector3 axisWorld);
        if (angleDeg > 180f) angleDeg -= 360f;
        float angleRad = angleDeg * Mathf.Deg2Rad;

        if (axisWorld.sqrMagnitude < 1e-12f)
            axisWorld = Vector3.zero;
        else
            axisWorld.Normalize();

        Vector3 eRotWorld = axisWorld * angleRad;

        // Vector de error 6D en MUNDO
        float[] e6 = new float[6]
        {
        posGain * ePosWorld.x,
        posGain * ePosWorld.y,
        posGain * ePosWorld.z,
        rotGain * eRotWorld.x,
        rotGain * eRotWorld.y,
        rotGain * eRotWorld.z
        };

        // Jacobiano 6xN en MUNDO
        float[,] J = Jacobian6FromUnity();
        int n = q.Length;

        // A = J J^T + λ² I  (6x6)
        float[,] A = new float[6, 6];
        for (int r = 0; r < 6; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                float sum = 0f;
                for (int k = 0; k < n; k++)
                    sum += J[r, k] * J[c, k];
                if (r == c) sum += damping * damping;
                A[r, c] = sum;
            }
        }

        // y = A^-1 * e6
        float[] y = SolveLinearSystem(A, e6, 6);

        // dq = J^T * y
        float[] dq = new float[n];
        for (int i = 0; i < n; i++)
        {
            float sum = 0f;
            for (int r = 0; r < 6; r++)
                sum += J[r, i] * y[r];
            dq[i] = sum;
        }

        // Limitar paso articular (por seguridad numérica)
        float maxStepRad = 30f * Mathf.Deg2Rad;
        for (int i = 0; i < n; i++)
        {
            if (dq[i] > maxStepRad) dq[i] = maxStepRad;
            else if (dq[i] < -maxStepRad) dq[i] = -maxStepRad;
        }

        // Integración (escalada por velocidad de trayectoria)
        float speed = Mathf.Max(trajectorySpeed, 0f);  

        for (int i = 0; i < n; i++)
            q[i] += dq[i] * dt * speed;


        ApplyJointsToUnity();
    }



    public void StepCartesianTwistWorld(Vector3 dPosWorld,
                                        Vector3 dRotWorld,
                                        float gain = 1.0f,
                                        float damping = 1e-4f)
    {
        Vector3 dPosLocal = transform.InverseTransformVector(dPosWorld);
        Vector3 dRotLocal = transform.InverseTransformVector(dRotWorld);

        float[,] J = Jacobian6();
        int n = q.Length;

        float[] e6 = new float[6]
        {
            gain * dPosLocal.x,
            gain * dPosLocal.y,
            gain * dPosLocal.z,
            gain * dRotLocal.x,
            gain * dRotLocal.y,
            gain * dRotLocal.z
        };

        float[,] A = new float[6, 6];
        for (int r = 0; r < 6; r++)
        {
            for (int c = 0; c < 6; c++)
            {
                float sum = 0f;
                for (int k = 0; k < n; k++)
                    sum += J[r, k] * J[c, k];
                if (r == c) sum += damping * damping;
                A[r, c] = sum;
            }
        }

        float[] y = SolveLinearSystem(A, e6, 6);

        float[] dq = new float[n];
        for (int i = 0; i < n; i++)
        {
            float sum = 0f;
            for (int r = 0; r < 6; r++)
                sum += J[r, i] * y[r];
            dq[i] = sum;
        }

        for (int i = 0; i < n; i++)
            q[i] += dq[i];

        ApplyJointsToUnity();
    }

    float[] ComputeDampedLeastSquaresStepPos(float[,] Jv,
                                             Vector3 e,
                                             float gain,
                                             float lambda)
    {
        int n = q.Length;
        float[,] A = new float[3, 3];

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                float sum = 0f;
                for (int k = 0; k < n; k++)
                    sum += Jv[r, k] * Jv[c, k];
                if (r == c) sum += lambda * lambda;
                A[r, c] = sum;
            }
        }

        float[] b = new float[3]
        {
            gain * e.x,
            gain * e.y,
            gain * e.z
        };

        float[] y = SolveLinearSystem(A, b, 3);

        float[] dq = new float[n];
        for (int i = 0; i < n; i++)
            dq[i] = Jv[0, i] * y[0] +
                    Jv[1, i] * y[1] +
                    Jv[2, i] * y[2];

        return dq;
    }

    // =====================================================================
    // Utilidades numéricas
    // =====================================================================

    float[] SolveLinearSystem(float[,] A, float[] b, int n)
    {
        float[,] M = new float[n, n + 1];

        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
                M[i, j] = A[i, j];
            M[i, n] = b[i];
        }

        const float EPS = 1e-9f;

        for (int k = 0; k < n; k++)
        {
            int piv = k;
            float maxVal = Mathf.Abs(M[k, k]);
            for (int i = k + 1; i < n; i++)
            {
                float val = Mathf.Abs(M[i, k]);
                if (val > maxVal)
                {
                    maxVal = val;
                    piv = i;
                }
            }

            if (maxVal < EPS)
                return new float[n];

            if (piv != k)
            {
                for (int j = k; j <= n; j++)
                {
                    float tmp = M[k, j];
                    M[k, j] = M[piv, j];
                    M[piv, j] = tmp;
                }
            }

            float diag = M[k, k];
            for (int j = k; j <= n; j++)
                M[k, j] /= diag;

            for (int i = 0; i < n; i++)
            {
                if (i == k) continue;
                float factor = M[i, k];
                for (int j = k; j <= n; j++)
                    M[i, j] -= factor * M[k, j];
            }
        }

        float[] x = new float[n];
        for (int i = 0; i < n; i++)
            x[i] = M[i, n];

        return x;
    }

    // =====================================================================
    // Aplicación de q a los Transforms
    // =====================================================================

    void ApplyJointsToUnity()
    {
        int n = Mathf.Min(jointTransforms.Length, q.Length);

        for (int i = 0; i < n; i++)
        {
            if (jointTransforms[i] == null) continue;

            char jt = char.ToUpper(jointTypes[i]);
            if (jt == 'R')
            {
                Vector3 axis = jointAxes[i].sqrMagnitude < 1e-6f
                    ? Vector3.forward
                    : jointAxes[i].normalized;

                Quaternion rot = Quaternion.AngleAxis(q[i] * Mathf.Rad2Deg, axis);
                jointTransforms[i].localRotation = jointInitialRot[i] * rot;
            }
        }
    }

    // =====================================================================
    // Helpers para Matrix4x4
    // =====================================================================

    static Vector3 ExtractPosition(Matrix4x4 m)
    {
        return new Vector3(m.m03, m.m13, m.m23);
    }

    static Vector3 ExtractZAxis(Matrix4x4 m)
    {
        return new Vector3(m.m02, m.m12, m.m22);
    }
}
