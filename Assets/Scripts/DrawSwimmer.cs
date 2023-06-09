using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using UnityEngine.VFX;
public class DrawSwimmer : MonoBehaviour
{
    public Image plotImage;
    public Image particleImage;
    public int DIM_X;
    public int DIM_Y;
    public int ParticleDIM_X;
    public int ParticleDIM_Y;
    public float tauf = 0.8f;
    public float u0 = 0.01f;
    public int loopCount = 1;

    public float minTemp = 0f;
    public float maxTemp = 1f;

    public float minSpeed = 0f;
    public float maxSpeed = 1f;
    public bool debugMode = false;
    public bool debugFrame = false;
    public int particleCount;

    public float particleDensity = 1.25f;
    public float particleRadius;
    public float epsw = 100.0f, zeta = 1.0f;
    public float squirmerBeta = 0f;
    public float squirmerSpeedConstant = 0.001f;
    Texture2D plotTexture;
    Texture2D particleTexture;
    RenderTexture renderTexture;
    RenderTexture particleRenderTexture;
    int init,collisions,streaming,boundaries,plotSpeed,immersedBoundary,initRoundParticles,plotParticle,draw;
    ComputeBuffer uv,f,force;
    ComputeBuffer roundParticleSmallDataBuffer;
    ComputeBuffer roundParticleRoundParticlePerimeterPosBuffer;
    ComputeBuffer roundParticleRoundParticlePerimeterVelBuffer;
    ComputeBuffer roundParticleRoundParticlePerimeterFluidVelBuffer;
    ComputeBuffer roundParticleRoundParticleForceOnPerimeterBuffer;
    ComputeBuffer roundParticleInitPosBuffer;
    Vector2[] particleInitPos;

    Vector2[] debugFluidVel;
    RoundParticleSmallData[] debugSmallData;

    public VisualEffect vfx;
    GraphicsBuffer velocityGraphicsBuffer;
    float[] uvBuffer;
    void OnDestroy()
    {
        uv.Dispose();
        f.Dispose();
        force.Dispose();
        roundParticleInitPosBuffer.Dispose();
        roundParticleSmallDataBuffer.Dispose();
        roundParticleRoundParticlePerimeterPosBuffer.Dispose();
        roundParticleRoundParticlePerimeterVelBuffer.Dispose();
        roundParticleRoundParticlePerimeterFluidVelBuffer.Dispose();
        roundParticleRoundParticleForceOnPerimeterBuffer.Dispose();
        velocityGraphicsBuffer.Dispose();
    }
    public ComputeShader compute;

    float umax, umin, tmp, u2, nu, chi, norm, taug, rbetag, h;
    public bool drawMode;
    bool initialized = false;
    bool isTouched;
    // Start is called before the first frame update
    void Start()
    {
        velocityGraphicsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, DIM_X*DIM_Y, sizeof(float)*2);
        vfx.SetGraphicsBuffer("VelocityBuffer",velocityGraphicsBuffer);
        vfx.SetInt("DIM_X",DIM_X);
        vfx.SetInt("DIM_Y",DIM_Y);
        int particlePerimeterCount = (int)(2f * Mathf.PI * particleRadius * 2f);

        debugSmallData = new RoundParticleSmallData[particleCount];
        debugFluidVel = new Vector2[particleCount * particlePerimeterCount];

        plotTexture = new Texture2D(DIM_X,DIM_Y);
        plotTexture.filterMode = FilterMode.Point;
        plotImage.sprite = Sprite.Create(plotTexture, new Rect(0,0,DIM_X,DIM_Y),UnityEngine.Vector2.zero);
        ((RectTransform)plotImage.transform).sizeDelta = new Vector2(DIM_X*1080/DIM_Y,1080);

        particleTexture = new Texture2D(ParticleDIM_X,ParticleDIM_Y);
        // particleTexture.filterMode = FilterMode.Point;
        particleImage.sprite = Sprite.Create(particleTexture, new Rect(0,0,ParticleDIM_X,ParticleDIM_Y),UnityEngine.Vector2.zero);
        ((RectTransform)particleImage.transform).sizeDelta = new Vector2(ParticleDIM_X*1080/ParticleDIM_Y,1080);

        renderTexture = new RenderTexture(DIM_X,DIM_Y,24);
        renderTexture.enableRandomWrite = true;
        particleRenderTexture = new RenderTexture(ParticleDIM_X,ParticleDIM_Y,24);
        particleRenderTexture.enableRandomWrite = true;

        uv = new ComputeBuffer(DIM_X*DIM_Y*2,sizeof(float));
        force = new ComputeBuffer(DIM_X*DIM_Y*2,sizeof(float));
        f = new ComputeBuffer(9*DIM_X*DIM_Y*2,sizeof(float));
        uvBuffer = new float[DIM_X*DIM_Y*2];

