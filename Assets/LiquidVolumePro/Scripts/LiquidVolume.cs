using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using LiquidVolumeFX.MIConvexHull;
using System.Linq;
using Random = UnityEngine.Random;
using UnityEngine.XR;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LiquidVolumeFX {
    public enum TOPOLOGY {
        Sphere = 0,
        Cylinder = 1,
        Cube = 2,
        Irregular = 10
    }

    public enum DETAIL {
        Simple = 0,
        Default = 10,
        DefaultNoFlask = 11,
        BumpTexture = 20,
        Reflections = 30,
        Smoke = 40,
        Multiple = 50,
        MultipleNoFlask = 51
    }

    public enum BuoyancyEffect {
        Simple = 0,
        PositionOnly = 1,
        PositionAndRotation = 2
    }

    public static class DetailExtensions {

        public static bool isMultiple(this DETAIL detail) {
            return detail == DETAIL.Multiple || detail == DETAIL.MultipleNoFlask;
        }

        public static bool allowsRefraction(this DETAIL detail) {
            return detail != DETAIL.DefaultNoFlask && detail != DETAIL.MultipleNoFlask;
        }

    }


    [ExecuteInEditMode]
    [HelpURL("https://kronnect.com/support")]
    [AddComponentMenu("Effects/Liquid Volume")]
    [DisallowMultipleComponent]
    public partial class LiquidVolume : MonoBehaviour {

        public static bool FORCE_GLES_COMPATIBILITY = false;

        /// <summary>
        /// Max number of layers for multiple style
        /// </summary>
        public const int MAX_LAYERS = 16;

        [Serializable]
        public struct LiquidLayer {
            [HideInInspector] public string layerName;
            public float amount;
            [Tooltip("Density defines the order in which layers stack up. Two or more liquids with same density and miscible set to true will mix.")]
            public float density;
            public Color color;
            [Tooltip("If miscible is set to true, this liquid will mix with any other with same density.")]
            public bool miscible;
            [Range(0, 1)]
            public float murkiness;
            public Color murkColor;
            [Range(0.001f, 0.48f)]
            public float scale;

            [Range(0, 1), Tooltip("The viscosity determines the influence of global turbulence to this liquid. A value of 0 means sticky while a value of 1 means a very fluid substance.")]
            public float viscosity;

            [Range(0.001f, 10)]
            public float adjustmentSpeed;

            [Range(0, 1), Tooltip("Bubbles opacity.")]
            public float bubblesOpacity;

            [NonSerialized]
            public float mixedAmount, mixedScale, mixedMurkiness;
            [NonSerialized]
            public Color mixedColor;
            [NonSerialized]
            public Color mixedColor2;
            [NonSerialized]
            public float mixedBubblesOpacity;
            [HideInInspector]
            public float baseLevel;
            [HideInInspector]
            public float currentBaseLevel;
            [HideInInspector]
            public float currentAmount;
            [HideInInspector]
            public Color currentColor;
            [HideInInspector]
            public float currentMurkiness;
            [HideInInspector]
            public Color currentColor2;
            [HideInInspector]
            public float currentBubblesOpacity;

            public void SetDefaults() {
                amount = 0.1f;
                density = 1f;
                color = new Color(Random.value, Random.value, Random.value, 0.5f);
                murkColor = new Color(0, 0, 0, 1f);
                murkiness = 0.5f;
                scale = 0.3f;
                adjustmentSpeed = 1f;
                viscosity = 1f;
                bubblesOpacity = 1f;
            }

            public void ResetCurrentStates() {
                currentColor = color;
                currentColor2 = murkColor;
                currentMurkiness = murkiness;
                currentAmount = amount;
                currentBaseLevel = baseLevel;
                currentBubblesOpacity = bubblesOpacity;
            }

        }


        [SerializeField]
        TOPOLOGY _topology = TOPOLOGY.Sphere;

        public TOPOLOGY topology {
            get { return _topology; }
            set {
                if (_topology != value) {
                    if (_topology == TOPOLOGY.Irregular)
                        CleanupCommandBuffer();
                    _topology = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        DETAIL _detail = DETAIL.Default;

        public DETAIL detail {
            get { return _detail; }
            set {
                if (_detail != value) {
                    _detail = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        float _level = 0.5f;

        public float level {
            get { return _level; }
            set {
                if (_level != value) {
                    _level = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 1)]
        float _levelMultiplier = 1f;

        public float levelMultiplier {
            get { return _levelMultiplier; }
            set {
                if (_levelMultiplier != value) {
                    _levelMultiplier = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        LiquidLayer[] _liquidLayers;

        public LiquidLayer[] liquidLayers {
            get { return _liquidLayers; }
            set {
                if (_liquidLayers != value) {
                    _liquidLayers = value;
                    lastLayerCount = 0;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 16)]
        int _smoothContactSurface = 3;

        public int smoothContactSurface {
            get { return _smoothContactSurface; }
            set {
                if (_smoothContactSurface != value) {
                    _smoothContactSurface = value;
                    requireLayersUpdate = true;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 1)]
        float _ditherStrength = 0.5f;

        public float ditherStrength {
            get { return _ditherStrength; }
            set {
                if (_ditherStrength != value) {
                    _ditherStrength = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0.001f, 10)]
        float _layersAdjustmentSpeed = 1f;

        public float layersAdjustmentSpeed {
            get { return _layersAdjustmentSpeed; }
            set {
                if (_layersAdjustmentSpeed != value) {
                    _layersAdjustmentSpeed = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        bool _layersAdjustmentCompact = true;

        public bool layersAdjustmentCompact {
            get { return _layersAdjustmentCompact; }
            set {
                if (_layersAdjustmentCompact != value) {
                    _layersAdjustmentCompact = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [ColorUsage(true)]
        Color _liquidColor1 = new Color(0, 1, 0, 0.1f);

        public Color liquidColor1 {
            get { return _liquidColor1; }
            set {
                if (_liquidColor1 != value) {
                    _liquidColor1 = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0.1f, 4.85f)]
        float _liquidScale1 = 1f;

        public float liquidScale1 {
            get { return _liquidScale1; }
            set {
                if (_liquidScale1 != value) {
                    _liquidScale1 = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [ColorUsage(true)]
        Color _liquidColor2 = new Color(1, 0, 0, 0.3f);

        public Color liquidColor2 {
            get { return _liquidColor2; }
            set {
                if (_liquidColor2 != value) {
                    _liquidColor2 = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(2f, 4.85f)]
        float _liquidScale2 = 5f;

        public float liquidScale2 {
            get { return _liquidScale2; }
            set {
                if (_liquidScale2 != value) {
                    _liquidScale2 = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        float _alpha = 1f;

        public float alpha {
            get { return _alpha; }
            set {
                if (_alpha != Mathf.Clamp01(value)) {
                    _alpha = Mathf.Clamp01(value);
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [ColorUsage(true)]
        Color _emissionColor = new Color(0, 0, 0);

        public Color emissionColor {
            get { return _emissionColor; }
            set {
                if (_emissionColor != value) {
                    _emissionColor = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 8f)]
        float _emissionBrightness;

        public float emissionBrightness {
            get { return _emissionBrightness; }
            set {
                if (_emissionBrightness != value) {
                    _emissionBrightness = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        bool _ditherShadows = true;

        public bool ditherShadows {
            get { return _ditherShadows; }
            set {
                if (_ditherShadows != value) {
                    _ditherShadows = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        float _murkiness = 1.0f;

        public float murkiness {
            get { return _murkiness; }
            set {
                if (_murkiness != value) {
                    _murkiness = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1f)]
        float _turbulence1 = 0.5f;

        public float turbulence1 {
            get { return _turbulence1; }
            set {
                if (_turbulence1 != value) {
                    _turbulence1 = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1f)]
        float _turbulence2 = 0.2f;

        public float turbulence2 {
            get { return _turbulence2; }
            set {
                if (_turbulence2 != value) {
                    _turbulence2 = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        float _frecuency = 1f;

        public float frecuency {
            get { return _frecuency; }
            set {
                if (_frecuency != value) {
                    _frecuency = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0f, 2f)]
        float _speed = 1f;

        public float speed {
            get { return _speed; }
            set {
                if (_speed != value) {
                    _speed = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 5f)]
        float _sparklingIntensity = 0.1f;

        public float sparklingIntensity {
            get { return _sparklingIntensity; }
            set {
                if (_sparklingIntensity != value) {
                    _sparklingIntensity = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        float _sparklingAmount = 0.2f;

        public float sparklingAmount {
            get { return _sparklingAmount; }
            set {
                if (_sparklingAmount != value) {
                    _sparklingAmount = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 10)]
        float _deepObscurance = 2.0f;

        public float deepObscurance {
            get { return _deepObscurance; }
            set {
                if (_deepObscurance != value) {
                    _deepObscurance = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [ColorUsage(true)]
        Color _foamColor = new Color(1, 1, 1, 0.65f);

        public Color foamColor {
            get { return _foamColor; }
            set {
                if (_foamColor != value) {
                    _foamColor = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0.01f, 1f)]
        float _foamScale = 0.2f;

        public float foamScale {
            get { return _foamScale; }
            set {
                if (_foamScale != value) {
                    _foamScale = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 0.1f)]
        float _foamThickness = 0.04f;

        public float foamThickness {
            get { return _foamThickness; }
            set {
                if (_foamThickness != value) {
                    _foamThickness = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(-1, 1)]
        float _foamDensity = 0.5f;

        public float foamDensity {
            get { return _foamDensity; }
            set {
                if (_foamDensity != value) {
                    _foamDensity = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(4, 100)]
        float _foamWeight = 10f;

        public float foamWeight {
            get { return _foamWeight; }
            set {
                if (_foamWeight != value) {
                    _foamWeight = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        float _foamTurbulence = 1f;

        public float foamTurbulence {
            get { return _foamTurbulence; }
            set {
                if (_foamTurbulence != value) {
                    _foamTurbulence = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        bool _foamVisibleFromBottom = true;

        public bool foamVisibleFromBottom {
            get { return _foamVisibleFromBottom; }
            set {
                if (_foamVisibleFromBottom != value) {
                    _foamVisibleFromBottom = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        bool _smokeEnabled = true;

        public bool smokeEnabled {
            get { return _smokeEnabled; }
            set {
                if (_smokeEnabled != value) {
                    _smokeEnabled = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [ColorUsage(true)]
        [SerializeField]
        Color _smokeColor = new Color(0.7f, 0.7f, 0.7f, 0.25f);

        public Color smokeColor {
            get { return _smokeColor; }
            set {
                if (_smokeColor != value) {
                    _smokeColor = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0.01f, 1f)]
        float _smokeScale = 0.25f;

        public float smokeScale {
            get { return _smokeScale; }
            set {
                if (_smokeScale != value) {
                    _smokeScale = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 10f)]
        float _smokeBaseObscurance = 2.0f;

        public float smokeBaseObscurance {
            get { return _smokeBaseObscurance; }
            set {
                if (_smokeBaseObscurance != value) {
                    _smokeBaseObscurance = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 10f)]
        float _smokeHeightAtten;

        public float smokeHeightAtten {
            get { return _smokeHeightAtten; }
            set {
                if (_smokeHeightAtten != value) {
                    _smokeHeightAtten = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 20f)]
        float _smokeSpeed = 5.0f;

        public float smokeSpeed {
            get { return _smokeSpeed; }
            set {
                if (_smokeSpeed != value) {
                    _smokeSpeed = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        bool _fixMesh;

        public bool fixMesh {
            get { return _fixMesh; }
            set {
                if (_fixMesh != value) {
                    _fixMesh = value;
                    UpdateMaterialProperties();
                }
            }
        }

        public Mesh originalMesh;
        public Vector3 originalPivotOffset;

        [SerializeField]
        Vector3 _pivotOffset;

        public Vector3 pivotOffset {
            get { return _pivotOffset; }
            set {
                if (_pivotOffset != value) {
                    _pivotOffset = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        bool _autoCloseMesh;

        public bool autoCloseMesh {
            get { return _autoCloseMesh; }
            set {
                if (_autoCloseMesh != value) {
                    _autoCloseMesh = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 1.5f)]
        float _upperLimit = 1.5f;

        public float upperLimit {
            get { return _upperLimit; }
            set {
                if (_upperLimit != value) {
                    _upperLimit = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(-1.5f, 1.5f)]
        float _lowerLimit = -1.5f;

        public float lowerLimit {
            get { return _lowerLimit; }
            set {
                if (_lowerLimit != value) {
                    _lowerLimit = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        int _subMeshIndex = -1;

        public int subMeshIndex {
            get { return _subMeshIndex; }
            set {
                if (_subMeshIndex != value) {
                    _subMeshIndex = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [ColorUsage(true)]
        Color _flaskColor = new Color(1, 1, 1, 0);

        public Color flaskColor {
            get { return _flaskColor; }
            set {
                if (_flaskColor != value) {
                    _flaskColor = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [ColorUsage(true)]
        Color _flaskTint = new Color(0, 0, 0, 1);

        public Color flaskTint {
            get { return _flaskTint; }
            set {
                if (_flaskTint != value) {
                    _flaskTint = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        float _flaskThickness = 0.03f;

        public float flaskThickness {
            get { return _flaskThickness; }
            set {
                if (_flaskThickness != value) {
                    _flaskThickness = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        float _flaskGlossinessExternal = 0.767f;

        public float flaskGlossinessExternal {
            get { return _flaskGlossinessExternal; }
            set {
                if (_flaskGlossinessExternal != value) {
                    _flaskGlossinessExternal = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        float _flaskGlossinessInternal = 0.5f;

        public float flaskGlossinessInternal {
            get { return _flaskGlossinessInternal; }
            set {
                if (_flaskGlossinessInternal != value) {
                    _flaskGlossinessInternal = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        bool _scatteringEnabled = false;

        public bool scatteringEnabled {
            get { return _scatteringEnabled; }
            set {
                if (_scatteringEnabled != value) {
                    _scatteringEnabled = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(1, 16)]
        int _scatteringPower = 5;

        public int scatteringPower {
            get { return _scatteringPower; }
            set {
                if (_scatteringPower != value) {
                    _scatteringPower = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 10)]
        float _scatteringAmount = 0.3f;

        public float scatteringAmount {
            get { return _scatteringAmount; }
            set {
                if (_scatteringAmount != value) {
                    _scatteringAmount = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        bool _refractionBlur = true;

        public bool refractionBlur {
            get { return _refractionBlur; }
            set {
                if (_refractionBlur != value) {
                    _refractionBlur = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1)]
        float _blurIntensity = 0.75f;

        public float blurIntensity {
            get { return _blurIntensity; }
            set {
                if (_blurIntensity != Mathf.Clamp01(value)) {
                    _blurIntensity = Mathf.Clamp01(value);
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        int _liquidRaySteps = 10;

        public int liquidRaySteps {
            get { return _liquidRaySteps; }
            set {
                if (_liquidRaySteps != value) {
                    _liquidRaySteps = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        int _foamRaySteps = 7;

        public int foamRaySteps {
            get { return _foamRaySteps; }
            set {
                if (_foamRaySteps != value) {
                    _foamRaySteps = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        int _smokeRaySteps = 5;

        public int smokeRaySteps {
            get { return _smokeRaySteps; }
            set {
                if (_smokeRaySteps != value) {
                    _smokeRaySteps = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        Texture2D _bumpMap;

        public Texture2D bumpMap {
            get { return _bumpMap; }
            set {
                if (_bumpMap != value) {
                    _bumpMap = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 1f)]
        float _bumpStrength = 1f;

        public float bumpStrength {
            get { return _bumpStrength; }
            set {
                if (_bumpStrength != value) {
                    _bumpStrength = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 10f)]
        float _bumpDistortionScale = 1f;

        public float bumpDistortionScale {
            get { return _bumpDistortionScale; }
            set {
                if (_bumpDistortionScale != value) {
                    _bumpDistortionScale = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        Vector2 _bumpDistortionOffset;

        public Vector2 bumpDistortionOffset {
            get { return _bumpDistortionOffset; }
            set {
                if (_bumpDistortionOffset != value) {
                    _bumpDistortionOffset = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        Texture2D _distortionMap;

        public Texture2D distortionMap {
            get { return _distortionMap; }
            set {
                if (_distortionMap != value) {
                    _distortionMap = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        Texture2D _texture;

        public Texture2D texture {
            get { return _texture; }
            set {
                if (_texture != value) {
                    _texture = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        Vector2
            _textureScale = Vector2.one;

        public Vector2 textureScale {
            get { return _textureScale; }
            set {
                if (_textureScale != value) {
                    _textureScale = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        Vector2
            _textureOffset;

        public Vector2 textureOffset {
            get { return _textureOffset; }
            set {
                if (_textureOffset != value) {
                    _textureOffset = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0, 10f)]
        float
            _distortionAmount = 1f;

        public float distortionAmount {
            get { return _distortionAmount; }
            set {
                if (_distortionAmount != value) {
                    _distortionAmount = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        bool _depthAware;

        public bool depthAware {
            get { return _depthAware; }
            set {
                if (_depthAware != value) {
                    _depthAware = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        float _depthAwareOffset;

        public float depthAwareOffset {
            get { return _depthAwareOffset; }
            set {
                if (_depthAwareOffset != value) {
                    _depthAwareOffset = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        bool _irregularDepthDebug = false;

        public bool irregularDepthDebug {
            get { return _irregularDepthDebug; }
            set {
                if (_irregularDepthDebug != value) {
                    _irregularDepthDebug = value;
                    UpdateMaterialProperties();
                }
            }
        }



        [SerializeField]
        bool _depthAwareCustomPass;

        public bool depthAwareCustomPass {
            get { return _depthAwareCustomPass; }
            set {
                if (_depthAwareCustomPass != value) {
                    _depthAwareCustomPass = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        bool _depthAwareCustomPassDebug = false;

        public bool depthAwareCustomPassDebug {
            get { return _depthAwareCustomPassDebug; }
            set {
                if (_depthAwareCustomPassDebug != value) {
                    _depthAwareCustomPassDebug = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 5f)]
        float _doubleSidedBias = 0.001f;

        public float doubleSidedBias {
            get { return _doubleSidedBias; }
            set {
                if (_doubleSidedBias != value) {
                    _doubleSidedBias = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        float _backDepthBias;

        public float backDepthBias {
            get { return _backDepthBias; }
            set {
                if (value < 0) value = 0;
                if (_backDepthBias != value) {
                    _backDepthBias = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 1f)]
        float _rotationLevelBias;

        public float rotationLevelBias {
            get { return _rotationLevelBias; }
            set {
                if (_rotationLevelBias != value) {
                    _rotationLevelBias = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        bool _ignoreGravity;

        public bool ignoreGravity {
            get { return _ignoreGravity; }
            set {
                if (_ignoreGravity != value) {
                    _ignoreGravity = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        bool _reactToForces;

        public bool reactToForces {
            get { return _reactToForces; }
            set {
                if (_reactToForces != value) {
                    _reactToForces = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        [Range(0, 1f)]
        float _textureAlpha = 1f;

        public float textureAlpha {
            get { return _textureAlpha; }
            set {
                if (_textureAlpha != value) {
                    _textureAlpha = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        Vector3 _extentsScale = Vector3.one;

        public Vector3 extentsScale {
            get { return _extentsScale; }
            set {
                if (_extentsScale != value) {
                    _extentsScale = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(1, 3)]
        int _noiseVariation = 1;

        public int noiseVariation {
            get { return _noiseVariation; }
            set {
                if (_noiseVariation != value) {
                    _noiseVariation = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        bool _allowViewFromInside = false;

        public bool allowViewFromInside {
            get { return _allowViewFromInside; }
            set {
                if (_allowViewFromInside != value) {
                    _allowViewFromInside = value;
                    lastDistanceToCam = -1;
                    CheckInsideOut();
                }
            }
        }


        [SerializeField]
        bool
            _debugSpillPoint = false;

        public bool debugSpillPoint {
            get { return _debugSpillPoint; }
            set {
                if (_debugSpillPoint != value) {
                    _debugSpillPoint = value;
                }
            }
        }

        [SerializeField]
        int
            _renderQueue = 3001;

        public int renderQueue {
            get { return _renderQueue; }
            set {
                if (_renderQueue != value) {
                    _renderQueue = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        Cubemap _reflectionTexture;

        public Cubemap reflectionTexture {
            get { return _reflectionTexture; }
            set {
                if (_reflectionTexture != value) {
                    _reflectionTexture = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0.1f, 5f)]
        float _physicsMass = 1f;

        public float physicsMass {
            get { return _physicsMass; }
            set {
                if (_physicsMass != value) {
                    _physicsMass = value;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        [Range(0.0f, 0.2f)]
        float _physicsAngularDamp = 0.02f;

        public float physicsAngularDamp {
            get { return _physicsAngularDamp; }
            set {
                if (_physicsAngularDamp != value) {
                    _physicsAngularDamp = value;
                    UpdateMaterialProperties();
                }
            }
        }

        #region Bubbles properties

        [SerializeField]
        int _bubblesAmount = 32;

        public int bubblesAmount {
            get { return _bubblesAmount; }
            set {
                if (_bubblesAmount != value) {
                    _bubblesAmount = value;
                    requireBubblesUpdate = true;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField, Range(1, 16)]
        int _bubblesSizeMin = 2;

        public int bubblesSizeMin {
            get { return _bubblesSizeMin; }
            set {
                if (_bubblesSizeMin != value) {
                    _bubblesSizeMin = value;
                    requireBubblesUpdate = true;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField, Range(1, 16)]
        int _bubblesSizeMax = 4;

        public int bubblesSizeMax {
            get { return _bubblesSizeMax; }
            set {
                if (_bubblesSizeMax != value) {
                    _bubblesSizeMax = value;
                    requireBubblesUpdate = true;
                    UpdateMaterialProperties();
                }
            }
        }

        [SerializeField]
        float _bubblesScale = 3f;

        public float bubblesScale {
            get { return _bubblesScale; }
            set {
                if (_bubblesScale != value) {
                    _bubblesScale = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        float _bubblesBrightness = 1;

        public float bubblesBrightness {
            get { return _bubblesBrightness; }
            set {
                if (_bubblesBrightness != value) {
                    _bubblesBrightness = value;
                    UpdateMaterialProperties();
                }
            }
        }


        [SerializeField]
        float _bubblesVerticalSpeed = 1;

        public float bubblesVerticalSpeed {
            get { return _bubblesVerticalSpeed; }
            set {
                if (_bubblesVerticalSpeed != value) {
                    _bubblesVerticalSpeed = value;
                    UpdateMaterialProperties();
                }
            }
        }



        [SerializeField]
        int _bubblesSeed = 1;

        public int bubblesSeed {
            get { return _bubblesSeed; }
            set {
                if (_bubblesSeed != value) {
                    _bubblesSeed = value;
                    requireBubblesUpdate = true;
                    UpdateMaterialProperties();
                }
            }
        }



        #endregion


        #region Point Light support

        [Serializable]
        public struct PointLightParams {
            [HideInInspector]
            public string name;
            [NonSerialized]
            public Vector3 localPosition;
            [NonSerialized]
            public Vector3 accel;
            [NonSerialized]
            public float nextTargetTime;

            public Vector3 targetPos;
            public bool randomTarget;
            public Vector3 randomTargetCenter;
            public float randomTargetRadius;
            public float nextTargetInterval;
            public float moveSpeed;
            public float lightRange;
            public float intensityMin, intensityMax;
            public float intensitySpeed;
            public Color color1, color2;
            public float colorSpeed;

        }


        [SerializeField]
        bool _pointLightsEnabled;

        public bool pointLightsEnabled {
            get { return _pointLightsEnabled; }
            set {
                if (_pointLightsEnabled != value) {
                    _pointLightsEnabled = value;
                    UpdateMaterialProperties();
                }
            }
        }

        public PointLightParams[] pointLightParams;

        public float pointLightsScatteringAmount = 0.8f;
        public float pointLightsIntensity = 1f;
        public float pointLightInsideAtten;


        #endregion

        public static bool useFPRenderTextures = true;
#if UNITY_EDITOR
        static int fpRenderTextureCheckFrame = -1;
#endif

        // ---- INTERNAL CODE ----
        const int SHADER_KEYWORD_DEPTH_AWARE_INDEX = 0;
        const int SHADER_KEYWORD_DEPTH_AWARE_CUSTOM_PASS_INDEX = 1;
        const int SHADER_KEYWORD_IGNORE_GRAVITY_INDEX = 2;
        const int SHADER_KEYWORD_NON_AABB_INDEX = 3;
        const int SHADER_KEYWORD_TOPOLOGY_INDEX = 4;

        const string SHADER_KEYWORD_DEPTH_AWARE = "LIQUID_VOLUME_DEPTH_AWARE";
        const string SHADER_KEYWORD_DEPTH_AWARE_CUSTOM_PASS = "LIQUID_VOLUME_DEPTH_AWARE_PASS";
        const string SHADER_KEYWORD_NON_AABB = "LIQUID_VOLUME_NON_AABB";
        const string SHADER_KEYWORD_IGNORE_GRAVITY = "LIQUID_VOLUME_IGNORE_GRAVITY";
        const string SHADER_KEYWORD_SPHERE = "LIQUID_VOLUME_SPHERE";
        const string SHADER_KEYWORD_CUBE = "LIQUID_VOLUME_CUBE";
        const string SHADER_KEYWORD_CYLINDER = "LIQUID_VOLUME_CYLINDER";
        const string SHADER_KEYWORD_IRREGULAR = "LIQUID_VOLUME_IRREGULAR";
        const string SHADER_KEYWORD_FP_RENDER_TEXTURE = "LIQUID_VOLUME_FP_RENDER_TEXTURES";
        const string SPILL_POINT_GIZMO = "SpillPointGizmo";

        Material liqMat;
        Material liqMatSimple, liqMatDefault, liqMatDefaultNoFlask, liqMatBump, liqMatReflections, liqMatSmoke, liqMatMultiple, liqMatMultipleNoFlask;
        Mesh mesh;
        Renderer mr;
        static List<Material> mrSharedMaterials = new List<Material>();
        bool wasRefractionBlur, wasBackBuffer, wasFrontBuffer;
        Vector3 lastPosition, lastScale;
        Quaternion lastRotation;
        string[] shaderKeywords;
        bool camInside;
        float lastDistanceToCam;
        DETAIL currentDetail;
        Vector4 turb, shaderTurb;
        float turbulenceSpeed, murkinessSpeed, bubblesVerticalSpeedAcum;
        float liquidLevelPos;
        bool shouldUpdateMaterialProperties;
        int currentNoiseVariation;
        float levelMultipled;

        // Multiple layers
        Color[] layersTextureColors, layersTextureColors2, layersTextureColorsTemp;
        Vector4[] layersProperties;
        Texture2D layersPropertiesTex, layersColorsTex, layersColors2Tex;
        Texture2D noise3DUnwrapped;
        Texture3D[] noise3DTex;
        Color[][] colors3D, colors3DWithBubbles;
        Color[] layersColorsTexContents, layersColors2TexContents;
        Color[] layersPropertiesTexContents;

        float[] layersSum;
        [NonSerialized]
        public bool requireLayersUpdate;
        [NonSerialized]
        public bool requireBubblesUpdate;
        int[] sortedLayers;
        int sortedLayersCount;
        int lastLayerCount;
        bool immediate;

        // Mesh info
        Vector3[] verticesUnsorted, verticesSorted;
        static Vector3[] rotatedVertices;
        int[] verticesIndices;
        float volumeRef, lastLevelVolumeRef;

        // Physics
        Vector3 prevVelocity, prev2Velocity, inertia, lastAvgVelocity;
        float angularVelocity, angularInertia;
        float turbulenceDueForces;
        Quaternion liquidRot;

        float prevThickness;

        // Spill point debug
        GameObject spillPointGizmo;
        static string[] defaultContainerNames = {
            "GLASS",
            "CONTAINER",
            "BOTTLE",
            "POTION",
            "FLASK",
            "LIQUID"
        };

        // Point light effects
        Color[] pointLightColorBuffer;
        Vector4[] pointLightPositionBuffer;
        int lastPointLightCount;


        #region Gameloop events


        void OnEnable() {
            if (!gameObject.activeInHierarchy)
                return;
            levelMultipled = _level * _levelMultiplier;
            turb.z = 1f;
            turbulenceDueForces = 0f;
            turbulenceSpeed = 1f;
            liquidRot = transform.rotation;
            currentDetail = _detail;
            currentNoiseVariation = -1;
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastScale = transform.localScale;
            prevThickness = _flaskThickness;
            if (_depthAwareCustomPass && transform.parent == null) {
                _depthAwareCustomPass = false;
            }
            CheckFPRenderTextureSetting();
            InitLayers();
            RefreshMaterialProperties();
            PointLightsInitialize();
            if (!Application.isPlaying) {
                if (_detail.isMultiple()) {
                    requireLayersUpdate = true;
                }
                shouldUpdateMaterialProperties = true;
            }
        }

        void Reset() {
            // Try to assign propert topology based on mesh
            if (mesh == null)
                return;

            if (mesh.vertexCount == 24) {
                topology = TOPOLOGY.Cube;
            } else {
                Renderer renderer = GetComponent<Renderer>();
                if (renderer == null) {
                    if (mesh.bounds.extents.y > mesh.bounds.extents.x) {
                        topology = TOPOLOGY.Cylinder;
                    }
                } else if (renderer.bounds.extents.y > renderer.bounds.extents.x) {
                    topology = TOPOLOGY.Cylinder;
                    if (!Application.isPlaying) {
                        if (transform.rotation.eulerAngles != Vector3.zero && (mesh.bounds.extents.y <= mesh.bounds.extents.x || mesh.bounds.extents.y <= mesh.bounds.extents.z)) {
                            Debug.LogWarning("Intrinsic model rotation detected. Consider using the Bake Transform and/or Center Pivot options in Advanced section.");
                        }
                    }

                }
            }
        }


        void OnDestroy() {
            CleanupCommandBuffer();

            RestoreOriginalMesh();

            liqMat = null;
            if (liqMatDefault != null) {
                DestroyImmediate(liqMatDefault);
                liqMatDefault = null;
            }
            if (liqMatDefaultNoFlask != null) {
                DestroyImmediate(liqMatDefaultNoFlask);
                liqMatDefaultNoFlask = null;
            }
            if (liqMatSimple != null) {
                DestroyImmediate(liqMatSimple);
                liqMatSimple = null;
            }
            if (liqMatBump != null) {
                DestroyImmediate(liqMatBump);
                liqMatBump = null;
            }
            if (liqMatReflections != null) {
                DestroyImmediate(liqMatReflections);
                liqMatReflections = null;
            }

            if (noise3DTex != null) {
                for (int k = 0; k < noise3DTex.Length; k++) {
                    Texture3D tex = noise3DTex[k];
                    if (tex != null && tex.name.Contains("Clone")) {
                        DestroyImmediate(tex);
                        noise3DTex[k] = null;
                    }
                }
            }

            Resources.UnloadUnusedAssets();
        }


        void RenderObject() {
            var act = gameObject.activeInHierarchy && enabled;

            if (shouldUpdateMaterialProperties || !Application.isPlaying) {
                shouldUpdateMaterialProperties = false;
                RefreshMaterialProperties();
            }

            if (act && _depthAware) {
                Camera.current.depthTextureMode |= DepthTextureMode.Depth;
            }

            if (act && _allowViewFromInside) {
                CheckInsideOut();
            }


            bool useRefraction = _refractionBlur && _detail.allowsRefraction();
            if (!act || (!useRefraction && wasRefractionBlur)) {
                CleanupRefractionBuffer();
                wasRefractionBlur = false;
            } else if (useRefraction) {
                SetupRefractionBuffer();
                wasRefractionBlur = true;
            }

            if (_detail.isMultiple()) {
                AnimateLayers();
            }

            UpdateAnimations();

            if (_pointLightsEnabled) {
                UpdateLightEffects();
            }

            if (!act || (_topology != TOPOLOGY.Irregular && wasBackBuffer)) {
                CleanupBackFacesBuffer();
                wasBackBuffer = false;
            } else if (_topology == TOPOLOGY.Irregular) {
                SetupBackFacesBuffer(GetComponent<Renderer>());
                wasBackBuffer = true;
            }

            if (!act || (!_depthAwareCustomPass && wasFrontBuffer)) {
                CleanupFrontFacesBuffer();
                wasFrontBuffer = false;
            } else if (_depthAwareCustomPass) {
                Transform parent = transform.parent;
                if (parent != null) {
                    Renderer parentRenderer = parent.GetComponent<Renderer>();
                    if (parentRenderer != null) {
                        SetupFrontFacesBuffer(parentRenderer);
                        wasFrontBuffer = true;
                    }
                }
            }
            if (_debugSpillPoint) {
                UpdateSpillPointGizmo();
            }

#if UNITY_EDITOR
            // Since Unity materials do not support color/vector arrays we need to submit them everytime to avoid losing the uniform values inside Unity Editor
            if (_detail.isMultiple())
                UploadMaterialArrays();
#endif
        }

        public void OnWillRenderObject() {
            RenderObject();
            liqMat.SetVector(ShaderParams.CustomWorldSpaceCameraPos, Camera.current.transform.position);
        }

        void FixedUpdate() {
            turbulenceSpeed += Time.deltaTime * 3f * _speed;
            liqMat.SetFloat(ShaderParams.TurbulenceSpeed, turbulenceSpeed * 4f);
            murkinessSpeed += Time.deltaTime * 0.05f * (shaderTurb.x + shaderTurb.y);
            liqMat.SetFloat(ShaderParams.MurkinessSpeed, murkinessSpeed);
            bubblesVerticalSpeedAcum += Time.deltaTime * _bubblesVerticalSpeed;
            if (_detail.isMultiple()) {
                UpdateBubblesProperties();
            }
        }

        void UpdateBubblesProperties() {
            if (_bubblesBrightness < 0)
                _bubblesBrightness = 0;
            liqMat.SetVector(ShaderParams.BubblesData, new Vector4(100f / (1f + _bubblesScale), bubblesVerticalSpeedAcum, _bubblesBrightness, 0));
        }


        void OnDidApplyAnimationProperties() {  // support for animating property based fields
            shouldUpdateMaterialProperties = true;
        }

        void OnDisable() {
            CleanupCommandBuffer();
        }

        #endregion

        #region Internal stuff

        struct MeshCache {
            public Vector3[] verticesSorted;
            public Vector3[] verticesUnsorted;
            public int[] indices;
        }

        static readonly Dictionary<Mesh, MeshCache> meshCache = new Dictionary<Mesh, MeshCache>();

        public void ClearMeshCache() {
            meshCache.Clear();
        }

        void ReadVertices() {
            if (mesh == null)
                return;

            if (!meshCache.TryGetValue(mesh, out MeshCache meshData)) {
                if (!mesh.isReadable) {
                    Debug.LogError("Mesh " + mesh.name + " is not readable. Please select your mesh and enable the Read/Write Enabled option.");
                }

                verticesUnsorted = mesh.vertices;
                verticesIndices = mesh.triangles;

                int vertexCount = verticesUnsorted.Length;

                if (verticesSorted == null || verticesSorted.Length != vertexCount) {
                    verticesSorted = new Vector3[vertexCount];
                }
                Array.Copy(verticesUnsorted, verticesSorted, vertexCount);
                Array.Sort(verticesSorted, vertexComparer);

                meshData.verticesUnsorted = verticesUnsorted;
                meshData.indices = verticesIndices;
                meshData.verticesSorted = verticesSorted;

                if (meshCache.Count > 64) {
                    // Clear cache to avoid memory overrun
                    ClearMeshCache();
                }
                meshCache[mesh] = meshData;
            } else {
                verticesUnsorted = meshData.verticesUnsorted;
                verticesIndices = meshData.indices;
                verticesSorted = meshData.verticesSorted;
            }
        }

        int vertexComparer(Vector3 v0, Vector3 v1) {
            if (v1.y < v0.y) return -1;
            if (v1.y > v0.y) return 1;
            return 0;
        }


        void UpdateAnimations() {
            // Check proper scale
            switch (topology) {
                case TOPOLOGY.Sphere:
                    if (transform.localScale.y != transform.localScale.x || transform.localScale.z != transform.localScale.x)
                        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.x, transform.localScale.x);
                    break;
                case TOPOLOGY.Cylinder:
                    if (transform.localScale.z != transform.localScale.x)
                        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, transform.localScale.x);
                    break;
            }


            if (liqMat != null) {
                Vector3 turbDir = Vector3.right;
                Quaternion rot = transform.rotation;
                if (_reactToForces) {
                    Quaternion instantRot = transform.rotation;
                    float dt = Time.smoothDeltaTime;
                    if (Application.isPlaying && dt > 0) {
                        Vector3 instantVelocity = (transform.position - lastPosition) / dt;
                        Vector3 avgVelocity = (prev2Velocity + prevVelocity + instantVelocity) / 3f;
                        prev2Velocity = prevVelocity;
                        prevVelocity = instantVelocity;

                        Vector3 instantAccel = (avgVelocity - lastAvgVelocity);
                        lastAvgVelocity = avgVelocity;
                        inertia += avgVelocity;

                        float accelMag = instantAccel.magnitude;
                        float force = Mathf.Max(accelMag / _physicsMass - _physicsAngularDamp, 0f);
                        angularInertia += force;
                        angularVelocity += angularInertia;
                        if (angularVelocity > 0) {
                            angularInertia -= Mathf.Abs(angularVelocity) * _physicsMass / 100f;
                        } else if (angularVelocity < 0) {
                            angularInertia += Mathf.Abs(angularVelocity) * _physicsMass / 100f;
                        }
                        float damp = 1f - _physicsAngularDamp;
                        angularInertia *= damp;
                        inertia *= damp;

                        float mag = Mathf.Clamp(angularVelocity, -90f, 90f);
                        if (inertia.x != 0 || inertia.z != 0) {
                            turbDir = inertia.normalized;
                        }
                        Vector3 axis = Vector3.Cross(turbDir, Vector3.down);
                        instantRot = Quaternion.AngleAxis(mag, axis);

                        float cinematic = Mathf.Abs(angularInertia) + Mathf.Abs(angularVelocity);
                        turbulenceDueForces = Mathf.Min(0.5f / _physicsMass, turbulenceDueForces + cinematic / 1000f);
                        turbulenceDueForces *= damp;
                    } else {
                        turbulenceDueForces = 0;
                    }

                    if (_topology == TOPOLOGY.Sphere) {
                        liquidRot = Quaternion.Lerp(liquidRot, instantRot, 0.1f);
                        rot = liquidRot;
                    }
                } else if (turbulenceDueForces > 0) {
                    turbulenceDueForces *= 0.1f;
                }
                Matrix4x4 m = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
                liqMat.SetMatrix(ShaderParams.RotationMatrix, m.inverse);
                if (_topology != TOPOLOGY.Sphere) {
                    float tx = turbDir.x;
                    turbDir.x += (turbDir.z - turbDir.x) * 0.25f;
                    turbDir.z += (tx - turbDir.z) * 0.25f;
                }
                turb.z = turbDir.x;
                turb.w = turbDir.z;
            }

            bool hasRotated = transform.rotation != lastRotation;
            if (_reactToForces || hasRotated || transform.position != lastPosition || transform.localScale != lastScale) {
                UpdateLevels(hasRotated);
            }
        }


        public void PointLightsRandomize() {
            PointLightsInitialize(true);
        }

        void PointLightsInitialize(bool randomize = false) {
            if (pointLightParams == null) {
                pointLightParams = new PointLightParams[6];
                randomize = true;
            } else if (pointLightParams.Length > 6) {
                Array.Resize<PointLightParams>(ref pointLightParams, 6);
            }

            if (!_pointLightsEnabled) {
                UpdateLightEffects();
                return;
            }
            if (randomize) {
                for (int k = 0; k < pointLightParams.Length; k++) {
                    pointLightParams[k].localPosition = Vector3.zero;
                    pointLightParams[k].randomTargetCenter = new Vector3(0, -0.5f + _level * 0.5f, 0);
                    pointLightParams[k].color1 = new Color(Random.value, Random.value, Random.value);
                    pointLightParams[k].color2 = new Color(Random.value, Random.value, Random.value);
                    pointLightParams[k].colorSpeed = Random.Range(0.5f, 2f);
                    pointLightParams[k].intensityMin = Random.Range(0.5f, 1.5f);
                    pointLightParams[k].intensityMax = pointLightParams[k].intensityMin + Random.value * 1.5f;
                    pointLightParams[k].intensitySpeed = Random.Range(0.5f, 2f);
                    pointLightParams[k].lightRange = Random.Range(0.01f, 0.1f);
                    pointLightParams[k].moveSpeed = Random.value * 0.1f;
                    pointLightParams[k].accel = Vector3.zero;
                    pointLightParams[k].nextTargetInterval = 1f;
                    pointLightParams[k].randomTarget = true;
                    if (pointLightParams[k].randomTargetRadius == 0)
                        pointLightParams[k].randomTargetRadius = 0.4f;
                    SetNewLightTargetPos(k);
                }
            }
        }

        void SetNewLightTargetPos(int k) {
            pointLightParams[k].targetPos = pointLightParams[k].randomTargetCenter + Random.insideUnitSphere * pointLightParams[k].randomTargetRadius;
            pointLightParams[k].targetPos.x = Mathf.Clamp(pointLightParams[k].targetPos.x, -0.4f, 0.4f);
            pointLightParams[k].targetPos.y = Mathf.Clamp(pointLightParams[k].targetPos.y, -0.4f, 0.4f);
            pointLightParams[k].targetPos.z = Mathf.Clamp(pointLightParams[k].targetPos.z, -0.4f, 0.4f);
            pointLightParams[k].nextTargetTime = Time.time + Random.value + pointLightParams[k].nextTargetInterval;
        }

        void UpdateLightEffects() {
#if UNITY_EDITOR
            if (lastPointLightCount != pointLightParams.Length) {
                lastPointLightCount = pointLightParams.Length;
            }
            for (int k = 0; k < pointLightParams.Length; k++) {
                pointLightParams[k].name = "Light " + (k + 1);
            }
#endif
            // Animate lights
            for (int k = 0; k < pointLightParams.Length; k++) {
                Vector3 curPos = pointLightParams[k].localPosition;
                if (pointLightParams[k].randomTarget && Time.time > pointLightParams[k].nextTargetTime) {
                    SetNewLightTargetPos(k);
                }
                Vector3 targetPos = pointLightParams[k].targetPos;
                Vector3 accel = pointLightParams[k].accel;
                float speed = pointLightParams[k].moveSpeed * Time.deltaTime;
                accel.x += curPos.x < targetPos.x ? speed : -speed;
                const float maxAccel = 5f;
                if (accel.x > speed * maxAccel)
                    accel.x = speed * maxAccel;
                else if (accel.x < speed * -maxAccel) {
                    accel.x = speed * -maxAccel;
                }
                accel.y += curPos.y < targetPos.y ? speed : -speed;
                if (accel.y > speed * maxAccel)
                    accel.y = speed * maxAccel;
                else if (accel.y < speed * -maxAccel) {
                    accel.y = speed * -maxAccel;
                }
                accel.z += curPos.z < targetPos.z ? speed : -speed;
                if (accel.z > speed * maxAccel)
                    accel.z = speed * maxAccel;
                else if (accel.z < speed * -maxAccel) {
                    accel.z = speed * -maxAccel;
                }
                pointLightParams[k].accel = accel;
                curPos += pointLightParams[k].accel;
                if (curPos.x < -0.5) {
                    curPos.x = -0.5f;
                    pointLightParams[k].accel.x = 0;
                } else if (curPos.x > 0.5f) {
                    curPos.x = 0.5f;
                    pointLightParams[k].accel.x = 0;
                }
                if (curPos.y < -0.5) {
                    curPos.y = -0.5f;
                    pointLightParams[k].accel.y = 0;
                } else if (curPos.y > 0.5f) {
                    curPos.y = 0.5f;
                    pointLightParams[k].accel.y = 0;
                }
                if (curPos.z < -0.5) {
                    curPos.z = -0.5f;
                    pointLightParams[k].accel.z = 0;
                } else if (curPos.z > 0.5f) {
                    curPos.z = 0.5f;
                    pointLightParams[k].accel.z = 0;
                }
                pointLightParams[k].localPosition = curPos;
            }

            // Pass data to shader
            if (pointLightColorBuffer == null || pointLightColorBuffer.Length != 6) {
                pointLightColorBuffer = new Color[6];
            }
            if (pointLightPositionBuffer == null || pointLightPositionBuffer.Length != 6) {
                pointLightPositionBuffer = new Vector4[6];
            }
            float range, multiplier;
            for (int k = 0; k < 6; k++) {
                range = multiplier = 0;
                if (_pointLightsEnabled && k < pointLightParams.Length) {
                    Vector3 pos = transform.TransformPoint(pointLightParams[k].localPosition);
                    pointLightPositionBuffer[k] = pos;
                    range = pointLightParams[k].lightRange * pointLightsScatteringAmount / 25f; // note: 25 comes from Unity point light attenuation equation
                    float intensity = pointLightParams[k].intensityMin + Mathf.PingPong(Time.time * pointLightParams[k].intensitySpeed, pointLightParams[k].intensityMax - pointLightParams[k].intensityMin);
                    multiplier = pointLightsIntensity * intensity;
                }
                if (range > 0 && multiplier > 0) {
                    // Apply attenuation if light is affected by fog distance & falloff
                    Color pointLightColor = Color.Lerp(pointLightParams[k].color1, pointLightParams[k].color2, Mathf.PingPong(Time.time * pointLightParams[k].colorSpeed, 1f));
                    pointLightColorBuffer[k] = new Vector4(pointLightColor.r * multiplier, pointLightColor.g * multiplier, pointLightColor.b * multiplier, range);
                } else {
                    pointLightColorBuffer[k] = Color.black;
                }
            }

            if (liqMat != null) {
                liqMat.SetFloat(ShaderParams.PointLightInsideAtten, pointLightInsideAtten);
                liqMat.SetColorArray(ShaderParams.PointLightColorArray, pointLightColorBuffer);
                liqMat.SetVectorArray(ShaderParams.PointLightPositionArray, pointLightPositionBuffer);
            }

        }

        public void UpdateMaterialProperties() {
            if (Application.isPlaying) {
                shouldUpdateMaterialProperties = true;
            } else {
                RefreshMaterialProperties();
            }

        }

        void RefreshMaterialProperties() {
            if (!gameObject.activeInHierarchy)
                return;

            switch (_detail) {
                case DETAIL.Simple:
                    if (liqMatSimple == null) {
                        liqMatSimple = Instantiate(Resources.Load<Material>("Materials/LiquidVolumeSimple")) as Material;
                    }
                    liqMat = liqMatSimple;
                    break;
                case DETAIL.DefaultNoFlask:
                    if (liqMatDefaultNoFlask == null) {
                        liqMatDefaultNoFlask = Instantiate(Resources.Load<Material>("Materials/LiquidVolumeDefaultNoFlask")) as Material;
                    }
                    liqMat = liqMatDefaultNoFlask;
                    break;
                case DETAIL.BumpTexture:
                    if (liqMatBump == null) {
                        liqMatBump = Instantiate(Resources.Load<Material>("Materials/LiquidVolumeBump")) as Material;
                    }
                    liqMat = liqMatBump;
                    break;
                case DETAIL.Reflections:
                    if (liqMatReflections == null) {
                        liqMatReflections = Instantiate(Resources.Load<Material>("Materials/LiquidVolumeReflections")) as Material;
                    }
                    liqMat = liqMatReflections;
                    break;
                case DETAIL.Smoke:
                    if (liqMatSmoke == null) {
                        liqMatSmoke = Instantiate(Resources.Load<Material>("Materials/LiquidVolumeSmoke")) as Material;
                    }
                    liqMat = liqMatSmoke;
                    break;
                case DETAIL.Multiple:
                    if (liqMatMultiple == null) {
                        liqMatMultiple = Instantiate(Resources.Load<Material>("Materials/LiquidVolumeMultiple")) as Material;
                    }
                    liqMat = liqMatMultiple;
                    break;
                case DETAIL.MultipleNoFlask:
                    if (liqMatMultipleNoFlask == null) {
                        liqMatMultipleNoFlask = Instantiate(Resources.Load<Material>("Materials/LiquidVolumeMultipleNoFlask")) as Material;
                    }
                    liqMat = liqMatMultipleNoFlask;
                    break;
                default:
                    if (liqMatDefault == null) {
                        liqMatDefault = Instantiate(Resources.Load<Material>("Materials/LiquidVolumeDefault")) as Material;
                    }
                    liqMat = liqMatDefault;
                    break;
            }

            if (liqMat == null)
                return;

            CheckMeshDisplacement();

            if (currentDetail != _detail) {
                currentDetail = _detail;
                if (_detail == DETAIL.Reflections) {
                    _flaskTint = Color.white;
                    _flaskGlossinessExternal = 0.1f;
                } else {
                    _flaskTint = new Color(0, 0, 0, 1f);
                }
                requireLayersUpdate = true;
            }

            if (requireLayersUpdate && _detail.isMultiple()) {
                UpdateLayersNow();
            }

            UpdateLevels();
            if (mr == null)
                return;

            // Try to compute submesh index heuristically if this is the first time the liquid has been added to a multi-material mesh
            mr.GetSharedMaterials(mrSharedMaterials);
            int sharedMaterialsCount = mrSharedMaterials.Count;
            if (_subMeshIndex < 0) {
                for (int w = 0; w < defaultContainerNames.Length; w++) {
                    if (_subMeshIndex >= 0)
                        break;
                    for (int k = 0; k < sharedMaterialsCount; k++) {
                        if (mrSharedMaterials[k] != null && mrSharedMaterials[k].name.ToUpper().Contains(defaultContainerNames[w])) {
                            _subMeshIndex = k;
                            break;
                        }
                    }
                }
            }
            if (_subMeshIndex < 0)
                _subMeshIndex = 0;

            if (sharedMaterialsCount > 1 && _subMeshIndex >= 0 && _subMeshIndex < sharedMaterialsCount) {
                mrSharedMaterials[_subMeshIndex] = liqMat;
                mr.sharedMaterials = mrSharedMaterials.ToArray();
            } else {
                mr.sharedMaterial = liqMat;
            }

            liqMat.SetFloat(ShaderParams.DitherStrength, _ditherStrength);
            liqMat.SetColor(ShaderParams.Color1, ApplyGlobalAlpha(_liquidColor1));
            liqMat.SetColor(ShaderParams.Color2, ApplyGlobalAlpha(_liquidColor2));
            liqMat.SetColor(ShaderParams.EmissionColor, _emissionColor * _emissionBrightness);

            liqMat.SetFloat(ShaderParams.Glossiness, _flaskGlossinessExternal);
            int scatteringPower = _scatteringPower;
            float scatteringAmount = _scatteringAmount;
            if (!_scatteringEnabled) {
                scatteringPower = 0;
                scatteringAmount = 0;
            }
            liqMat.SetVector(ShaderParams.GlossinessInt, new Vector4(_flaskGlossinessInternal * 96f + 1f, Mathf.Pow(2, scatteringPower), scatteringAmount, 0));
            liqMat.SetFloat(ShaderParams.DoubleSidedBias, _doubleSidedBias);
            liqMat.SetFloat(ShaderParams.BackDepthBias, -_backDepthBias);

            liqMat.SetFloat(ShaderParams.Muddy, _murkiness);
            liqMat.SetFloat(ShaderParams.Alpha, _alpha);

            float alphaCombined = _alpha * Mathf.Clamp01((_liquidColor1.a + _liquidColor2.a) * 4f);
            if (_ditherShadows) {
                liqMat.SetFloat(ShaderParams.AlphaCombined, alphaCombined);
            } else {
                liqMat.SetFloat(ShaderParams.AlphaCombined, alphaCombined > 0 ? 1000f : 0f);
            }

            liqMat.SetFloat(ShaderParams.SparklingIntensity, _sparklingIntensity * 250.0f);
            liqMat.SetFloat(ShaderParams.SparklingThreshold, 1.0f - _sparklingAmount);
            liqMat.SetFloat(ShaderParams.DepthAtten, _deepObscurance);
            Color smokeColor = ApplyGlobalAlpha(_smokeColor);
            int smokeRaySteps = _smokeRaySteps;
            if (!_smokeEnabled) {
                smokeColor.a = 0;
                smokeRaySteps = 1;
            }
            liqMat.SetColor(ShaderParams.SmokeColor, smokeColor);
            liqMat.SetFloat(ShaderParams.SmokeAtten, _smokeBaseObscurance);
            liqMat.SetFloat(ShaderParams.SmokeHeightAtten, _smokeHeightAtten);
            liqMat.SetFloat(ShaderParams.SmokeSpeed, _smokeSpeed);
            liqMat.SetFloat(ShaderParams.SmokeRaySteps, smokeRaySteps);

            liqMat.SetFloat(ShaderParams.LiquidRaySteps, _liquidRaySteps);
            liqMat.SetFloat(ShaderParams.FlaskBlurIntensity, _blurIntensity * (_refractionBlur ? 1f : 0f));
            liqMat.SetColor(ShaderParams.FlaskColor, _flaskColor);
            liqMat.SetColor(ShaderParams.FlaskTint, _flaskTint * _flaskTint.a);

            liqMat.SetColor(ShaderParams.FoamColor, ApplyGlobalAlpha(_foamColor));
            liqMat.SetFloat(ShaderParams.FoamRaySteps, _foamThickness > 0 ? _foamRaySteps : 1);
            liqMat.SetFloat(ShaderParams.FoamDensity, _foamThickness > 0 ? _foamDensity : -1f);
            liqMat.SetFloat(ShaderParams.FoamWeight, _foamWeight);
            liqMat.SetFloat(ShaderParams.FoamBottom, _foamVisibleFromBottom ? 1f : 0f);
            liqMat.SetFloat(ShaderParams.FoamTurbulence, _foamTurbulence);

            liqMat.SetFloat(ShaderParams.FlaskTexAlpha, _textureAlpha);

            switch (_detail) {
                case DETAIL.BumpTexture:
                    liqMat.SetTexture(ShaderParams.FlaskBumpMap, _bumpMap);
                    liqMat.SetFloat(ShaderParams.FlaskBumpStrength, _bumpStrength);
                    liqMat.SetTextureScale(ShaderParams.FlaskBumpMap, Vector2.one * _bumpDistortionScale);
                    liqMat.SetTextureOffset(ShaderParams.FlaskBumpMap, _bumpDistortionOffset);
                    liqMat.SetTexture(ShaderParams.FlaskDispMap, _distortionMap);
                    liqMat.SetTextureScale(ShaderParams.FlaskDispMap, Vector2.one * _bumpDistortionScale);
                    liqMat.SetTextureOffset(ShaderParams.FlaskDispMap, _bumpDistortionOffset);
                    liqMat.SetFloat(ShaderParams.FlaskDispAmount, _distortionAmount);
                    liqMat.SetTexture(ShaderParams.FlaskTex, _texture);
                    liqMat.SetTextureScale(ShaderParams.FlaskTex, _textureScale);
                    liqMat.SetTextureOffset(ShaderParams.FlaskTex, _textureOffset);
                    break;
                case DETAIL.Reflections:
                    if (_reflectionTexture == null) {
                        _reflectionTexture = Resources.Load<Cubemap>("Textures/Reflections");
                    }
                    liqMat.SetTexture(ShaderParams.RefractTex, _reflectionTexture);
                    break;
                case DETAIL.Multiple:
                case DETAIL.MultipleNoFlask:
                    UpdateBubblesProperties();
                    break;
            }

            if (_bubblesAmount < 0) {
                _bubblesAmount = 0;
            }

            if (_noiseVariation != currentNoiseVariation || requireBubblesUpdate) {
                requireBubblesUpdate = false;
                currentNoiseVariation = _noiseVariation;
                if (noise3DTex == null || noise3DTex.Length != 4) {
                    noise3DTex = new Texture3D[4];
                }
                if (noise3DTex[currentNoiseVariation] == null) {
                    noise3DTex[currentNoiseVariation] = Resources.Load<Texture3D>("Textures/Noise3D" + currentNoiseVariation.ToString());
                }
                Texture3D tex3d = noise3DTex[currentNoiseVariation];
                if (tex3d != null) {
                    bool needsBubbleTextureUpdate = _bubblesAmount > 0 || (_bubblesAmount == 0 && tex3d != null && tex3d.name.Contains("Clone"));
                    if (needsBubbleTextureUpdate && _detail.isMultiple()) {
                        if (!tex3d.name.Contains("Clone")) {
                            tex3d = Instantiate(tex3d);
                            tex3d.hideFlags = HideFlags.DontSave;
                            noise3DTex[currentNoiseVariation] = tex3d;
                        }

                        if (colors3D == null || colors3D.Length != 4) {
                            colors3D = new Color[4][];
                        }
                        if (colors3D[currentNoiseVariation] == null) {
                            colors3D[currentNoiseVariation] = tex3d.GetPixels();
                            int length = colors3D[currentNoiseVariation].Length;
                            if (colors3DWithBubbles == null || colors3DWithBubbles.Length != 4) {
                                colors3DWithBubbles = new Color[4][];
                            }
                            if (colors3DWithBubbles[currentNoiseVariation] == null || colors3DWithBubbles[currentNoiseVariation].Length != length) {
                                colors3DWithBubbles[currentNoiseVariation] = new Color[length];
                            }
                        }
                        if (_bubblesAmount > 0 && _detail.isMultiple()) {
                            AddBubbles(tex3d);
                        } else {
                            ClearBubbles(tex3d);
                        }
                    }
                    liqMat.SetTexture(ShaderParams.NoiseTex, tex3d);
                }
            }


            liqMat.renderQueue = _renderQueue;
            UpdateInsideOut();

            if (_topology == TOPOLOGY.Irregular && prevThickness != _flaskThickness) {
                prevThickness = _flaskThickness;
                CleanupBackFacesBuffer();
            }


        }

        Color ApplyGlobalAlpha(Color originalColor) {
            return new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * _alpha);
        }


        void UpdateLevels(bool updateShaderKeywords = true) {

            _level = Mathf.Clamp01(_level);
            levelMultipled = _level * _levelMultiplier;

            if (liqMat == null)
                return;

            if (mesh == null) {
                MeshFilter mf = GetComponent<MeshFilter>();
                if (mf != null) {
                    mesh = mf.sharedMesh;
                    mr = GetComponent<MeshRenderer>();
                } else {
                    SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
                    if (smr != null) {
                        mesh = smr.sharedMesh;
                        mr = smr;
                    }
                }
                ReadVertices();
            }
            if (mesh == null || mr == null) {
                return;
            }

            Vector4 size = new Vector4(mesh.bounds.extents.x * 2f * transform.lossyScale.x, mesh.bounds.extents.y * 2f * transform.lossyScale.y, mesh.bounds.extents.z * 2f * transform.lossyScale.z, 0);
            size.x *= _extentsScale.x;
            size.y *= _extentsScale.y;
            size.z *= _extentsScale.z;
            float maxWidth = Mathf.Max(size.x, size.z);

            Vector3 extents = _ignoreGravity ? new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f) : mr.bounds.extents;
            extents *= (1f - _flaskThickness);
            extents.x *= _extentsScale.x;
            extents.y *= _extentsScale.y;
            extents.z *= _extentsScale.z;

            // Compensate levelpos with upperlimit
            float rotationAdjustment;
            if (_upperLimit < 1f && !_ignoreGravity) {
                float y1 = transform.TransformPoint(Vector3.up * extents.y).y;
                float y0 = transform.TransformPoint(Vector3.up * (extents.y * _upperLimit)).y;
                rotationAdjustment = Mathf.Max(y0 - y1, 0);
            } else {
                rotationAdjustment = 0;
            }

            // Compensate rotation in cylindrical shapes where mesh height is on another scale than width
            float thisLevel = levelMultipled;
            if (!_ignoreGravity && _rotationLevelBias > 0 && thisLevel > 0) {
                Vector3 mrCenter = mr.bounds.center;
                if (lastLevelVolumeRef != thisLevel) {

                    lastLevelVolumeRef = thisLevel;
                    Quaternion q = transform.rotation;
                    transform.rotation = Quaternion.identity;

                    Vector3 tempExtents = _ignoreGravity ? new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f) : mr.bounds.extents;
                    tempExtents *= (1f - _flaskThickness);
                    tempExtents.x *= _extentsScale.x;
                    tempExtents.y *= _extentsScale.y;
                    tempExtents.z *= _extentsScale.z;

                    liquidLevelPos = mrCenter.y - tempExtents.y + tempExtents.y * 2f * thisLevel;

                    RotateVertices();
                    volumeRef = GetMeshVolumeUnderLevel(liquidLevelPos, mrCenter);
                    transform.rotation = q;
                }
                RotateVertices();

                float maxLevel = Mathf.Clamp01(thisLevel + 0.5f);
                float minLevel = Mathf.Clamp01(thisLevel - 0.5f);
                for (int i = 0; i < 12; i++) {
                    thisLevel = (minLevel + maxLevel) * 0.5f;
                    // Supposed liquid level pos?
                    liquidLevelPos = mrCenter.y - extents.y + extents.y * 2f * thisLevel;
                    float volume = GetMeshVolumeUnderLevel(liquidLevelPos, mrCenter);
                    if (volume < volumeRef) {
                        minLevel = thisLevel;
                    } else {
                        if (i >= 8)
                            break;
                        maxLevel = thisLevel;
                    }
                }

                thisLevel = Mathf.Lerp(levelMultipled, thisLevel * _levelMultiplier, _rotationLevelBias);
            } else {
                if (levelMultipled <= 0)
                    thisLevel = -0.001f; // ensure it's below the flask thickness
            }

            liquidLevelPos = mr.bounds.center.y - extents.y; // transform.position.y - extents.y;
            if (_detail != DETAIL.Smoke) {
                liquidLevelPos += extents.y * 2f * thisLevel + rotationAdjustment;
            }
            liqMat.SetFloat(ShaderParams.LevelPos, liquidLevelPos);
            float upperLimit = mesh.bounds.extents.y * _extentsScale.y * _upperLimit;
            liqMat.SetFloat(ShaderParams.UpperLimit, upperLimit);
            float lowerLimit = mesh.bounds.extents.y * _extentsScale.y * _lowerLimit;
            liqMat.SetFloat(ShaderParams.LowerLimit, lowerLimit);
            float visibleLevel = (levelMultipled <= 0 || levelMultipled >= 1f) ? 0f : 1f;
            UpdateTurbulence();
            float foamPos = mr.bounds.center.y - extents.y + (rotationAdjustment + extents.y * 2.0f * (thisLevel + _foamThickness)) * visibleLevel;
            liqMat.SetFloat(ShaderParams.FoamMaxPos, foamPos);
            Vector4 thickness = new Vector4(1.0f - _flaskThickness, (1.0f - _flaskThickness * maxWidth / size.z), (1.0f - _flaskThickness * maxWidth / size.z), 0);
            liqMat.SetVector(ShaderParams.FlaskThickness, thickness);
            size.w = size.x * 0.5f * thickness.x;
            size.x = Vector3.Distance(mr.bounds.max, mr.bounds.min);
            liqMat.SetVector(ShaderParams.Size, size);
            float scaleFactor = size.y * 0.5f * (1.0f - _flaskThickness * maxWidth / size.y);
            liqMat.SetVector(ShaderParams.Scale, new Vector4(_smokeScale / scaleFactor, _foamScale / scaleFactor, _liquidScale1 / scaleFactor, _liquidScale2 / scaleFactor));
            liqMat.SetVector(ShaderParams.Center, transform.position);

            if (shaderKeywords == null || shaderKeywords.Length == 0) {
                shaderKeywords = new string[5];
            }
            for (int k = 0; k < shaderKeywords.Length; k++) {
                shaderKeywords[k] = null;
            }

            if (_detail.isMultiple()) {
                liqMat.SetFloat(ShaderParams.SizeWorld, mr.bounds.size.y);
            }

            if (_depthAware) {
                shaderKeywords[SHADER_KEYWORD_DEPTH_AWARE_INDEX] = SHADER_KEYWORD_DEPTH_AWARE;
                liqMat.SetFloat(ShaderParams.DepthAwareOffset, _depthAwareOffset);
            }

            if (_depthAwareCustomPass) {
                shaderKeywords[SHADER_KEYWORD_DEPTH_AWARE_CUSTOM_PASS_INDEX] = SHADER_KEYWORD_DEPTH_AWARE_CUSTOM_PASS;
            }

            if (_reactToForces && _topology == TOPOLOGY.Sphere) {
                shaderKeywords[SHADER_KEYWORD_IGNORE_GRAVITY_INDEX] = SHADER_KEYWORD_IGNORE_GRAVITY;
            } else if (_ignoreGravity) {
                shaderKeywords[SHADER_KEYWORD_IGNORE_GRAVITY_INDEX] = SHADER_KEYWORD_IGNORE_GRAVITY;
            } else if (transform.rotation.eulerAngles != Vector3.zero) {
                shaderKeywords[SHADER_KEYWORD_NON_AABB_INDEX] = SHADER_KEYWORD_NON_AABB;
            }
            switch (_topology) {
                case TOPOLOGY.Sphere:
                    shaderKeywords[SHADER_KEYWORD_TOPOLOGY_INDEX] = SHADER_KEYWORD_SPHERE;
                    break;
                case TOPOLOGY.Cube:
                    shaderKeywords[SHADER_KEYWORD_TOPOLOGY_INDEX] = SHADER_KEYWORD_CUBE;
                    break;
                case TOPOLOGY.Cylinder:
                    shaderKeywords[SHADER_KEYWORD_TOPOLOGY_INDEX] = SHADER_KEYWORD_CYLINDER;
                    break;
                default:
                    shaderKeywords[SHADER_KEYWORD_TOPOLOGY_INDEX] = SHADER_KEYWORD_IRREGULAR;
                    break;
            }
            if (updateShaderKeywords) {
                liqMat.shaderKeywords = shaderKeywords;
            }

            lastPosition = transform.position;
            lastScale = transform.localScale;
            lastRotation = transform.rotation;
        }


        void RotateVertices() {
            int vertexCount = verticesUnsorted.Length;
            if (rotatedVertices == null || rotatedVertices.Length != vertexCount) {
                rotatedVertices = new Vector3[vertexCount];
            }
            for (int k = 0; k < vertexCount; k++) {
                rotatedVertices[k] = transform.TransformPoint(verticesUnsorted[k]);
            }
        }


        float SignedVolumeOfTriangle(ref Vector3 p1, ref Vector3 p2, ref Vector3 p3) {
            float v321 = p3.x * p2.y * p1.z;
            float v231 = p2.x * p3.y * p1.z;
            float v312 = p3.x * p1.y * p2.z;
            float v132 = p1.x * p3.y * p2.z;
            float v213 = p2.x * p1.y * p3.z;
            float v123 = p1.x * p2.y * p3.z;
            return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }

        float GetMeshVolumeUnderLevel(float level, Vector3 zeroPoint) {
            float vol = 0;
            for (int k = 0; k < verticesIndices.Length; k += 3) {
                Vector3 p1 = rotatedVertices[verticesIndices[k]];
                Vector3 p2 = rotatedVertices[verticesIndices[k + 1]];
                Vector3 p3 = rotatedVertices[verticesIndices[k + 2]];
                if (p1.y > level) p1.y = level;
                if (p2.y > level) p2.y = level;
                if (p3.y > level) p3.y = level;
                p1.x -= zeroPoint.x; p1.y -= zeroPoint.y; p1.z -= zeroPoint.z;
                p2.x -= zeroPoint.x; p2.y -= zeroPoint.y; p2.z -= zeroPoint.z;
                p3.x -= zeroPoint.x; p3.y -= zeroPoint.y; p3.z -= zeroPoint.z;
                vol += SignedVolumeOfTriangle(ref p1, ref p2, ref p3);
            }
            return Mathf.Abs(vol);
        }


        void UpdateTurbulence() {
            if (liqMat == null)
                return;
            float visibleLevel = 1f; // (_level<=0 || _level>=1f) ? 0.1f: 1f;	// commented out to allow animation even level is 0 or full
            float isInsideContainer = (camInside && _allowViewFromInside) ? 0f : 1f;
            turb.x = _turbulence1 * visibleLevel * isInsideContainer;
            turb.y = Mathf.Max(_turbulence2, turbulenceDueForces) * visibleLevel * isInsideContainer;
            shaderTurb = turb;
            shaderTurb.z *= 3.1415927f * _frecuency * 4f;
            shaderTurb.w *= 3.1415927f * _frecuency * 4f;
            liqMat.SetVector(ShaderParams.Turbulence, shaderTurb);
        }

        void CheckInsideOut() {
            Camera cam = Camera.current;
            if (cam == null || mr == null) {
                if (!_allowViewFromInside)
                    UpdateInsideOut();
                return;
            }

            Vector3 currentCamPos = cam.transform.position + cam.transform.forward * cam.nearClipPlane;
            float currentDistanceToCam = (currentCamPos - transform.position).sqrMagnitude;
            if (currentDistanceToCam == lastDistanceToCam)
                return;
            lastDistanceToCam = currentDistanceToCam;

            // Check if position is inside container
            bool nowInside = false;
            switch (_topology) {
                case TOPOLOGY.Cube:
                    nowInside = PointInAABB(currentCamPos);
                    break;
                case TOPOLOGY.Cylinder:
                    nowInside = PointInCylinder(currentCamPos);
                    break;
                default:
                    float diam = mesh.bounds.extents.x * 2f;
                    nowInside = (currentCamPos - transform.position).sqrMagnitude < (diam * diam);
                    break;
            }

            if (nowInside != camInside) {
                camInside = nowInside;
                UpdateInsideOut();
            }
        }


        bool PointInAABB(Vector3 point) {
            point = transform.InverseTransformPoint(point);
            Vector3 ext = mesh.bounds.extents;
            if (point.x < ext.x && point.x > -ext.x &&
                point.y < ext.y && point.y > -ext.y &&
                point.z < ext.z && point.z > -ext.z) {
                return true;
            } else {
                return false;
            }
        }

        bool PointInCylinder(Vector3 point) {
            point = transform.InverseTransformPoint(point);
            Vector3 ext = mesh.bounds.extents;
            if (point.x < ext.x && point.x > -ext.x &&
                point.y < ext.y && point.y > -ext.y &&
                point.z < ext.z && point.z > -ext.z) {

                point.y = 0;
                Vector3 currentPos = transform.position;
                currentPos.y = 0;
                return (point - currentPos).sqrMagnitude < ext.x * ext.x;
            }
            return false;
        }


        void UpdateInsideOut() {
            if (liqMat == null)
                return;
            if (_allowViewFromInside && camInside) {
                liqMat.SetInt(ShaderParams.CullMode, (int)UnityEngine.Rendering.CullMode.Front);
                liqMat.SetInt(ShaderParams.ZTestMode, (int)UnityEngine.Rendering.CompareFunction.Always);
            } else {
                liqMat.SetInt(ShaderParams.CullMode, (int)UnityEngine.Rendering.CullMode.Back);
                liqMat.SetInt(ShaderParams.ZTestMode, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
            UpdateTurbulence();
        }

        #endregion


        #region CommandBuffer setup

        private void CleanupCommandBuffer() {
            CleanupRefractionBuffer();
            CleanupBackFacesBuffer();
            CleanupFrontFacesBuffer();
            wasBackBuffer = wasRefractionBlur = wasFrontBuffer = false;
        }

        static Dictionary<Camera, CommandBuffer> m_CamerasBlur = new Dictionary<Camera, CommandBuffer>();
        static Material blurMat;

        internal static void CleanupRefractionBuffer() {
            foreach (var cam in m_CamerasBlur) {
                if (cam.Key) {
                    cam.Key.RemoveCommandBuffer(CameraEvent.AfterImageEffectsOpaque, cam.Value);
                }
            }
            m_CamerasBlur.Clear();
            if (blurMat != null) {
                DestroyImmediate(blurMat);
                blurMat = null;
            }
        }

        internal static void SetupRefractionBuffer() {
            var cam = Camera.current;
            if (!cam)
                return;

            CommandBuffer buf = null;
            if (m_CamerasBlur.ContainsKey(cam))
                return;

            if (blurMat == null) {
                blurMat = Instantiate(Resources.Load<Material>("Materials/LiquidVolumeBlur")) as Material;
                blurMat.hideFlags = HideFlags.DontSave;
            }

            buf = new CommandBuffer();
            buf.name = "Volumetric Liquid Background Blur";
            m_CamerasBlur[cam] = buf;

            int screenCopyID = Shader.PropertyToID("_VLScreenCopyTexture");
            buf.GetTemporaryRT(screenCopyID, -1, -1, 0, FilterMode.Bilinear);
            buf.Blit(BuiltinRenderTextureType.CurrentActive, screenCopyID);

            int blurredID = Shader.PropertyToID("_VLTemp1");
            int blurredID2 = Shader.PropertyToID("_VLTemp2");
            buf.GetTemporaryRT(blurredID, -2, -2, 0, FilterMode.Bilinear);
            buf.GetTemporaryRT(blurredID2, -3, -3, 0, FilterMode.Bilinear);

            buf.Blit(screenCopyID, blurredID2, blurMat, 0);
            buf.ReleaseTemporaryRT(screenCopyID);

            buf.Blit(blurredID2, blurredID, blurMat, 1);
            buf.ReleaseTemporaryRT(blurredID2);

            buf.SetGlobalTexture(ShaderParams.GlobalRefractionTexture, blurredID);

            cam.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, buf);

        }

        #endregion

        #region Backbuffer

        static Dictionary<Camera, CommandBuffer> m_CamerasBackBuffer = new Dictionary<Camera, CommandBuffer>();
        static Material backBufferMat;
        static List<Renderer> bbRenderers = new List<Renderer>();

        internal static void SetupBackFacesBuffer(Renderer renderer) {
            var cam = Camera.current;
            if (!cam || renderer == null)
                return;

            Renderer[] renderers = renderer.GetComponentsInChildren<Renderer>();
            for (int k = 0; k < renderers.Length; k++) {
                if (!bbRenderers.Contains(renderers[k])) {
                    CleanupBackFacesCameras();
                    break;
                }
            }

            CommandBuffer buf = null;
            if (m_CamerasBackBuffer.ContainsKey(cam)) {
                return;
            }

            if (backBufferMat == null) {
                backBufferMat = new Material(Shader.Find("LiquidVolume/ZWriteBack")) as Material;
                backBufferMat.hideFlags = HideFlags.DontSave;
            }

            for (int k = 0; k < renderers.Length; k++) {
                if (!bbRenderers.Contains(renderers[k])) {
                    bbRenderers.Add(renderers[k]);
                }
            }
            //bbRenderers.Add(renderer);
            buf = new CommandBuffer();
            buf.name = "Volumetric Liquid BackBuffer";
            m_CamerasBackBuffer[cam] = buf;

            int backBufferID = Shader.PropertyToID("_VLBackBufferTexture");
            if (LiquidVolume.useFPRenderTextures) {
                buf.GetTemporaryRT(backBufferID, -1, -1, 24, FilterMode.Point, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
                buf.SetRenderTarget(backBufferID);
                buf.ClearRenderTarget(true, true, new Color(cam.farClipPlane, 0, 0, 0), 0f);
                buf.EnableShaderKeyword(SHADER_KEYWORD_FP_RENDER_TEXTURE);
            } else {
                buf.GetTemporaryRT(backBufferID, -1, -1, 24, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                buf.SetRenderTarget(backBufferID);
                buf.ClearRenderTarget(true, true, new Color(0.9882353f, 0.4470558f, 0.75f, 0f), 0f);
                buf.DisableShaderKeyword(SHADER_KEYWORD_FP_RENDER_TEXTURE);
            }
            bbRenderers.ForEach((Renderer obj) => {
                if (obj != null && obj.gameObject.activeSelf) {
                    LiquidVolume liquidVolume = obj.GetComponent<LiquidVolume>();
                    if (liquidVolume != null) {
                        backBufferMat.SetFloat(ShaderParams.FlaskThickness, 1.0f - liquidVolume.flaskThickness);
                    } else {
                        backBufferMat.SetFloat(ShaderParams.FlaskThickness, 1.0f);
                    }
                    buf.DrawRenderer(obj, backBufferMat);
                }
            });
            cam.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, buf);
        }

        internal static void CleanupBackFacesBuffer() {
            CleanupBackFacesCameras();
            if (backBufferMat != null) {
                DestroyImmediate(backBufferMat);
                backBufferMat = null;
            }
            bbRenderers.Clear();
        }


        internal static void CleanupBackFacesCameras() {
            foreach (var cam in m_CamerasBackBuffer) {
                if (cam.Key) {
                    cam.Key.RemoveCommandBuffer(CameraEvent.AfterImageEffectsOpaque, cam.Value);
                }
            }
            m_CamerasBackBuffer.Clear();
        }

        #endregion

        #region FrontBuffer

        static Dictionary<Camera, CommandBuffer> m_CamerasFrontBuffer = new Dictionary<Camera, CommandBuffer>();
        static Material frontBufferMat;
        static List<Renderer> fbRenderers = new List<Renderer>();

        internal static void SetupFrontFacesBuffer(Renderer renderer) {
            var cam = Camera.current;
            if (!cam)
                return;

            if (!fbRenderers.Contains(renderer))
                CleanupFrontFacesCameras();

            CommandBuffer buf = null;
            if (m_CamerasFrontBuffer.ContainsKey(cam)) {
                return;
            }

            if (frontBufferMat == null) {
                frontBufferMat = new Material(Shader.Find("LiquidVolume/ZWriteFront")) as Material;
                frontBufferMat.hideFlags = HideFlags.DontSave;
            }

            fbRenderers.Add(renderer);
            buf = new CommandBuffer();
            buf.name = "Volumetric Liquid FrontBuffer";
            m_CamerasFrontBuffer[cam] = buf;

            int frontBufferID = Shader.PropertyToID("_VLFrontBufferTexture");
            if (useFPRenderTextures) {
                buf.GetTemporaryRT(frontBufferID, -1, -1, 24, FilterMode.Point, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
                buf.SetRenderTarget(frontBufferID);
                buf.ClearRenderTarget(true, true, new Color(cam.farClipPlane, 0, 0, 0), 1f);
                buf.EnableShaderKeyword(SHADER_KEYWORD_FP_RENDER_TEXTURE);
            } else {
                buf.GetTemporaryRT(frontBufferID, -1, -1, 24, FilterMode.Point, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                buf.SetRenderTarget(frontBufferID);
                buf.ClearRenderTarget(true, true, new Color(0.9882353f, 0.4470558f, 0.75f, 0f), 1f);
                buf.DisableShaderKeyword(SHADER_KEYWORD_FP_RENDER_TEXTURE);
            }
            fbRenderers.ForEach((Renderer obj) => {
                buf.DrawRenderer(obj, frontBufferMat);
            });
            cam.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, buf);
        }

        internal static void CleanupFrontFacesBuffer() {
            CleanupFrontFacesCameras();
            if (frontBufferMat != null) {
                DestroyImmediate(frontBufferMat);
                frontBufferMat = null;
            }
            fbRenderers.Clear();
        }


        internal static void CleanupFrontFacesCameras() {
            foreach (var cam in m_CamerasFrontBuffer) {
                if (cam.Key) {
                    cam.Key.RemoveCommandBuffer(CameraEvent.AfterImageEffectsOpaque, cam.Value);
                }
            }
            m_CamerasFrontBuffer.Clear();
        }

        #endregion



        #region Public API

        /// <summary>
        /// Returns the vertical position in world space coordinates of the liquid surface
        /// </summary>
        /// <value>The get liquid surface Y position.</value>
        public float liquidSurfaceYPosition {
            get {
                return liquidLevelPos;
            }
        }

        /// <summary>
        /// Changes the transform position and/or rotation of a floating object.
        /// </summary>
        public void MoveToLiquidSurface(Transform transform, BuoyancyEffect effect, Transform root = null, float dampen = 0.8f) {
            Vector3 position = transform.position;
            if (effect == BuoyancyEffect.Simple) {
                position.y = liquidLevelPos;
                transform.position = position;
                return;
            }
            if (effect == BuoyancyEffect.PositionOnly) {
                Vector4 shaderTurb = liqMat.GetVector("_Turbulence");
                float sizeY = liqMat.GetVector("_Size").y;
                float turbFactor = shaderTurb.y * 0.05f * sizeY * _foamTurbulence;
                float turbulence1 = Mathf.Sin(transform.position.x * shaderTurb.z + transform.position.z * shaderTurb.w + turbulenceSpeed * 4f) * turbFactor;
                float y1 = liquidLevelPos + turbulence1;
                position.y = y1;
                transform.position = position;
                return;
            }

            // Position and rotation
            if (effect == BuoyancyEffect.PositionAndRotation) {
                if (root == null) {
                    Debug.LogError("No root specified when calling MoveToLiquidSurface(..). Usually the root is a parent object that receives the buoyancy position and rotation changes.");
                }
                Vector4 shaderTurb = liqMat.GetVector("_Turbulence");
                Collider collider = transform.GetComponent<Collider>();
                if (collider == null) {
                    Debug.LogError("No collider found in object used when calling MoveToLiquidSurface(..) of Liquid Volume.");
                    return;
                }
                float sizeY = liqMat.GetVector("_Size").y;
                float turbFactor = shaderTurb.y * 0.05f * sizeY * _foamTurbulence;
                Vector3 min = collider.bounds.min;
                Vector3 max = collider.bounds.max;
                float turbulence1 = Mathf.Sin(min.x * shaderTurb.z + min.z * shaderTurb.w + turbulenceSpeed * 4f) * turbFactor;
                float turbulence2 = Mathf.Sin(max.x * shaderTurb.z + max.z * shaderTurb.w + turbulenceSpeed * 4f) * turbFactor;
                float y1 = liquidLevelPos + turbulence1;
                float y2 = liquidLevelPos + turbulence2;
                position.y = (y1 + y2) * 0.5f;
                root.transform.position = position;
                Vector3 boundsCenter = collider.bounds.center;
                Vector3 p1 = new Vector3(min.x - boundsCenter.x, y1, min.z - boundsCenter.z);
                Vector3 p2 = new Vector3(max.x - boundsCenter.x, y2, max.z - boundsCenter.z);
                Vector3 n = Vector3.Cross((p2 - p1).normalized, Vector3.back);
                n.y = n.y < 0 ? -n.y : n.y;
                Debug.DrawRay(transform.position, transform.position + n);
                root.transform.up = Vector3.Lerp(root.transform.up, n, dampen);
            }
        }

        /// <summary>
        /// Computes approximate point where liquid starts pouring over the flask when it's rotated
        /// </summary>
        /// <returns><c>true</c>, if spill point is detected, <c>false</c> otherwise.</returns>
        /// <param name="spillPosition">Returned spill position in world space coordinates.</param>
        /// <param name="apertureStart">A value that determines where the aperture of the flask starts (0-1 where 0 is flask center and 1 is the very top).</param>
        public bool GetSpillPoint(out Vector3 spillPosition, float apertureStart = 1f) {
            float spillAmount;
            return GetSpillPoint(out spillPosition, out spillAmount, apertureStart);
        }



        /// <summary>
        /// Computes approximate point where liquid starts pouring over the flask when it's rotated
        /// </summary>
        /// <returns><c>true</c>, if spill point is detected, <c>false</c> otherwise.</returns>
        /// <param name="spillPosition">Returned spill position in world space coordinates.</param>
        /// <param name="spillAmount">A returned value that represent the amount of liquid spilt.</param>
        /// <param name="apertureStart">A value that determines where the aperture of the flask starts (0-1 where 0 is flask center and 1 is the very top).</param>
        public bool GetSpillPoint(out Vector3 spillPosition, out float spillAmount, float apertureStart = 1f) {
            spillPosition = Vector3.zero;
            spillAmount = 0;
            if (mesh == null || verticesSorted == null || levelMultipled <= 0)
                return false;

            float maxy = float.MinValue;
            for (int k = 0; k < verticesSorted.Length; k++) {
                Vector3 vertex = verticesSorted[k];
                if (vertex.y > maxy) {
                    maxy = vertex.y;
                }
            }
            float clampy = maxy * apertureStart * 0.99f;
            Vector3 vt = transform.position;
            bool crossed = false;
            float miny = float.MaxValue;
            for (int k = 0; k < verticesSorted.Length; k++) {
                Vector3 vertex = verticesSorted[k];
                if (vertex.y < clampy)
                    break;
                vertex = transform.TransformPoint(vertex);
                if (vertex.y < liquidLevelPos && vertex.y < miny) {
                    miny = vertex.y;
                    vt = vertex;
                    crossed = true;
                }
            }
            if (!crossed)
                return false;

            spillPosition = vt;
            spillAmount = (liquidLevelPos - vt.y) / (mesh.bounds.extents.y * 2f * transform.localScale.y);
            return true;
        }


        void UpdateSpillPointGizmo() {
            if (!_debugSpillPoint) {
                if (spillPointGizmo != null) {
                    DestroyImmediate(spillPointGizmo.gameObject);
                    spillPointGizmo = null;
                }
                return;
            }

            if (spillPointGizmo == null) {
                Transform t = transform.Find(SPILL_POINT_GIZMO);
                if (t != null) {
                    DestroyImmediate(t.gameObject);
                }
                spillPointGizmo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spillPointGizmo.name = SPILL_POINT_GIZMO;
                spillPointGizmo.transform.SetParent(transform, true);
                Collider collider = spillPointGizmo.GetComponent<Collider>();
                if (collider != null)
                    DestroyImmediate(collider);
                MeshRenderer mr = spillPointGizmo.GetComponent<MeshRenderer>();
                if (mr != null) {
                    mr.sharedMaterial = Instantiate(mr.sharedMaterial); // to avoid Editor (non playing) warning
                    mr.sharedMaterial.hideFlags = HideFlags.DontSave;
                    mr.sharedMaterial.color = Color.yellow;
                }
            }

            Vector3 spillPoint;
            if (GetSpillPoint(out spillPoint, 1f)) {
                spillPointGizmo.transform.position = spillPoint;
                if (mesh != null) {
                    Vector3 size = mesh.bounds.extents * 0.2f;
                    float s = size.x > size.y ? size.x : size.z;
                    s = s > size.z ? s : size.z;
                    spillPointGizmo.transform.localScale = new Vector3(s, s, s);
                } else {
                    spillPointGizmo.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                }
                spillPointGizmo.SetActive(true);
            } else {
                spillPointGizmo.SetActive(false);
            }

        }

        /// <summary>
        /// Applies current transform rotation and scale to the vertices and resets the transform rotation and scale to default values.
        /// This operation makes the game object transform point upright as normal game objects and is required for Liquid Volume to work on imported models that comes with a rotation
        /// </summary>
        public void BakeRotation() {

            if (transform.localRotation == transform.rotation) {
                // nothing to do!
                return;
            }
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) return;
            mesh = Instantiate(mesh);
            Vector3[] vertices = mesh.vertices;
            Vector3 scale = transform.localScale;
            Vector3 localPos = transform.localPosition;

            transform.localScale = Vector3.one;

            Transform parent = transform.parent;
            if (parent != null) {
                transform.SetParent(null, false);
            }

            for (int k = 0; k < vertices.Length; k++) {
                vertices[k] = transform.TransformVector(vertices[k]);
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;

            // Ensure parent has no different rotation
            if (parent != null) {
                transform.SetParent(parent, false);
                transform.localPosition = localPos;
            }

            transform.localRotation = Quaternion.Euler(0, 0, 0);
            transform.localScale = scale;

            RefreshMeshAndCollider();
        }

        /// <summary>
        /// This operation computes the geometric center of all vertices and displaces them so the pivot is centered in the model
        /// </summary>
        public void CenterPivot() {
            CenterPivot(Vector3.zero, false);
        }

        List<Vertex> hullVertices = new List<Vertex>();
        Mesh fixedMesh, closedMesh;

        /// <summary>
        /// This operation computes the geometric center of all vertices and displaces them so the pivot is centered in the model
        /// </summary>
        public void CenterPivot(Vector3 offset, bool closeMesh) {

            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) return;

            mesh = Instantiate(mesh);
            mesh.name = mf.sharedMesh.name; // keep original name to detect if user assigns a different mesh to meshfilter and discard originalMesh reference

            Vector3[] vertices = mesh.vertices;

            if (closeMesh) {
                hullVertices.Clear();
                for (int k = 0; k < vertices.Length; k++) {
                    hullVertices.Add(new Vertex(vertices[k]));
                }

                // Create hull
                var result = ConvexHull.Create(hullVertices);
                mesh.Clear();
                mesh.vertices = result.Points.Select(x => x.ToVec()).ToArray();
                var xxx = result.Points.ToList();

                List<int> triangles = new List<int>();
                foreach (var face in result.Faces) {
                    triangles.Add(xxx.IndexOf(face.Vertices[0]));
                    triangles.Add(xxx.IndexOf(face.Vertices[1]));
                    triangles.Add(xxx.IndexOf(face.Vertices[2]));
                }
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();

                vertices = mesh.vertices;
                closedMesh = mesh;
            }

            Vector3 midPoint = Vector3.zero;
            for (int k = 0; k < vertices.Length; k++) {
                midPoint += vertices[k];
            }
            midPoint /= vertices.Length;
            midPoint += offset;
            for (int k = 0; k < vertices.Length; k++) {
                vertices[k] -= midPoint;
            }
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            if (closeMesh) {
                mesh.RecalculateNormals();
            }
            mf.sharedMesh = mesh;

            fixedMesh = mesh;

            Vector3 localScale = transform.localScale;
            midPoint.x *= localScale.x;
            midPoint.y *= localScale.y;
            midPoint.z *= localScale.z;
            transform.localPosition += midPoint;

            RefreshMeshAndCollider();
        }

        public void RefreshMeshAndCollider() {
            ClearMeshCache();
            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null) {
                Mesh oldMesh = mc.sharedMesh;
                mc.sharedMesh = null;
                mc.sharedMesh = oldMesh;
            }
        }

        /// <summary>
        /// Assigns a new layer density and optionally keep the same volume
        /// </summary>
        /// <param name="newDensity">New density.</param>
        /// <param name="keepVolume">If set to <c>true</c> keep volume.</param>
        public void SetLayerDensity(int layerIndex, float newDensity, bool keepVolume) {
            if (layerIndex < 0 || _liquidLayers == null || layerIndex >= _liquidLayers.Length)
                return;
            float amount = _liquidLayers[layerIndex].amount;
            if (keepVolume && _liquidLayers[layerIndex].density > 0) {
                amount *= newDensity / _liquidLayers[layerIndex].density;
            }
            _liquidLayers[layerIndex].amount = amount;
            _liquidLayers[layerIndex].density = newDensity;
        }


        #endregion

        #region Multiple layers

        void InitLayers() {
            requireLayersUpdate = true;
            lastLayerCount = -1;

            if (_liquidLayers == null)
                return;
            for (int k = 0; k < _liquidLayers.Length; k++) {
                _liquidLayers[k].ResetCurrentStates();
            }
        }

        public void UpdateLayers(bool immediate = false) {
            shouldUpdateMaterialProperties = true;
            requireLayersUpdate = true;
            lastLayerCount = _liquidLayers.Length;
            if (immediate) {
                this.immediate = true;
                RenderObject();
            }
        }

        void UpdateLayersNow() {

            requireLayersUpdate = false;

            if (_liquidLayers == null) {
                _liquidLayers = new LiquidLayer[0];
            } else if (_liquidLayers.Length > MAX_LAYERS) {
                Array.Resize(ref _liquidLayers, MAX_LAYERS);
                Debug.Log("Liquid layers max count exceeded. Capping to " + MAX_LAYERS + ".");
            }

            if (lastLayerCount >= 0 && lastLayerCount < _liquidLayers.Length) {
                for (int k = lastLayerCount; k < _liquidLayers.Length; k++) {
                    _liquidLayers[k].SetDefaults();
                }
            }
            lastLayerCount = _liquidLayers.Length;

            if (sortedLayers == null || sortedLayers.Length < MAX_LAYERS) {
                sortedLayers = new int[MAX_LAYERS];
            }

            // ensure proper values
            for (int k = 0; k < _liquidLayers.Length; k++) {
                _liquidLayers[k].amount = Mathf.Max(0, _liquidLayers[k].amount);
                _liquidLayers[k].layerName = "Layer " + k;
                _liquidLayers[k].scale = Mathf.Clamp(_liquidLayers[k].scale, 0.001f, 0.48f);
                _liquidLayers[k].murkiness = Mathf.Clamp01(_liquidLayers[k].murkiness);
                if (_liquidLayers[k].adjustmentSpeed <= 0)
                    _liquidLayers[k].adjustmentSpeed = 1f;
                if (_liquidLayers[k].density < 0.001f)
                    _liquidLayers[k].density = 0.001f;
            }

            // Sort & stack by density
            sortedLayersCount = 0;
            float currentDensity = float.MaxValue;
            float baseLevel = 0;
            for (int k = 0; k < _liquidLayers.Length; k++) {
                float maxDensity = float.MinValue;
                // Pick greater density first
                for (int j = 0; j < _liquidLayers.Length; j++) {
                    float density = _liquidLayers[j].density;
                    if (density > maxDensity && density < currentDensity) {
                        maxDensity = density;
                    }
                }
                if (maxDensity == float.MinValue)
                    break;
                // Place layers of the mix
                for (int j = 0; j < _liquidLayers.Length; j++) {
                    if (_liquidLayers[j].density == maxDensity) {
                        _liquidLayers[j].baseLevel = baseLevel;
                        sortedLayers[sortedLayersCount++] = j;
                        baseLevel += _liquidLayers[j].amount;
                    }
                }
                currentDensity = maxDensity;
            }
            // Handle miscible layers
            for (int k = 0; k < sortedLayersCount; k++) {
                int i = sortedLayers[k];
                if (_liquidLayers[i].miscible) {
                    LiquidLayer temp = new LiquidLayer();
                    int j = k;
                    float groupBaseLevel = _liquidLayers[i].baseLevel;
                    float groupDensity = _liquidLayers[i].density;
                    while (_liquidLayers[i].miscible && _liquidLayers[i].density == groupDensity) {
                        float amount = Mathf.Max(_liquidLayers[i].amount, 0.0001f);
                        temp.color += _liquidLayers[i].color * amount;
                        temp.murkColor += _liquidLayers[i].murkColor * amount;
                        temp.murkiness += _liquidLayers[i].murkiness * amount;
                        temp.scale += _liquidLayers[i].scale * amount;
                        temp.amount += amount;
                        if (++j >= sortedLayersCount)
                            break;
                        i = sortedLayers[j];
                    }
                    // Combine
                    for (int l = k; l < j; l++) {
                        i = sortedLayers[l];
                        _liquidLayers[i].mixedColor = temp.color / temp.amount;
                        _liquidLayers[i].mixedColor2 = temp.murkColor / temp.amount;
                        _liquidLayers[i].mixedMurkiness = temp.murkiness / temp.amount;
                        _liquidLayers[i].mixedScale = temp.scale / temp.amount;
                        _liquidLayers[i].mixedBubblesOpacity = temp.bubblesOpacity / temp.amount;
                        _liquidLayers[i].mixedAmount = temp.amount / groupDensity;
                        _liquidLayers[i].baseLevel = groupBaseLevel;
                    }
                    k = j - 1;
                } else {
                    _liquidLayers[i].mixedColor = _liquidLayers[i].color;
                    _liquidLayers[i].mixedColor2 = _liquidLayers[i].murkColor;
                    _liquidLayers[i].mixedMurkiness = _liquidLayers[i].murkiness;
                    _liquidLayers[i].mixedScale = _liquidLayers[i].scale;
                    _liquidLayers[i].mixedBubblesOpacity = _liquidLayers[i].bubblesOpacity;
                    _liquidLayers[i].mixedAmount = Mathf.Clamp01(_liquidLayers[i].amount / _liquidLayers[i].density);
                }
            }

            // Compact
            for (int k = 0; k < sortedLayersCount - 1; k++) {
                int i = sortedLayers[k];
                int j = sortedLayers[k + 1];
                float expectedBaseLevel = _liquidLayers[i].baseLevel + _liquidLayers[i].mixedAmount;
                if (_liquidLayers[j].baseLevel > expectedBaseLevel) {
                    _liquidLayers[j].baseLevel = expectedBaseLevel;
                }
            }

            AnimateLayers(true);
        }

        int layerComparer(LiquidLayer layer1, LiquidLayer layer2) {
            return layer1.density.CompareTo(layer2.density);
        }

        void AnimateLayers(bool changes = false) {
            if (Application.isPlaying) {
                for (int j = 0; j < sortedLayersCount; j++) {
                    int k = sortedLayers[j];
                    if (_liquidLayers[k].currentBaseLevel != _liquidLayers[k].baseLevel || _liquidLayers[k].currentAmount != _liquidLayers[k].mixedAmount || _liquidLayers[k].currentMurkiness != _liquidLayers[k].mixedMurkiness || _liquidLayers[k].currentBubblesOpacity != _liquidLayers[k].mixedBubblesOpacity || _liquidLayers[k].currentColor != _liquidLayers[k].mixedColor || _liquidLayers[k].currentColor2 != _liquidLayers[k].mixedColor2) {
                        float t = Time.deltaTime * _layersAdjustmentSpeed * _liquidLayers[k].adjustmentSpeed;
                        if (t > 1f || immediate) {
                            t = 1f;
                        }
                        if (Similar(_liquidLayers[k].currentBaseLevel, _liquidLayers[k].baseLevel)) {
                            _liquidLayers[k].currentBaseLevel = _liquidLayers[k].baseLevel;
                        } else {
                            _liquidLayers[k].currentBaseLevel = Mathf.Lerp(_liquidLayers[k].currentBaseLevel, _liquidLayers[k].baseLevel, t);
                        }
                        if (Similar(_liquidLayers[k].currentAmount, _liquidLayers[k].mixedAmount)) {
                            _liquidLayers[k].currentAmount = _liquidLayers[k].mixedAmount;
                        } else {
                            _liquidLayers[k].currentAmount = Mathf.Lerp(_liquidLayers[k].currentAmount, _liquidLayers[k].mixedAmount, t);
                        }
                        if (Similar(_liquidLayers[k].currentMurkiness, _liquidLayers[k].mixedMurkiness)) {
                            _liquidLayers[k].currentMurkiness = _liquidLayers[k].mixedMurkiness;
                        } else {
                            _liquidLayers[k].currentMurkiness = Mathf.Lerp(_liquidLayers[k].currentMurkiness, _liquidLayers[k].mixedMurkiness, t);
                        }
                        if (Similar(ref _liquidLayers[k].currentColor, ref _liquidLayers[k].mixedColor)) {
                            _liquidLayers[k].currentColor = _liquidLayers[k].mixedColor;
                        } else {
                            _liquidLayers[k].currentColor = Color.Lerp(_liquidLayers[k].currentColor, _liquidLayers[k].mixedColor, t);
                        }
                        if (Similar(ref _liquidLayers[k].currentColor2, ref _liquidLayers[k].mixedColor2)) {
                            _liquidLayers[k].currentColor2 = _liquidLayers[k].mixedColor2;
                        } else {
                            _liquidLayers[k].currentColor2 = Color.Lerp(_liquidLayers[k].currentColor2, _liquidLayers[k].mixedColor2, t);
                        }
                        if (Similar(_liquidLayers[k].currentBubblesOpacity, _liquidLayers[k].mixedBubblesOpacity)) {
                            _liquidLayers[k].currentBubblesOpacity = _liquidLayers[k].mixedBubblesOpacity;
                        } else {
                            _liquidLayers[k].currentBubblesOpacity = Mathf.Lerp(_liquidLayers[k].currentBubblesOpacity, _liquidLayers[k].mixedBubblesOpacity, t);
                        }
                        changes = true;
                    }
                }
                if (changes) {
                    // Compact
                    if (_layersAdjustmentCompact) {
                        float currentBaseLevel = float.MinValue;
                        float expectedBaseLevel = 0;
                        for (int k = 0; k < _liquidLayers.Length; k++) {
                            // Get the minimum
                            float minCurrentBaseLevel = float.MaxValue;
                            int min = -1;
                            for (int j = 0; j < _liquidLayers.Length; j++) {
                                if (_liquidLayers[j].currentBaseLevel < minCurrentBaseLevel && _liquidLayers[j].currentBaseLevel > currentBaseLevel) {
                                    minCurrentBaseLevel = _liquidLayers[j].currentBaseLevel;
                                    min = j;
                                }
                            }
                            if (min < 0)
                                break;
                            if (k > 0) {
                                if (_liquidLayers[min].currentBaseLevel > expectedBaseLevel) {
                                    _liquidLayers[min].currentBaseLevel = expectedBaseLevel;
                                }
                            }
                            expectedBaseLevel = _liquidLayers[min].currentBaseLevel + _liquidLayers[min].currentAmount;
                            currentBaseLevel = _liquidLayers[min].currentBaseLevel;
                        }
                    }
                }
            } else {
                for (int j = 0; j < sortedLayersCount; j++) {
                    int k = sortedLayers[j];
                    if (_liquidLayers[k].currentBaseLevel != _liquidLayers[k].baseLevel || _liquidLayers[k].currentAmount != _liquidLayers[k].mixedAmount || _liquidLayers[k].currentMurkiness != _liquidLayers[k].mixedMurkiness || _liquidLayers[k].currentColor != _liquidLayers[k].mixedColor || _liquidLayers[k].currentColor2 != _liquidLayers[k].mixedColor2) {
                        _liquidLayers[k].currentBaseLevel = _liquidLayers[k].baseLevel;
                        _liquidLayers[k].currentAmount = _liquidLayers[k].mixedAmount;
                        _liquidLayers[k].currentColor = _liquidLayers[k].mixedColor;
                        _liquidLayers[k].currentColor2 = _liquidLayers[k].mixedColor2;
                        _liquidLayers[k].currentMurkiness = _liquidLayers[k].mixedMurkiness;
                        _liquidLayers[k].currentBubblesOpacity = _liquidLayers[k].mixedBubblesOpacity;
                        changes = true;
                    }
                }
            }
            immediate = false;

            if (changes) {
                UpdateLayersProperties();
            }
        }

        bool Similar(float a, float b) {
            if (a < b) {
                return (b - a) < 0.01f;
            } else {
                return (a - b) < 0.01f;
            }
        }

        bool Similar(ref Color a, ref Color b) {
            float dr, dg, db, da;
            dr = a.r < b.r ? b.r - a.r : a.r - b.r;
            if (dr > 0.01f)
                return false;
            dg = a.g < b.g ? b.g - a.g : a.g - b.g;
            if (dg > 0.01f)
                return false;
            db = a.b < b.b ? b.b - a.b : a.b - b.b;
            if (db > 0.01f)
                return false;
            da = a.a < b.a ? b.a - a.a : a.a - b.a;
            return da < 0.01f;
        }


        void UpdateLayersProperties() {

            if (_liquidLayers == null || sortedLayers == null || liqMat == null)
                return;

            const int LAYERS_RESOLUTION = 256;

            if (layersTextureColors == null || layersTextureColors.Length != LAYERS_RESOLUTION) {
                layersTextureColors = new Color[LAYERS_RESOLUTION];
            }
            if (layersTextureColors2 == null || layersTextureColors2.Length != LAYERS_RESOLUTION) {
                layersTextureColors2 = new Color[LAYERS_RESOLUTION];
            }
            if (layersTextureColorsTemp == null || layersTextureColorsTemp.Length != LAYERS_RESOLUTION) {
                layersTextureColorsTemp = new Color[LAYERS_RESOLUTION];
            }
            if (layersProperties == null || layersProperties.Length != LAYERS_RESOLUTION) {
                layersProperties = new Vector4[LAYERS_RESOLUTION];
            }
            if (layersSum == null || layersSum.Length != LAYERS_RESOLUTION) {
                layersSum = new float[LAYERS_RESOLUTION];
            }

            Color colorNull = new Color(0, 0, 0, 0);
            Vector4 zero = Vector4.zero;
            for (int j = 0; j < LAYERS_RESOLUTION; j++) {
                layersTextureColors[j] = colorNull;
                layersTextureColors2[j] = colorNull;
                layersTextureColorsTemp[j] = colorNull;
                layersProperties[j] = zero;
                layersSum[j] = 0;
            }

            float level = 0;
            for (int k = 0; k < sortedLayersCount; k++) {
                float layerBaseLevel = _liquidLayers[k].currentBaseLevel;
                float layerAmount = _liquidLayers[k].currentAmount;
                if (layerAmount + layerBaseLevel > 1f) {
                    layerAmount = 1f - layerBaseLevel;
                    if (layerAmount < 0)
                        layerAmount = 0;
                }
                if (level < layerBaseLevel + layerAmount)
                    level = layerBaseLevel + layerAmount;
            }

            if (level != _level) {
                _level = level;
                UpdateLevels();
            }

            int y = 0, y_next = 0;
            for (int i = 0; i < sortedLayersCount; i++) {
                int k = sortedLayers[i];
                Color layerColor = ApplyGlobalAlpha(_liquidLayers[k].currentColor);
                Color layerColor2 = ApplyGlobalAlpha(_liquidLayers[k].currentColor2);
                float alpha = layerColor.a;
                layerColor *= alpha;
                layerColor2 *= alpha;
                float layerMurkiness = _liquidLayers[k].currentMurkiness * alpha;
                float layerScale = _liquidLayers[k].mixedScale * 10.0f;
                float layerBaseLevel = _liquidLayers[k].currentBaseLevel * _levelMultiplier;
                float layerAmount = _liquidLayers[k].currentAmount * _levelMultiplier;
                float layerTurbulence = _liquidLayers[k].viscosity * alpha;
                float layerBubblesOpacity = _liquidLayers[k].bubblesOpacity * alpha;
                y = (int)((levelMultipled - layerBaseLevel) * LAYERS_RESOLUTION);
                y_next = (int)((levelMultipled - layerBaseLevel - layerAmount) * LAYERS_RESOLUTION);
                for (int j = y_next; j < y; j++) {
                    if (j >= 0 && j < LAYERS_RESOLUTION) {
                        layersTextureColors[j] += layerColor;
                        layersTextureColors2[j] += layerColor2;
                        layersProperties[j].x += layerMurkiness;
                        layersProperties[j].y = layerScale > layersProperties[j].y ? layerScale : layersProperties[j].y;
                        layersProperties[j].z += layerTurbulence;
                        layersProperties[j].w += layerBubblesOpacity;
                        layersSum[j] += alpha;
                    }
                }
            }

            for (int j = 0; j < LAYERS_RESOLUTION; j++) {
                float m = layersSum[j];
                if (m > 0) {
                    layersTextureColors[j] /= m;
                    layersTextureColors2[j] /= m;
                    layersProperties[j].x /= m;
                    layersProperties[j].z /= m;
                    layersProperties[j].w /= m;
                }
            }

            // Smooth contact surfaces
            if (_smoothContactSurface > 0) {
                for (int j = 0; j < LAYERS_RESOLUTION; j++) {
                    Color sum = colorNull;
                    int count = 0;
                    for (int k = j - _smoothContactSurface; k <= j + _smoothContactSurface; k++) {
                        if (k >= 0 && k < LAYERS_RESOLUTION) {
                            sum += layersTextureColors[k];
                            count++;
                        }
                    }
                    layersTextureColorsTemp[j] = sum / count;
                }
                // Blit colors back
                for (int j = 0; j < LAYERS_RESOLUTION; j++) {
                    layersTextureColors[j] = layersTextureColorsTemp[j];
                }
            }


            UploadMaterialArrays();

#if UNITY_WEBGL
            SetLayersTextures();
#else
            if (FORCE_GLES_COMPATIBILITY) {
                SetLayersTextures();
            }
#endif
        }

        void UploadMaterialArrays() {
            liqMat.SetColorArray(ShaderParams.LayersColorArray, layersTextureColors);
            liqMat.SetColorArray(ShaderParams.LayersColor2Array, layersTextureColors2);
            liqMat.SetVectorArray(ShaderParams.LayersProperties, layersProperties);
        }

        void SetLayersTextures() {
            if (layersPropertiesTex == null) {
                layersPropertiesTex = new Texture2D(1, 256, TextureFormat.RGBAFloat, false, true);
                layersPropertiesTex.filterMode = FilterMode.Point;
                layersPropertiesTex.wrapMode = TextureWrapMode.Clamp;
            }
            if (layersColorsTex == null) {
                layersColorsTex = new Texture2D(1, 256, TextureFormat.ARGB32, false);
                layersColorsTex.filterMode = FilterMode.Point;
                layersColorsTex.wrapMode = TextureWrapMode.Clamp;
            }
            if (layersPropertiesTexContents == null || layersPropertiesTexContents.Length < 256) {
                layersPropertiesTexContents = new Color[256];
            }
            if (layersColorsTexContents == null || layersColorsTexContents.Length < 256) {
                layersColorsTexContents = new Color[256];
            }
            if (layersColors2Tex == null) {
                layersColors2Tex = new Texture2D(1, 256, TextureFormat.ARGB32, false);
                layersColors2Tex.filterMode = FilterMode.Point;
                layersColors2Tex.wrapMode = TextureWrapMode.Clamp;
            }
            if (layersColors2TexContents == null || layersColors2TexContents.Length < 256) {
                layersColors2TexContents = new Color[256];
            }
            for (int k = 0; k < 256; k++) {
                layersPropertiesTexContents[k].r = layersProperties[k].x;
                layersPropertiesTexContents[k].g = layersProperties[k].y;
                layersPropertiesTexContents[k].b = layersProperties[k].z;
                layersPropertiesTexContents[k].a = layersProperties[k].w;
                layersColorsTexContents[k] = layersTextureColors[k];
                layersColors2TexContents[k] = layersTextureColors2[k];
            }
            layersPropertiesTex.SetPixels(layersPropertiesTexContents);
            layersPropertiesTex.Apply();
            liqMat.SetTexture(ShaderParams.LayersPropertiesTex, layersPropertiesTex);
            layersColorsTex.SetPixels(layersColorsTexContents);
            layersColorsTex.Apply();
            liqMat.SetTexture(ShaderParams.LayersColorsTex, layersColorsTex);
            layersColors2Tex.SetPixels(layersColors2TexContents);
            layersColors2Tex.Apply();
            liqMat.SetTexture(ShaderParams.LayersColors2Tex, layersColors2Tex);
        }


        public void Redraw() {
            if (_liquidLayers != null) {
                for (int k = 0; k < _liquidLayers.Length; k++) {
                    _liquidLayers[k].baseLevel = 0;
                    _liquidLayers[k].currentAmount = 0;
                    _liquidLayers[k].currentBaseLevel = 0;
                }
                requireLayersUpdate = true;
            }
            UpdateMaterialProperties();
        }

        #endregion


        #region Mesh Displacement

        void CheckMeshDisplacement() {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) {
                originalMesh = null;
                return;
            }
            if (!_fixMesh) {
                RestoreOriginalMesh();
                originalMesh = null;
                return;
            }

            Mesh currentMesh = meshFilter.sharedMesh;
            if (_autoCloseMesh && currentMesh == closedMesh) return;
            if (!_autoCloseMesh && currentMesh == fixedMesh) return;

            // Backup original mesh
            if (originalMesh == null || !(originalMesh.name.Equals(currentMesh.name))) {
                originalMesh = meshFilter.sharedMesh;
            }

            if (currentMesh != originalMesh) {
                RestoreOriginalMesh();
            }

            Vector3 pos = transform.localPosition;
            CenterPivot(_pivotOffset, _autoCloseMesh);
            originalPivotOffset = transform.localPosition - pos;
        }

        void RestoreOriginalMesh() {
            fixedMesh = closedMesh = null;
            if (originalMesh == null) return;

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) return;

            meshFilter.sharedMesh = originalMesh;
            transform.localPosition -= originalPivotOffset;
            RefreshMeshAndCollider();
        }

        #endregion

        #region Bubbles support

        void AddBubbles(Texture3D tex) {

            if (tex == null)
                return;

            if (_bubblesSizeMax < _bubblesSizeMin) {
                _bubblesSizeMax = _bubblesSizeMin;
            }
            int size = tex.width;

            Array.Copy(colors3D[currentNoiseVariation], colors3DWithBubbles[currentNoiseVariation], colors3DWithBubbles[currentNoiseVariation].Length);
            Color[] colors = colors3DWithBubbles[currentNoiseVariation];

            // Clear bubble buffer
            for (int k = 0; k < colors.Length; k++) {
                colors[k].g = 0.5f;
                colors[k].b = 0.5f;
                colors[k].a = 0.5f;
            }

            // Save current random state
            Random.InitState(_bubblesSeed);

            // Mark bubble positions
            Vector3 nrm = Vector3.zero;
            for (int k = 0; k < _bubblesAmount; k++) {
                int bubbleSize = Random.Range(_bubblesSizeMin, _bubblesSizeMax + 1);
                float bubbleSizeSqr = (float)(bubbleSize * bubbleSize);
                int y = Random.Range(bubbleSize, size - bubbleSize);
                int z = Random.Range(bubbleSize, size - bubbleSize);
                int x = Random.Range(bubbleSize, size - bubbleSize);
                for (int y2 = y - bubbleSize; y2 <= y + bubbleSize; y2++) {
                    float dy = y2 - y;
                    for (int z2 = z - bubbleSize; z2 <= z + bubbleSize; z2++) {
                        float dz = z2 - z;
                        for (int x2 = x - bubbleSize; x2 <= x + bubbleSize; x2++) {
                            float dx = x2 - x;
                            float dSqr = dy * dy + dz * dz + dx * dx;
                            float d = 1f - dSqr / bubbleSizeSqr;
                            if (d >= 0) {
                                float l = (float)System.Math.Sqrt(dSqr);
                                nrm.x = dx / l;
                                nrm.y = dy / l;
                                nrm.z = dz / l;
                                nrm.x = (nrm.x + 1f) * 0.5f;
                                nrm.y = (nrm.y + 1f) * 0.5f;
                                nrm.z = (nrm.z + 1f) * 0.5f;
                                int c = y2 * size * size + z2 * size + x2;
                                colors[c].g = nrm.x;
                                colors[c].b = nrm.y;
                                colors[c].a = nrm.z;
                            }
                        }
                    }
                }
            }

#if UNITY_WEBGL
			bool useUnwrappedTexture = true;
#else
            bool useUnwrappedTexture = FORCE_GLES_COMPATIBILITY;
#endif

            if (useUnwrappedTexture) {

                if (noise3DUnwrapped == null) {
                    noise3DUnwrapped = new Texture2D(size, size * size, TextureFormat.RGBA32, false, true);
                }
                noise3DUnwrapped.SetPixels(colors);
                noise3DUnwrapped.Apply();
                liqMat.SetTexture(ShaderParams.NoiseTexUnwrapped, noise3DUnwrapped);
            }

            // Push colors to texture
            tex.SetPixels(colors);
            tex.Apply();
        }


        void ClearBubbles(Texture3D tex) {

            if (tex == null)
                return;

            Array.Copy(colors3D[currentNoiseVariation], colors3DWithBubbles[currentNoiseVariation], colors3DWithBubbles[currentNoiseVariation].Length);
            Color[] colors = colors3DWithBubbles[currentNoiseVariation];

            // Clear bubble buffer
            for (int k = 0; k < colors.Length; k++) {
                colors[k].g = 0.5f;
                colors[k].b = 0.5f;
                colors[k].a = 0.5f;
            }

            // Push colors to texture
            tex.SetPixels(colors);
            tex.Apply();

            liqMat.SetTexture(ShaderParams.NoiseTexUnwrapped, null);
        }

        #endregion

        #region FP Render Texture comp setting

        public void CheckFPRenderTextureSetting() {
#if UNITY_EDITOR
            if (Time.frameCount != fpRenderTextureCheckFrame) {
                fpRenderTextureCheckFrame = Time.frameCount;
                Shader shader = Shader.Find("LiquidVolume/ZWriteBack");
                if (shader == null) return;
                string path = AssetDatabase.GetAssetPath(shader);
                path = System.IO.Path.GetDirectoryName(path) + "/LVLiquidPassBase.cginc";
                string[] lines = System.IO.File.ReadAllLines(path, System.Text.Encoding.UTF8);
                for (int k = 0; k < lines.Length; k++) {
                    if (lines[k].Contains(SHADER_KEYWORD_FP_RENDER_TEXTURE)) {
                        if (lines[k].TrimStart().StartsWith("//")) {
                            useFPRenderTextures = false;
                        } else {
                            useFPRenderTextures = true;
                        }
                        break;
                    }
                }
            }
#endif
            CleanupCommandBuffer();
        }


        #endregion


    }
}
