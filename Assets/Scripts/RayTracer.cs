using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static UnityEngine.GraphicsBuffer;

[ImageEffectAllowedInSceneView, ExecuteAlways]
public class RayTracer : MonoBehaviour
{
    private Camera _camera;

    [SerializeField] bool enableShader;


    [SerializeField, Range(1, 32)] int maxBounces = 4;
    [SerializeField, Range(1, 200)] int raysPerPixel = 2;

    public ComputeShader shader;
    public RenderTexture renderTexture;
    ComputeBuffer sphereBuffer;

    public Texture skyboxTexture;

    Material prevFrameMaterial;
    int frameNum = 0;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        prevFrameMaterial = new Material(Shader.Find("Hidden/AddPrevFrame"));
    }


    private void Update()
    {
        if(transform.hasChanged)
        {
            frameNum = 0;
            transform.hasChanged = false;
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (enableShader || EditorApplication.isPlaying)
        {
            updateShaderVariables();

            addPrevFrameData(destination);
            //Graphics.Blit(renderTexture, destination);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
    

    private void addPrevFrameData(RenderTexture destination)
    {
        if (renderTexture == null || renderTexture.width != Screen.width || renderTexture.height != Screen.height)
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
            }

            renderTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
        
            
        prevFrameMaterial.SetFloat("_Sample", frameNum);

        Graphics.Blit(renderTexture, destination, prevFrameMaterial);

        if(EditorApplication.isPlaying)
        {
            frameNum++;
        }
    }


    private void GetSpheres()
    {
        GameObject[] sphereObjects = GameObject.FindGameObjectsWithTag("RTSphere");
        Sphere[] spheres = new Sphere[sphereObjects.Length];

        for(int i = 0; i < sphereObjects.Length; i++) 
        {
            spheres[i] = new Sphere()
            {
                pos = sphereObjects[i].transform.position,
                radius = sphereObjects[i].transform.localScale.x * 0.5f,
                material = sphereObjects[i].GetComponent<RayTracedSphere>().material
            };
        }

        sphereBuffer = new ComputeBuffer(sphereObjects.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere)));
        sphereBuffer.SetData(spheres);
        shader.SetBuffer(0, "spheres", sphereBuffer);
        shader.SetInt("NumSpheres", sphereObjects.Length);
    }


    private void updateShaderVariables()
    {
        shader.SetTexture(0, "Result", renderTexture);
        shader.SetInt("RaysPerPixel", raysPerPixel);
        shader.SetInt("MaxBounces", maxBounces);
        shader.SetInt("FrameNum", frameNum);
        shader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
        shader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
        shader.SetTexture(0, "_SkyboxTexture", skyboxTexture);

        //Passing compute buffers
        GetSpheres();

        shader.Dispatch(0, renderTexture.width / 16, renderTexture.height / 16, 1);
    }


    private void OnDisable()
    {
        if (sphereBuffer != null)
            sphereBuffer.Release();
    }
}
