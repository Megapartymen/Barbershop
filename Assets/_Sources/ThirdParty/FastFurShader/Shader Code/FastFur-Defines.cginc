//--------------------------------------------------------------------------------
// We are re-using some un-used post-processing keywords for our own features
//
#if defined (FXAA)
#define FASTFUR_TWOSIDED
#endif

#if defined (FXAA_LOW)
#define FASTFUR_MAX16
#endif

#if defined (FXAA_KEEP_ALPHA)
#define FASTFUR_MAX24
#endif

//--------------------------------------------------------------------------------
// Global quality and rendering settings
//
// If you are looking through the code and are thinking of tweaking these settings,
// be aware that I've optimized things for the average VR Chat public lobby or large
// group meet-up. Also, changes here might break things, and possibly crash people!
// If you are changing these settings, please only use the results in private lobbies
// with friends whom you have warned that you are wearing something experimental.


// This is the base target number of shells, before distance, angle, quality, etc...
// are all factored in. Speed and quality are basically interchangable, meaning if
// I can write faster code, I can increase the number of shells to spend that speed on
// extra quality. The opposite is also true, where if I can write an algorithm
// that looks better, I can lower the number of shells to spend some of that quality
// on more speed.
// 
// If the net gains are high enough to make the shader both faster and better looking,
// I keep the new code. Otherwise I revert it.
// 
// The rule I go with is that any speed/quality gains between releases will be split
// 50-50 between making the shader both faster and better looking than the previous
// version. I do that by tweaking this number.
#define BASE_SHELL_COUNT 38


// The chosen pipeline is stored in a separate include file, so that it can be easily
// set by the GUI. Yes, I could use a shader_feature keyword, but I would rather keep
// my shader keyword-free until I better understand how that stuff really works and
// why VR Chat complains about them.
#include "FastFur-Pipeline.cginc"


// "Fallback Pipeline"
// Pipeline 1 is a "normal" geometry shader. There is no hull or domain shader, so all
// early culling happens in the geometry shader, which isn't ideal. This is the slowest
// pipeline with the lowest render quality, but it should be compatible with pretty much
// anything. Despite its drawbacks, it's still dramatically faster than other fur shaders.
#if defined (PIPELINE1)
// Because Pipeline 1 is so slow, we need to cut the render quality in half
#define LAYER_DENSITY 0.5

#define MIN_SHELLS 1.0
#define MID_SHELLS_FACTOR 0.6

#define HULL_SCREEN_CULLING 1.0
#define HULL_BACKFACE_CULLING 0.25
#endif


// "Turbo Pipeline"
// Pipeline 2 uses the hull + domain shader for culling geometry before it reaches the
// geometry shader. The speed boost over Pipeline 1 is dramatic, varying from ~20% faster
// up close to roughly double the speed when far away. This version has been a real
// workhorse, proving to be reliable. It's the version that almost everyone has been using.
#if defined (PIPELINE2)
#define LAYER_DENSITY 1.0

#define MIN_SHELLS 1.0
#define MID_SHELLS_FACTOR 0.75

#define HULL_SCREEN_CULLING 1.0
#define HULL_BACKFACE_CULLING 0.25
#endif


// Pipelines 1 + 2 have fixed layer limits that can be set by the user.
#if defined (PIPELINE1) || defined (PIPELINE2)
#if defined (LITEFUR) || defined (FASTFUR_MAX16)
#define GEOM_INSTANCES 2
#define GEOM_SHELLSPERINSTANCE 8
#define GEOM_MAXVERTS 24
#define GEOM_START_SHELL 1
#define GEOM_STOP_SHELL 16
#else
#if defined (FASTFUR_MAX24)
#define GEOM_INSTANCES 6
#define GEOM_SHELLSPERINSTANCE 4
#define GEOM_MAXVERTS 12
#define GEOM_START_SHELL 1
#define GEOM_STOP_SHELL 24
#else
#define GEOM_INSTANCES 8
#define GEOM_SHELLSPERINSTANCE 4
#define GEOM_MAXVERTS 12
#define GEOM_START_SHELL 1
#define GEOM_STOP_SHELL 32
#endif
#endif
#endif