        roundParticleSmallDataBuffer = new ComputeBuffer(particleCount,Marshal.SizeOf(typeof(RoundParticleSmallData)));
        roundParticleRoundParticlePerimeterPosBuffer = new ComputeBuffer(particleCount * particlePerimeterCount,sizeof(float) * 2);
        roundParticleRoundParticlePerimeterVelBuffer = new ComputeBuffer(particleCount * particlePerimeterCount,sizeof(float) * 2);
        roundParticleRoundParticlePerimeterFluidVelBuffer = new ComputeBuffer(particleCount * particlePerimeterCount,sizeof(float) * 2);
        roundParticleRoundParticleForceOnPerimeterBuffer = new ComputeBuffer(particleCount * particlePerimeterCount,sizeof(float) * 2);
        roundParticleInitPosBuffer = new ComputeBuffer(particleCount,sizeof(float) * 2);

        immersedBoundary = compute.FindKernel("ImmersedBoundary");
        compute.SetBuffer(immersedBoundary,"uv",uv);
        compute.SetBuffer(immersedBoundary,"force",force);
        compute.SetBuffer(immersedBoundary,"roundParticleSmallDataBuffer",roundParticleSmallDataBuffer);
        compute.SetBuffer(immersedBoundary,"roundParticleRoundParticlePerimeterPosBuffer",roundParticleRoundParticlePerimeterPosBuffer);
        compute.SetBuffer(immersedBoundary,"roundParticleRoundParticlePerimeterVelBuffer",roundParticleRoundParticlePerimeterVelBuffer);
        compute.SetBuffer(immersedBoundary,"roundParticleRoundParticlePerimeterFluidVelBuffer",roundParticleRoundParticlePerimeterFluidVelBuffer);
        compute.SetBuffer(immersedBoundary,"roundParticleRoundParticleForceOnPerimeterBuffer",roundParticleRoundParticleForceOnPerimeterBuffer);
        compute.SetBuffer(immersedBoundary,"particleInitPos",roundParticleInitPosBuffer);

        compute.SetInt("DIM_X",DIM_X);
        compute.SetInt("DIM_Y",DIM_Y);
        compute.SetInt("ParticleDIM_X",ParticleDIM_X);
        compute.SetInt("ParticleDIM_Y",ParticleDIM_Y);
        compute.SetFloat("minSpeed",minSpeed);
        compute.SetFloat("maxSpeed",maxSpeed);
        compute.SetFloat("squirmerSpeedConstant",squirmerSpeedConstant);
        compute.SetFloat("squirmerBeta",squirmerBeta);
        compute.SetFloat("u0",u0);
        compute.SetFloat("tauf",tauf);

        compute.SetInt("particleCount",particleCount);
        compute.SetFloat("particleDensity",particleDensity);
        compute.SetFloat("particleRadius",particleRadius);
        compute.SetInt("particlePerimeterCount",particlePerimeterCount);
        compute.SetFloat("zeta",zeta);
        compute.SetFloat("epsw",epsw);
        particleInitPos = new Vector2[particleCount];
        float dist = 3 * particleRadius; 
        for(int i = 0; i < particleCount; i++)
        {
            // particleInitPos[i] = new Vector2(DIM_X/2,DIM_Y/2);
            particleInitPos[i] = new Vector2(UnityEngine.Random.Range(0f,DIM_X),UnityEngine.Random.Range(0f,DIM_Y));
        }
        roundParticleInitPosBuffer.SetData(particleInitPos);

        initRoundParticles = compute.FindKernel("InitRoundParticles");
        compute.SetBuffer(initRoundParticles,"roundParticleSmallDataBuffer",roundParticleSmallDataBuffer);
        compute.SetBuffer(initRoundParticles,"roundParticleRoundParticlePerimeterPosBuffer",roundParticleRoundParticlePerimeterPosBuffer);
        compute.SetBuffer(initRoundParticles,"roundParticleRoundParticlePerimeterVelBuffer",roundParticleRoundParticlePerimeterVelBuffer);
        compute.SetBuffer(initRoundParticles,"roundParticleRoundParticlePerimeterFluidVelBuffer",roundParticleRoundParticlePerimeterFluidVelBuffer);
        compute.SetBuffer(initRoundParticles,"roundParticleRoundParticleForceOnPerimeterBuffer",roundParticleRoundParticleForceOnPerimeterBuffer);
        compute.SetBuffer(initRoundParticles,"particleInitPos",roundParticleInitPosBuffer);

        init = compute.FindKernel("Init");
        compute.SetBuffer(init,"force",force);
        compute.SetBuffer(init,"uv",uv);
        compute.SetBuffer(init,"f",f);

        plotSpeed = compute.FindKernel("PlotSpeed");
        compute.SetBuffer(plotSpeed,"uv",uv);
        compute.SetBuffer(plotSpeed,"f",f);
        compute.SetTexture(plotSpeed,"renderTexture",renderTexture);

