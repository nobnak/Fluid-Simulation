using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
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