// "Super Pipeline"
// Pipeline 3 uses the hull + domain shader for both culling and to generate fur layers.
// It's ~20% faster than Pipeline 2, and because it can generate layers on-the-fly, without
// reserving memory beforehand, it also has no real limit on resolution. It's the fastest
// and best in every way except for the fact that AMD drivers like to crash. Because Pipeline
// 3 allocates and frees memory as-needed, my theory is that the AMD memory-manager is buggy,
// resulting in corruption and crashing.
#if defined (PIPELINE3)
#define LAYER_DENSITY 1.0

#define MIN_SHELLS 1.0
#define MID_SHELLS_FACTOR 0.4375

#define HULL_SCREEN_CULLING 1.0
#define HULL_BACKFACE_CULLING 0.35

#define PIPELINE3_CHUNKSIZE 4.0
#define PIPELINE3_CHUNKSCALE 0.25
#define PIPELINE3_CHUNKGEOM 12
#endif


// When the fur is far away, where should the skin be?
#define SKIN_CUTOFF 0.4


//--------------------------------------------------------------------------------
// Detect VR
#if defined(UNITY_SINGLE_PASS_STEREO) || defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define USING_STEREO_MATRICES
#endif


//--------------------------------------------------------------------------------
// Custom samplers
SamplerState my_point_repeat_sampler;


//--------------------------------------------------------------------------------
// Main Skin Settings
sampler2D _MainTex;
float _SelectedUV;
float4 _Color;
float _Cutoff;
float _HideSeams;
float _TiltEdges;
float _TwoSided;
float4 _BackfaceColor;
float4 _BackfaceEmission;

// Supplied by VR Chat
float _VRChatCameraMode;
float _VRChatMirrorMode;

// Quality Settings
float _V4QualityEditor;
float _V4QualityVR;
float _V4Quality2D;
float _V4QualityVRMirror;
float _V4Quality2DMirror;
float _V4QualityCameraView;
float _V4QualityStreamCamera;
float _V4QualityCameraPhoto;
float _V4QualityScreenshot;


// Physically Based Shading
#if defined(SKIN_LAYER)
float _PBSSkin;

#if defined(_NORMALMAP)
sampler2D _BumpMap;
float _BumpScale;
#endif

#if defined(_METALLICGLOSSMAP)
sampler2D _MetallicGlossMap;
#endif

#if defined(_SPECGLOSSMAP)
sampler2D _SpecGlossMap;
#endif

sampler2D _OcclusionMap;
float _OcclusionStrength;

float _Metallic;
float _Glossiness;
float _GlossMapScale;
#endif

// MatCap
#if defined(SKIN_LAYER)
float _MatcapEnable;
sampler2D _Matcap;
float4 _MatcapColor;
sampler2D _MatcapMask;
float _MatcapTextureChannel;
float _MatcapAdd;
float _MatcapReplace;
float _MatcapEmission;
float _MatcapSpecular;
#endif


//--------------------------------------------------------------------------------
// Fur Shaping
Texture2D _FurShapeMap;
SamplerState sampler_FurShapeMap;
float4 _FurShapeMap_TexelSize;

Texture2D _FurShapeMask1;
Texture2D _FurShapeMask2;

uint _FurShapeMask1Bits;
uint _FurShapeMask2Bits;

float _FurShellSpacing;
float _FurMinHeight;
float _BodyShrinkOffset;
float _BodyExpansion;
float _BodyResizeCutoff;

float _FurCombStrength;
//float _FurCombCompression;
float _FurClipping;

float _HairDensityThreshold;

float _ScaleCalibration;


//--------------------------------------------------------------------------------
// Hairs
Texture2D _HairMap;
SamplerState sampler_HairMap;
float4 _HairMap_TexelSize;

Texture2D _HairMapCoarse;
float _CoarseMapActive;

float _HairMapAlphaFilter;
float _HairMapMipFilter;
float _HairMapMediumAlphaFilter;
float _HairMapMediumMipFilter;
float _HairMapMediumStrength;
float _HairMapCoarseAlphaFilter;
float _HairMapCoarseMipFilter;
float _HairMapCoarseStrength;

float _HairSharpen;
float _HairBlur;

float4 _HairMapScaling3;
float4 _HairMapScaling4;
float4 _HairMapScaling5;

float _HairCurlsActive;
float _HairCurlXWidth;
float _HairCurlYWidth;
float _HairCurlXTwists;
float _HairCurlYTwists;
float _HairCurlXYOffset;

float _HairHighlights;
float _HairColourShift;

