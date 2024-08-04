using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using Random = Unity.Mathematics.Random;


public class Pointers : System.IDisposable {

    protected float colorUpdateTimer;
    protected Random rand;

    public List<Pointer> pointers { get; protected set; } = new List<Pointer>();
    public Solver Solver { get; set; }

    public Pointers(Solver solver, uint seed = 31) {
        Solver = solver;
        colorUpdateTimer = 0f;
        rand = new Random(seed);
    }

    public void Update(float dt) {
        ApplyInputs(); 
        UpdateColors(dt);
    }

    public void SplatPointer(Pointer pointer) {
        float2 dx = pointer.delta * Solver.CurrConfig.SPLAT_FORCE;
        Solver.Splat(pointer.texcoord, dx, pointer.color);
    }
    public void ApplyInputs() {
        foreach (var pointer in pointers) {
            if (pointer.moved) {
                pointer.moved = false;
                SplatPointer(pointer);
            }
        }
    }
    public void UpdateColors(float dt) {
        if (!Solver.CurrConfig.COLORFUL) return;

        colorUpdateTimer += dt * Solver.CurrConfig.COLOR_UPDATE_SPEED;
        if (colorUpdateTimer >= 1) {
            colorUpdateTimer = math.frac(colorUpdateTimer);
            foreach (var pointer in pointers) {
                pointer.color = Solver.GenerateColor(ref rand);
            }
        }
    }
    public void UpdatePointerDownData(Pointer pointer, int id, float2 texcoord) {
        pointer.id = id;
        pointer.down = true;
        pointer.moved = false;
        pointer.texcoord = texcoord;
        pointer.prevTexcoord = pointer.texcoord;
        pointer.delta = float2.zero;
        pointer.color = Solver.GenerateColor(ref rand);
    }
    public void UpdatePointerMoveData(Pointer pointer, float2 texcoord) {
        pointer.prevTexcoord = pointer.texcoord;
        pointer.texcoord = texcoord;
        pointer.delta = pointer.texcoord - pointer.prevTexcoord;
        pointer.moved = math.lengthsq(pointer.delta) > 0;
    }
    public void UpdatePointerUpData(Pointer pointer) {
        pointer.id = -1;
        pointer.down = false;
    }

    #region idisposable
    public void Dispose() {
        pointers.Clear();
    }
    #endregion

    #region listeners
    public void ListenMouseMove(float2 texcoord, int id = -1) {
        var pointer = pointers.FirstOrDefault(pointers => pointers.id == id);
        if (pointer != null) {
            if (!pointer.down) return;
            UpdatePointerMoveData(pointer, texcoord);
        }
    }

    public void ListenMouseUp(int id = -1) {
        var pointer = pointers.FirstOrDefault(pointers => pointers.id == id);
        if (pointer != null) {
            UpdatePointerUpData(pointer);
        }
    }

    public void ListenMouseDown(float2 texcoord, int id = -1) {
        var pointer = pointers.FirstOrDefault(pointers => pointers.id == id);
        if (pointer == null) {
            pointer = new Pointer();
            pointers.Add(pointer);
        }
        UpdatePointerDownData(pointer, -1, texcoord);
    }
    #endregion

    #region declarations
    public class Pointer {

        public int id = -1;
        public float2 texcoord = float2.zero;
        public float2 prevTexcoord = float2.zero;
        public float2 delta = float2.zero;
        public bool down = false;
        public bool moved = false;
        public Color color = new Color(30, 0, 300);

        //function pointerPrototype() {
        //    this.id = -1;
        //    this.texcoordX = 0;
        //    this.texcoordY = 0;
        //    this.prevTexcoordX = 0;
        //    this.prevTexcoordY = 0;
        //    this.deltaX = 0;
        //    this.deltaY = 0;
        //    this.down = false;
        //    this.moved = false;
        //    this.color = [30, 0, 300];
        //}
    }
    #endregion
}