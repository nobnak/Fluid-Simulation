using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

public class SolverImageEffect : MonoBehaviour {

    [SerializeField] protected Presets presets = new();
    [SerializeField] protected Solver.Config config = new();

    protected Solver solver;
    protected Camera camAttached;
    protected RenderTexture target;

    #region unity
    protected void OnEnable() {
        solver = new(config);

        camAttached = GetComponent<Camera>();
    }
    protected void OnDisable() {
        if (solver != null) {
            solver.Dispose();
            solver = null;
        }
        DisposeTargetTex();
    }

    protected void Update() {
        if (camAttached != null) {
            var screenSize = new int2(camAttached.pixelWidth, camAttached.pixelHeight);
            if (target == null || screenSize.x != target.width || screenSize.y != target.height) {
                DisposeTargetTex();
                target = new RenderTexture(screenSize.x, screenSize.y, 0, DefaultFormat.LDR);
                target.hideFlags = HideFlags.DontSave;
            }
        }
        if (solver != null) {
            solver.CurrTarget = target;
            solver.Update();
        }
        if (presets.mat != null) {
            var mat = presets.mat;
            mat.SetTexture(P_SourceTex, target);
        }
    }
    #endregion

    #region methods
    private void DisposeTargetTex() {
        if (target == null) {
            return;
        }
        Object.Destroy(target);
        target = null;
    }
    #endregion

    #region declarations
    public static readonly int P_SourceTex = Shader.PropertyToID("_SourceTex");
    [System.Serializable]
    public class Presets {
        public Material mat;
    }
    #endregion
}
