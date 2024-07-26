using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Random = Unity.Mathematics.Random;

public class SolverImageEffect : SolverMonoBase<SolverImageEffect.Presets> {

    [SerializeField] Presets presets = new();
    [SerializeField] Solver.Config solverConfig = new();

    protected Camera camAttached;

    public override int TextureId => P_SourceTex;
    public override Presets CurrPresets => presets;
    public override Solver.Config CurrSolverConfig => solverConfig;

    #region unity
    protected override void OnEnable() {
        camAttached = GetComponent<Camera>();
        base.OnEnable();
        solver.MultipleSplats((int)rand.NextFloat(0f, 20f) + 5);
    }
    protected override void OnDisable() {
        base.OnDisable();
    }

    protected override void Update() {
        base.Update();
    }
    #endregion

    #region methods
    protected override bool TryGetScreenSize(out int2 screenSize) {
        if (camAttached != null) {
            screenSize = new int2(camAttached.pixelWidth, camAttached.pixelHeight);
            return true;
        }
        screenSize = default;
        return false;
    }
    #endregion

    #region declarations
    public static readonly int P_SourceTex = Shader.PropertyToID("_SourceTex");

    [System.Serializable]
    public class Presets : SolverMonoBase<Presets>.Presets {
        public Camera view;
    }
    #endregion
}
