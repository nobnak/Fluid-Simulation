using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using Random = Unity.Mathematics.Random;


public class Pointers {

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
        if (Input.GetMouseButtonDown(0)) {
            var pos = Input.mousePosition;
            var screenSize = Solver.GetScreenSize();
            var texcoord = new float2(pos.x / screenSize.x, 1 - pos.y / screenSize.y);
            var pointer = pointers.FirstOrDefault(pointers => pointers.id == -1);
            if (pointer == null) {
                pointer = new Pointer();
                pointers.Add(pointer);
            }
            UpdatePointerDownData(pointer, -1, texcoord);
        }
        if (Input.GetMouseButtonUp(0)) {
            var pointer = pointers.FirstOrDefault(pointers => pointers.id == -1);
            if (pointer != null) {
                UpdatePointerUpData(pointer);
            }
        }
        if (Input.GetMouseButton(0)) {
            var pointer = pointers.FirstOrDefault(pointers => pointers.id == -1);
            if (pointer != null) {
                if (!pointer.down) return;
                var pos = Input.mousePosition;
                var screenSize = Solver.GetScreenSize();
                var texcoord = new float2(pos.x / screenSize.x, 1 - pos.y / screenSize.y);
                UpdatePointerMoveData(pointer, texcoord);
            }
        }

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
                pointer.color = Solver.GenerateColor(rand);
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
        pointer.color = Solver.GenerateColor(rand);
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
}