float _AdvancedHairColour;
float4 _HairRootColour;
float4 _HairMidColour;
float4 _HairTipColour;
float _HairRootAlbedo;
float _HairMidAlbedo;
float _HairTipAlbedo;
float _HairRootMarkings;
float _HairMidMarkings;
float _HairTipMarkings;
float _HairColourMinHeight;

float _HairRootPoint;
float _HairMidLowPoint;
float _HairMidHighPoint;
float _HairTipPoint;

float _HairDensity;

float _HairClipping;


//--------------------------------------------------------------------------------
// Fur Markings
sampler2D _MarkingsMap;

float4 _MarkingsColour;
float _FurMarkingsActive;

float _MarkingsHeight;
float _MarkingsVisibility;
float _MarkingsDensity;
float _MarkingsContrast;
float _Sin;
float _Cos;

float _MarkingsMapPositiveCutoff;
float _MarkingsMapNegativeCutoff;


//--------------------------------------------------------------------------------
// Lighting
#if !defined (PREPASS)
sampler2D _EmissionMap;
float3 _EmissionColor;
float _EmissionMapStrength;
float _AlbedoEmission;


float _ExtraLightingEnable;
float _ExtraLighting;
float _ExtraLightingRim;
float3 _ExtraLightingColor;
float _ExtraLightingMode;

float _FallbackLightEnable;
float3 _FallbackLightColor;
float _FallbackLightStrength;
float _FallbackLightDirection;
float _FallbackLightAngle;

float _MaxBrightness;
float _SoftenBrightness;
float3 _WorldLightReColour;
float _WorldLightReColourStrength;

float _FurShadowCastSize;
float _SoftenShadows;

float _LightPenetrationDepth;
float _DeepFurOcclusionStrength;
float _ProximityOcclusion;
float _ProximityOcclusionRange;
float _LightWraparound;
float _SubsurfaceScattering;

float _FurAnisotropicEnable;
float _FurAnisotropicReflect;
float _FurAnisoReflectAngle;
float _FurAnisoReflectGloss;
float _FurAnisoReflectMetallic;
float4 _FurAnisotropicReflectColor;
float4 _FurAnisotropicReflectColorNeg;
float4 _FurAnisotropicReflectColorPos;
float _FurAnisoReflectIridescenceStrength;
float _FurAnisoReflectEmission;
float _FurAnisoWindShimmer;
float _FurAnisoFlat;

float _FurAnisotropicRefract;
float _FurAnisoRefractAngle;
float _FurAnisoRefractGloss;
float _FurAnisoRefractMetallic;
float4 _FurAnisotropicRefractColor;
float _FurAnisoRefractEmission;
float _FurAnisoDepth;
float _FurAnisoSkin;
#endif

//--------------------------------------------------------------------------------
// Toon Shading
#if !defined (PREPASS)
float _ToonShading;
float4 _ToonColour1;
float4 _ToonColour2;
float4 _ToonColour3;
float4 _ToonColour4;
float4 _ToonColour5;
float4 _ToonColour6;
float4 _ToonColour7;
float4 _ToonColour8;
float4 _ToonColour9;

float _ToonHueRGB;
float _ToonHueGBR;
float _ToonHueBRG;
float _ToonHueRBG;
float _ToonHueGRB;
float _ToonHueBGR;
float _ToonBrightness;
float _ToonWhiten;

float _ToonLighting;
float4 _ToonLightingHigh;
float4 _ToonLightingMid;
float4 _ToonLightingShadow;
float _ToonLightingBlend;
float _ToonLightingHighLevel;
float _ToonLightingHighSoftEdge;
float _ToonLightingShadowLevel;
float _ToonLightingShadowSoftEdge;
#endif


//--------------------------------------------------------------------------------
// Dynamic Movements
float _HairStiffness;
float _FurGravitySlider;
UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
float _CameraProximityTouch;
float _FurContactStrength;
float _EnableWind;
float _WindSpeed;
float _WindDirection;
float _WindAngle;
float4 _WindDirectionActual;
float _WindTurbulenceStrength;
float _WindGustsStrength;
float _MovementStrength;
float _VelocityX;
float _VelocityY;
float _VelocityZ;