        collisions = compute.FindKernel("Collisions");
        compute.SetBuffer(collisions,"velocityBuffer",velocityGraphicsBuffer);
        compute.SetBuffer(collisions,"f",f);
        compute.SetBuffer(collisions,"uv",uv);
        compute.SetBuffer(collisions,"force",force);

        streaming = compute.FindKernel("Streaming");
        compute.SetBuffer(streaming,"f",f);

        boundaries = compute.FindKernel("Boundaries");
        compute.SetBuffer(boundaries,"f",f);

        plotParticle = compute.FindKernel("PlotParticle");
        compute.SetTexture(plotParticle,"particleRenderTexture",particleRenderTexture);
        compute.SetBuffer(plotParticle,"roundParticleSmallDataBuffer",roundParticleSmallDataBuffer);

        draw = compute.FindKernel("Draw");
        compute.SetTexture(draw,"particleRenderTexture",particleRenderTexture);
    }

    void Init()
    {
        compute.Dispatch(init,(DIM_X+7)/8,(DIM_Y+7)/8,1);
        if(particleCount > 0)compute.Dispatch(initRoundParticles,(particleCount+63)/64,1,1);
        compute.Dispatch(plotSpeed,(DIM_X+7)/8,(DIM_Y+7)/8,1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
        compute.Dispatch(plotParticle,(ParticleDIM_X+7)/8,(ParticleDIM_Y+7)/8,1);
        RenderTexture.active = particleRenderTexture;
        particleTexture.ReadPixels(new Rect(0, 0, particleRenderTexture.width, particleRenderTexture.height), 0, 0);
        particleTexture.Apply();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(drawMode)
        {
            if(Input.GetMouseButton(0))
            {
                Vector2 touchPos = ((Vector2)Input.mousePosition - new Vector2(1080f,1080f)/2f - particleImage.GetComponent<RectTransform>().anchoredPosition)/1080f;
                if(Mathf.Abs(touchPos.x) <= 0.5f && Mathf.Abs(touchPos.y) <= 0.5f)
                {
                    if(!isTouched)
                    {
                        isTouched = true;
                        compute.SetBool("isTouched",isTouched);
                    }
                    compute.SetVector("touchTextureCoord",touchPos);
                    // compute.Dispatch(draw,(ParticleDIM_X+7)/8,(ParticleDIM_Y+7)/8,1);
                    compute.Dispatch(draw,1,1,1);
                    RenderTexture.active = particleRenderTexture;
                    particleTexture.ReadPixels(new Rect(0, 0, particleRenderTexture.width, particleRenderTexture.height), 0, 0);
                    particleTexture.Apply();
                }
                else if(isTouched)
                {
                    isTouched = false;
                    compute.SetBool("isTouched",isTouched);
                }
            }
            return;
        }
        if(!initialized) 
        {
            Init();
            initialized = true;
        }
        if(debugMode)
        {
            debugMode = false;
                roundParticleSmallDataBuffer.GetData(debugSmallData);
                int particleIndex = UnityEngine.Random.Range(0,particleCount);
                print((debugSmallData[particleIndex].radius*debugSmallData[particleIndex].vel)/((2*tauf-1)*6));
        }
        else
        {
            for (int i = 0; i < loopCount; i++)
            {
                compute.Dispatch(collisions,(DIM_X+7)/8,(DIM_Y+7)/8,1);
                compute.Dispatch(streaming,(DIM_X+7)/8,(DIM_Y+7)/8,1);
                if(particleCount > 0)compute.Dispatch(immersedBoundary,(particleCount+63)/64,1,1);
            }
        }

        vfx.SetFloat("VelocityScale",((10f/(float)DIM_Y) * loopCount / Time.deltaTime) * Time.fixedDeltaTime/Time.deltaTime);
        compute.Dispatch(plotSpeed,(DIM_X+7)/8,(DIM_Y+7)/8,1);
        RenderTexture.active = renderTexture;
        plotTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        plotTexture.Apply();
        compute.Dispatch(plotParticle,(ParticleDIM_X+7)/8,(ParticleDIM_Y+7)/8,1);
        RenderTexture.active = particleRenderTexture;
        particleTexture.ReadPixels(new Rect(0, 0, particleRenderTexture.width, particleRenderTexture.height), 0, 0);
        particleTexture.Apply();
    }

    private void OnValidate() 
    {
        compute.SetFloat("minSpeed",minSpeed);
        compute.SetFloat("maxSpeed",maxSpeed);
        compute.SetFloat("u0",u0);
        compute.SetFloat("tauf",tauf);
        compute.SetFloat("epsw",epsw);
        compute.SetFloat("zeta",zeta);
        compute.SetFloat("squirmerBeta",squirmerBeta);
        compute.SetFloat("squirmerSpeedConstant",squirmerSpeedConstant);

    }
}
