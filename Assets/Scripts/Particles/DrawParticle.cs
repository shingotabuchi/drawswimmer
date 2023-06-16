using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
public struct DrawParticleSmallData
{
    public float density;
    public float omega;//角速度
    public float theta;//角
    public float prevOmega1;//前フレームの角速度
    public float prevOmega2;//前々フレームの角速度
    public float torque;
    public int perimeterPointCount;
    public float mass;
    public float momentOfInertia;
    public int bottomPointIndex;

    public Vector2 pos;
    public Vector2 vel;
    public Vector2 prevVel1;//前フレームの速度
    public Vector2 prevVel2;//前々フレームの速度
    public Vector2 forceFromCollisions;
    public Vector2 forceFromFluid;
}
