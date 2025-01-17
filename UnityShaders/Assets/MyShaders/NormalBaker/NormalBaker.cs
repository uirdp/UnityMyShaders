using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TextureUtility
{
    public class NormalBaker : ScriptableRendererFeature
    {
        #if UNITY_EDITOR
        #region editor
        [CustomEditor(typeof(NormalBaker))]
        public class NormalBakerEditor : Editor
        {
            
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                NormalBaker baker = target as NormalBaker;
                if (GUILayout.Button("Bake"))
                {
                    baker.Bake();
                }
            }
        }
        #endregion
        #endif
        
        #region render pass
        class NormalBakerPass : ScriptableRenderPass
        {
            private ComputeShader m_normalBaker;
            private string m_kernelName;
            private int m_kernel;

            private RenderTexture m_resultTexture;
            private Texture2D m_srcTexture;
            private Texture2D m_normalMap;
            
            private Vector4 m_vertexToLight;
            private float m_contrast;

            private RenderTargetIdentifier m_resultTextureId;
            private int m_renderTextureWidth;
            private int m_renderTextureHeight;


            public NormalBakerPass(ComputeShader normalBaker, string KernelName,
                NormalBakerSettings normalBakerSettings)
            {
                m_normalBaker = normalBaker;
                m_kernelName = KernelName;
                
                m_srcTexture = normalBakerSettings.texture;
                m_renderTextureWidth = normalBakerSettings.texture.width;
                m_renderTextureHeight = normalBakerSettings.texture.height;
                m_normalMap = normalBakerSettings.normalMap;
                
                UpdateBakeParameters(normalBakerSettings.bakeParameters);
                
                m_resultTextureId = new RenderTargetIdentifier(normalBakerSettings.renderTargetID);

                InitComputeShader();
            }

            private void CreateRenderTexture()
            {
                m_resultTexture = new RenderTexture(m_srcTexture.width, m_srcTexture.height, 0);
                m_resultTexture.enableRandomWrite = true;
                m_resultTexture.Create();

                m_resultTextureId = new RenderTargetIdentifier(m_resultTexture);
            }

            private void InitComputeShader()
            {
                CreateRenderTexture();

                m_kernel = m_normalBaker.FindKernel(m_kernelName);
            }

            public void UpdateBakeParameters(BakeParameters parameters)
            {
                m_vertexToLight = parameters.vertexToLight;
                m_vertexToLight.w = 1;
                m_contrast = parameters.contrast;
            }

            public Texture2D GetTexture2D()
            {
                var tex = TexClip.RT2Tex(m_resultTexture);
                return tex;
            }
            
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (renderingData.cameraData.isSceneViewCamera) return;

                CommandBuffer cmd = CommandBufferPool.Get();

                cmd.SetComputeTextureParam(m_normalBaker, m_kernel, "Result", m_resultTexture);
                cmd.SetComputeTextureParam(m_normalBaker, m_kernel, "Input", m_srcTexture);
                cmd.SetComputeTextureParam(m_normalBaker, m_kernel, "NormalMap", m_normalMap);
                cmd.SetComputeIntParam(m_normalBaker, "_TextureWidth", m_renderTextureWidth);
                cmd.SetComputeIntParam(m_normalBaker, "_TextureHeight", m_renderTextureHeight);
                cmd.SetComputeVectorParam(m_normalBaker, "_VertexToLight", m_vertexToLight);
                cmd.SetComputeFloatParam(m_normalBaker, "_Contrast", m_contrast);
                m_normalBaker.GetKernelThreadGroupSizes(m_kernel, out uint x, out uint y, out _);

                cmd.DispatchCompute(m_normalBaker, m_kernel,
                    Mathf.CeilToInt(m_renderTextureWidth / x),
                    Mathf.CeilToInt(m_renderTextureHeight / y),
                    1);

                cmd.Blit(m_resultTextureId, renderingData.cameraData.renderer.cameraColorTarget);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
            }
        }

        #endregion

        #region render feature

        public struct BakeParameters
        {
            public Vector3 vertexToLight;
            public float contrast;
        }
        public struct NormalBakerSettings
        {
            public int renderTargetID;
            public Texture2D texture;
            public Texture2D normalMap;
            public BakeParameters bakeParameters;
        }

        public Texture2D texture;
        public Texture2D normalMap;
        
        public Vector3 vertexToLight = new Vector3(1, 1, 1);
        [Range(0, 3)] 
        public float contrast = 1.0f;
        
        public ComputeShader normalBaker;
        public string kernelName = "CSMain";
        
        [Header("保存するテクスチャの名前")]
        public string saveTexturePath = "NormalMap";
        
        private NormalBakerPass m_scriptablePass;
        private bool m_isInitialized;
        
        private void PackParameters(ref BakeParameters parameters)
        {
            parameters.vertexToLight = vertexToLight;
            parameters.contrast = contrast;
        }
        private void PackSettings(ref NormalBakerSettings settings)
        {
            settings.renderTargetID = Shader.PropertyToID("Result");
            settings.texture = texture;
            settings.normalMap = normalMap;
            PackParameters(ref settings.bakeParameters);
        }

        public override void Create()
        {
            if (normalBaker == null)
            {
                Debug.LogError("ComputeShader is null");
                m_isInitialized = false;
                return;
            }

            NormalBakerSettings settings = new NormalBakerSettings();
            PackSettings(ref settings);

            m_scriptablePass = new NormalBakerPass(normalBaker, kernelName, settings);
            m_scriptablePass.renderPassEvent = RenderPassEvent.AfterRendering;
            m_isInitialized = true;
        }



        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_isInitialized)
            {
                renderer.EnqueuePass(m_scriptablePass);
            }
        }

        private void OnValidate()
        {
            BakeParameters param = new BakeParameters();
            PackParameters(ref param);
            m_scriptablePass?.UpdateBakeParameters(param);
        }

        private void Bake()
        {
            var tex = m_scriptablePass.GetTexture2D();
            TexClip.SaveTexture(tex, saveTexturePath);
        }

        #endregion

    }
}
