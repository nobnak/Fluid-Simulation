using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class SolverQuad : SolverMonoBase<SolverQuad.Presets> {

    [SerializeField] protected Presets presets = new();
    [SerializeField] protected Config config = new();

    public override int TextureId => P_MainTex;
    public override Presets CurrPresets => presets;
    public override Solver.Config CurrSolverConfig => config.solverConfig;

    #region unity
    protected override void Update() {
        var cam = presets.view ?? Camera.main;
        if (cam != null) {
            var p0 = cam.ViewportToWorldPoint(new Vector3(0f, 0f, config.z));
            var p1 = cam.ViewportToWorldPoint(new Vector3(1f, 1f, config.z));
            var size = p1 - p0;

            transform.position = cam.transform.position + cam.transform.forward * config.z;
            transform.rotation = cam.transform.rotation;
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        base.Update();
    }
    #endregion

    #region methods
    protected override bool TryGetScreenSize(out int2 screenSize) {
        if (presets.view != null) {
            screenSize = new int2(Screen.width, Screen.height);
            return true;
        }
        screenSize = default;
        return false;
    }
    #endregion

    #region declarations
    public static readonly int P_MainTex = Shader.PropertyToID("_MainTex");

    [System.Serializable]
    public class Presets : SolverMonoBase<Presets>.Presets {
        public Camera view;
    }
    [System.Serializable]
    public class Config {
        public Solver.Config solverConfig = new();
        public float z = 10f;
    }
    #endregion
}