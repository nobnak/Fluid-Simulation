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
    protected RenderTexture debugOutTex;

    #region unity
    protected void OnEnable() {
        solver = new(config);

        camAttached = GetComponent<Camera>();

        solver.MultipleSplats(10);
    }
    protected void OnDisable() {
        if (solver != null) {
            solver.Dispose();
            solver = null;
        }
        DisposeAllTextures();
    }

    protected void Update() {
        if (camAttached != null) {
            var screenSize = new int2(camAttached.pixelWidth, camAttached.pixelHeight);
            if (target == null || screenSize.x != target.width || screenSize.y != target.height) {
                DisposeAllTextures();
                target = new RenderTexture(screenSize.x, screenSize.y, 0, DefaultFormat.LDR);
                target.hideFlags = HideFlags.DontSave;
                debugOutTex = new RenderTexture(target.descriptor);
                debugOutTex.hideFlags = HideFlags.DontSave;
            }
        }
        if (solver != null) {
            solver.CurrTarget = target;
            //solver.Update();
            switch (presets.debug) {
                case DebugOutTex.Dye: {
                    Graphics.Blit(solver.dye.Read, debugOutTex);
                    break;
                }
                case DebugOutTex.Velocity: {
                    Graphics.Blit(solver.velocity.Read, debugOutTex);
                    break;
                }
            }
        }
        if (presets.mat != null) {
            var mat = presets.mat;
            mat.SetTexture(P_SourceTex, presets.debug == default ? target : debugOutTex);
        }
    }
    #endregion

    #region methods
    private void DisposeAllTextures() {
        if (target != null) {
            Object.Destroy(target);
            target = null;
        }
        if (debugOutTex != null) {
            Object.Destroy(debugOutTex);
            debugOutTex = null;
        }
    }
    #endregion

    #region declarations
    public static readonly int P_SourceTex = Shader.PropertyToID("_SourceTex");

    public enum DebugOutTex {
        None = 0,
        Dye = 1,
        Velocity = 2,
    }

    [System.Serializable]
    public class Presets {
        public Material mat;
        public DebugOutTex debug;
    }
    #endregion
}
