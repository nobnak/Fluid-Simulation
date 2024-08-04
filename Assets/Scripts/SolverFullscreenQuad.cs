using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class SolverFullscreenQuad : SolverMonoBase<SolverFullscreenQuad.Presets> {

    [SerializeField] protected Presets presets = new();
    [SerializeField] protected Config config = new();

    public override int TextureId => P_MainTex;
    public override Presets CurrPresets => presets;
    public override Solver.Config CurrSolverConfig => config.solverConfig;
    protected Pointers pointers;

    #region unity
    protected override void OnEnable() {
        base.OnEnable();
        pointers = new(solver);
        solver.MultipleSplats((int)rand.NextFloat(0f, 20f) + 5);
    }
    protected override void OnDisable() {
        base.OnDisable();
    }
    protected override void Update() {
        var screenSize = GetScreenSize();
        var screenPos = Input.mousePosition;
        var texcoord = new float2(screenPos.x / screenSize.x, screenPos.y / screenSize.y);
        if (Input.GetMouseButtonDown(0)) {
            pointers.ListenMouseDown(texcoord);
        }
        if (Input.GetMouseButton(0)) {
            pointers.ListenMouseMove(texcoord);
        }
        if (Input.GetMouseButtonUp(0)) {
            pointers.ListenMouseUp();
        }
        var dt = GetDeltaTime();
        pointers.Update(dt);

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
        screenSize = GetScreenSize();
        return true;
    }
    protected int2 GetScreenSize() {
        var view = presets.view;
        if (view == null) {
            return new int2(Screen.width, Screen.height);
        } else {
            return new int2(view.pixelWidth, view.pixelHeight);
        }
    }
    #endregion

    #region declarations
    public static readonly int P_MainTex = Shader.PropertyToID("_MainTex");

    [System.Serializable]
    public new class Presets : SolverMonoBase<Presets>.Presets {
        public Camera view;
    }
    [System.Serializable]
    public class Config {
        public Solver.Config solverConfig = new();
        public float z = 10f;
    }
    #endregion

    #region editor
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(SolverFullscreenQuad))]
    public class Editor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            var mono = target as SolverFullscreenQuad;
            var enabled = mono != null && mono.isActiveAndEnabled && Application.isPlaying;

            GUI.enabled = enabled;
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Random splats"))
                    mono.solver.MultipleSplats((int)mono.rand.NextFloat(0f, 20f) + 5);
            }
        }
    }
#endif
#endregion
}