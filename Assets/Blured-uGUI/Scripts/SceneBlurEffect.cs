using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Example.uGUI
{
    /// <summary>
    /// Screen capture using CommandBuffer.
    /// </summary>
    public class SceneBlurEffect : MonoBehaviour
    {
        [SerializeField]
        private Shader _shader;

        [SerializeField, Range(1f, 100f)]
        private float _offset = 1f;

        [SerializeField, Range(10f, 1000f)]
        private float _blur = 100f;

        [SerializeField, Range(0f, 1f)]
        private float _intencity = 0;

        [SerializeField]
        private CameraEvent _cameraEvent = CameraEvent.AfterImageEffects;

        private Material _material;

        private Dictionary<Camera, CommandBuffer> _cameras = new Dictionary<Camera, CommandBuffer>();

        private float[] _weights = new float[10];
        private bool _enabledBlur = false;
        private bool _isInitialized = false;

        public float Intencity
        {
            get { return _intencity; }
            set { _intencity = value; }
        }

        private int _copyTexID = 0;
        private int _blurredID1 = 0;
        private int _blurredID2 = 0;
        private int _weightsID = 0;
        private int _intencityID = 0;
        private int _offsetsID = 0;
        private int _grabBlurTextureID = 0;

        private void Awake()
        {
            // OnWillRenderObjectが呼ばれるように、MeshRendererとMeshFilterを追加する
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.hideFlags = HideFlags.DontSave;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.hideFlags = HideFlags.DontSave;

            _copyTexID = Shader.PropertyToID("_ScreenCopyTexture");
            _blurredID1 = Shader.PropertyToID("_Temp1");
            _blurredID2 = Shader.PropertyToID("_Temp2");
            _weightsID = Shader.PropertyToID("_Weights");
            _intencityID = Shader.PropertyToID("_Intencity");
            _offsetsID = Shader.PropertyToID("_Offsets");
            _grabBlurTextureID = Shader.PropertyToID("_GrabBlurTexture");

            Transform parent = Camera.main.transform;
            transform.SetParent(parent);
            transform.localPosition = Vector3.forward;

            UpdateWeights();
        }

        private void Update()
        {
            foreach (var kv in _cameras)
            {
                kv.Value.Clear();
                BuildCommandBuffer(kv.Value);
            }
        }

        private void OnEnable()
        {
            Cleanup();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        public void OnWillRenderObject()
        {
            if (!gameObject.activeInHierarchy || !enabled)
            {
                Cleanup();
                return;
            }

            if (_material == null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }

            Camera cam = Camera.current;
            if (cam == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (cam == UnityEditor.SceneView.lastActiveSceneView.camera)
            {
                return;
            }
#endif

            if (_cameras.ContainsKey(cam))
            {
                return;
            }

            // コマンドバッファ構築
            CommandBuffer buf = new CommandBuffer();
            buf.name = "Blur AR Screen";
            _cameras[cam] = buf;

            BuildCommandBuffer(buf);

            cam.AddCommandBuffer(_cameraEvent, buf);
        }

        private void BuildCommandBuffer(CommandBuffer buf)
        {
            buf.GetTemporaryRT(_copyTexID, -1, -1, 0, FilterMode.Bilinear);
            buf.Blit(BuiltinRenderTextureType.CurrentActive, _copyTexID);

            // 半分の解像度で2枚のRender Textureを生成
            buf.GetTemporaryRT(_blurredID1, -2, -2, 0, FilterMode.Bilinear);
            buf.GetTemporaryRT(_blurredID2, -2, -2, 0, FilterMode.Bilinear);

            // 半分にスケールダウンしてコピー
            buf.Blit(_copyTexID, _blurredID1);

            // コピー後はいらないので破棄
            buf.ReleaseTemporaryRT(_copyTexID);

            float x = _offset / Screen.width;
            float y = _offset / Screen.height;

            buf.SetGlobalFloatArray(_weightsID, _weights);
            buf.SetGlobalFloat(_intencityID, Intencity);

            // 横方向のブラー
            buf.SetGlobalVector(_offsetsID, new Vector4(x, 0, 0, 0));
            buf.Blit(_blurredID1, _blurredID2, _material);

            // 縦方向のブラー
            buf.SetGlobalVector(_offsetsID, new Vector4(0, y, 0, 0));
            buf.Blit(_blurredID2, _blurredID1, _material);

            buf.SetGlobalTexture(_grabBlurTextureID, _blurredID1);
        }

        private void Cleanup()
        {
            foreach (var cam in _cameras)
            {
                if (cam.Key != null)
                {
                    cam.Key.RemoveCommandBuffer(_cameraEvent, cam.Value);
                }
            }

            _cameras.Clear();
            Object.DestroyImmediate(_material);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UpdateWeights();
        }

        public void EnableBlur(bool enabled)
        {
            _enabledBlur = enabled;
        }

        private void UpdateWeights()
        {
            float total = 0;
            float d = _blur * _blur * 0.001f;

            for (int i = 0; i < _weights.Length; i++)
            {
                float r = 1.0f + 2.0f * i;
                float w = Mathf.Exp(-0.5f * (r * r) / d);
                _weights[i] = w;
                if (i > 0)
                {
                    w *= 2.0f;
                }
                total += w;
            }

            for (int i = 0; i < _weights.Length; i++)
            {
                _weights[i] /= total;
            }
        }
    }
}
