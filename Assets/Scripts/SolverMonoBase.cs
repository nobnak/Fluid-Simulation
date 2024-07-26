using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = Unity.Mathematics.Random;

public abstract class SolverMonoBase<T> : MonoBehaviour 
    where T : SolverMonoBase<T>.Presets {

    protected Solver solver;
    protected RenderTexture target;
    protected RenderTexture debugOutTex;
    protected Random rand;

    public abstract int TextureId { get; }
    public abstract T CurrPresets { get; }
    public abstract Solver.Config CurrSolverConfig { get; }

    #region unity
    protected virtual void OnEnable() {
        solver = new(CurrSolverConfig);
        rand = new Random((uint)GetInstanceID());

        solver.MultipleSplats((int)rand.NextFloat(0f, 20f) + 5);
    }
    protected virtual void OnDisable() {
        if (solver != null) {
            solver.Dispose();
            solver = null;
        }
        DisposeAllTextures();
    }

    protected virtual void Update() {
        var presets = CurrPresets;

        if (TryGetScreenSize(out int2 screenSize)) {
            if (target == null || screenSize.x != target.width || screenSize.y != target.height) {
                DisposeAllTextures();

                var format = DefaultFormat.HDR;
                target = new RenderTexture(screenSize.x, screenSize.y, 0, format);
                target.hideFlags = HideFlags.DontSave;
                target.filterMode = FilterMode.Bilinear;
                target.wrapMode = TextureWrapMode.Clamp;
                target.anisoLevel = 0;
                target.useMipMap = false;
                debugOutTex = new RenderTexture(target.descriptor);
                debugOutTex.hideFlags = HideFlags.DontSave;
            }
        }
        if (solver != null) {
            solver.CurrTarget = target;
            solver.Update();
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
            mat.SetTexture(TextureId, presets.debug == default ? target : debugOutTex);
        }
    }
    #endregion

    #region methods
    protected virtual void DisposeAllTextures() {
        if (target != null) {
            Object.Destroy(target);
            target = null;
        }
        if (debugOutTex != null) {
            Object.Destroy(debugOutTex);
            debugOutTex = null;
        }
    }
    protected abstract bool TryGetScreenSize(out int2 screenSize);
    #endregion

    #region declarations

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