//--------------------------------------------------------------------------------
// Debugging
#if defined(FASTFUR_DEBUGGING)
uint _FurDebugDistance;
uint _FurDebugTopLayer;
uint _FurDebugUpperLimit;
uint _FurDebugDepth;
uint _FurDebugVerticies;
uint _FurDebugMipMap;
uint _FurDebugHairMap;
uint _FurDebugLength;
uint _FurDebugDensity;
uint _FurDebugCombing;
uint _FurDebugQuality;
#endif

int _DebuggingLog;// This is here so I have an easy toggle to use during testing

float _OverrideScale;
float _OverrideQualityBias;
float _OverrideDistanceBias;
float _TS1;
float _TS2;
float _TS3;


//--------------------------------------------------------------------------------
// Pack a bunch of attributes into different channels.
#define ZDATA uv.z
#define FURFADEIN uv.a
#define VIEWDISTANCE windEffect.a

// These are only needed in the hull and geometry shaders.
#define SAMPLE_Z worldNormal.a
#define MAX_Z furData.a
#define SKIN_Z furData.x
#define WORLDTHICKNESS furData.y
#define FURCULLTEST furData.z

// Lighting information.
#define MAINLIGHTDOT lightData1.a
#define ANISOTROPICBOTH lightData2.xy
#define ANISOTROPIC1REFLECT lightData2.x
#define ANISOTROPIC2REFRACT lightData2.y
#define ANISOTROPICANGLE lightData2.w
#define SUBSURFACESTRENGTH lightData2.z

// Required in the geometry shader, but only needed in the fragment shader if the debugging
// is active and we want to see how many layers are being rendered.
#define VISIBLELAYERS worldPos.a

// Only needed for debugging
#define LAYERNUMBER vertDist.a

// The hull shader needs the screenpos, but the geometry shader doesn't, so we can reuse the field
#define HULLCOORDS screenPosMin


//--------------------------------------------------------------------------------
// The structure for the vertex shader input data
struct meshData
{
	float4 vertex : POSITION;
	float3 normal : NORMAL;
#if !defined (PREPASS)
	float4 tangent : TANGENT;
#endif
	float2 uv0 : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
	float2 uv3 : TEXCOORD3;

	UNITY_VERTEX_INPUT_INSTANCE_ID
};


//--------------------------------------------------------------------------------
// In hullGeomInput doesn't have a 'pos' field, so in order to use the Unity
// shadow macros we need a temporary structure to get the shadow data
#if defined(USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE))
struct shadowStruct
{
	float4 pos : SV_POSITION;
	UNITY_SHADOW_COORDS(0)
};
#endif


//--------------------------------------------------------------------------------
// The structure for the hull, domain, and geometry shaders' input data
struct hullGeomInput
{
	float4 worldPos : TEXCOORD0;
	float4 worldNormal : TEXCOORD1;

	float2 screenPosMin : TEXCOORD2;
	float2 screenPosMax : TEXCOORD3;

	float4 uv : TEXCOORD4;
	float4 furData : TEXCOORD5;

	float4 windEffect : TEXCOORD6;

#if !defined (PREPASS)
	float4 lightData1 : TEXCOORD7;
	float4 lightData2 : TEXCOORD8;
#endif

#if defined(USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE))
	UNITY_SHADOW_COORDS(9)
#endif

		UNITY_VERTEX_OUTPUT_STEREO
};


// The structure for the fragment shader input data
struct fragInput
{
	float4 pos : SV_POSITION;
#if !defined (PREPASS) && defined(FASTFUR_DEBUGGING)
	float4 worldPos : TEXCOORD0;
#else
	float3 worldPos : TEXCOORD0;
#endif

	float4 uv : TEXCOORD1;

#if !defined (PREPASS)
	centroid float4 lightData1 : TEXCOORD2;// We need to use 'centroid' interpolation, otherwise MSAA causes pixel 'fireflies'
	float4 lightData2 : TEXCOORD3;

#if defined(SKIN_LAYER)
	float3 worldNormal : TEXCOORD4;
#if defined(_NORMALMAP)
	float4 worldTangent : TEXCOORD5;
#endif
#endif

#if defined(FASTFUR_DEBUGGING) && !defined(SKIN_LAYER)
	float4 vertDist : TEXCOORD6;
#endif

	UNITY_FOG_COORDS(7)

#if defined(USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE))
		UNITY_SHADOW_COORDS(8)
#endif
#endif

		UNITY_VERTEX_OUTPUT_STEREO
};
