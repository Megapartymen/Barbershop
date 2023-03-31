Shader "Warren's Fast Fur/Fast Fur - Lite"
{
	Properties
	{
		[Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2


		//--------------------------------------------------------------------------------
		// Main Skin Settings
		[ToggleUI] _MainMapsGroup("Main Render Settings", Int) = 0
		[Enum(Opaque,0,Cutout,1)] _Mode("Render Mode", Float) = 0
		[NOSCALEOFFSET] _MainTex("Albedo Map", 2D) = "white" {}
		[ToggleUI] _HideSeams("Aggressively Hide Seams", Int) = 0
		_Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
		[Enum(UV0,0,UV1,1,UV2,2,UV3,3)] _SelectedUV("UV", Int) = 0
		[HDR]_Color("Albedo Colour", Color) = (1,1,1,1)
		[HDR]_Colour("(obsolete)", Color) = (-1,-1,-1,-1)

		// Quality Settings
		[ToggleUI] _QualityGroup("Quality Settings", Int) = 0
		[Enum(Maximum 16 Fur Layers,0,Maximum 24 Fur Layers,1,Maximum 32 Fur Layers,2)] _MaximumLayers("Maximum Fur Layers", Int) = 1
		// Heya! If you're going to be editing this file and turning VR Mode quality up past "Fast", please don't
		// use the resulting avatars in public lobbies. Please stick with friends that you know have solid, VR-capable rigs.
		// 
		// The "Fast" VR quality has been calibrated to be close to the limits of a Vega 64 card, which is a moderately popular, 5-year
		// old "high-end" (at the time) card, as of Oct 2022. It's roughly equal to a 3060.
		//
		// The shader has an exponential distance falloff, so groups of people wearing it that are spread out will cause only a small
		// performance hit. However, there is a small radius around you that will make lower-spec players choppy. It might be
		// elbow-length, or nose-lenth, or arms-length, depending on how powerful their system is. On my test system, running the
		// Standard version of the shader on "Fast", the Vega 64 gets choppy (but still playable) when I'm nose-to-nose in a mirror.
		[Enum(Fastest 100,0,Very Fast 125,1,Fast 150,2,Medium 200,3,Slow 300,5,Very Slow 500,9,Slowest 800,15)] _V4QualityEditor("Unity Editor Quality", Int) = 5
		[Enum(Fastest 100,0,Very Fast 125,1,Fast 150,2)] _V4QualityVR("VR Quality", Int) = 1
		[Enum(Fastest 100,0,Very Fast 125,1,Fast 150,2,Medium 200,3)] _V4Quality2D("Desktop Quality", Int) = 2
		[Enum(Fastest 100,0,Very Fast 125,1,Fast 150,2)] _V4QualityVRMirror("Mirror Quality (VR)", Int) = 1
		[Enum(Fastest 100,0,Very Fast 125,1,Fast 150,2,Medium 200,3)] _V4Quality2DMirror("Mirror Quality (Desktop)", Int) = 2
		[Enum(Fastest 100,0,Very Fast 125,1,Fast 150,2,Medium 200,3)] _V4QualityCameraView("Camera Viewfinder", Int) = 1
		[Enum(Fastest 100,0,Very Fast 125,1,Fast 150,2,Medium 200,3,Slow 300,5,Very Slow 500,9)] _V4QualityStreamCamera("Stream Camera", Int) = 2
		[Enum(Fastest 100,0,Very Fast 125,1,Fast 150,2,Medium 200,3,Slow 300,5,Very Slow 500,9,Slowest 800,15)] _V4QualityCameraPhoto("Camera Photo", Int) = 15
		[Enum(Fastest 100,0,Very Fast 125,1,Fast 150,2,Medium 200,3,Slow 300,5,Very Slow 500,9,Slowest 800,15)] _V4QualityScreenshot("Screenshot", Int) = 15


		// Render Pipeline
		//[Enum(Fallback Pipeline,0,Turbo Pipeline,1,Super Pipeline,2)] _RenderPipeline("Fast Fur Render Pipeline", Int) = 1
		[Enum(Fallback Pipeline,0,Turbo Pipeline,1)] _RenderPipeline("Fast Fur Render Pipeline", Int) = 1
		_ConfirmPipeline("Are you sure?", Int) = 0


		//--------------------------------------------------------------------------------
		// Physically Based Shading
		[ToggleUI] _PBSSkinGroup("Skin - Advanced Material Settings", Int) = 0
		_PBSSkin ("PBS Skin Depth Visibility", Range(0, 1)) = 0.1
		[NOSCALEOFFSET]_BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Strength", Range(0, 10)) = 1
		[NOSCALEOFFSET] _MetallicGlossMap("Metallic", 2D) = "black" {}
		[NOSCALEOFFSET] _SpecGlossMap("Specular", 2D) = "black" {}
		_Metallic("Metallic", Range(0, 1)) = 0
		_Glossiness("Smoothness", Range(0, 1)) = 0.7
		_GlossMapScale("Smoothness Source Scale", Range(0.0, 1.0)) = 0.7
		[Enum(Use Metallic (A),0,Use Albedo Map (A),1)] _SmoothnessTextureChannel ("Smoothness Source", Float) = 0
		[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
		[ToggleOff] _GlossyReflections("Reflections", Float) = 0.0
		[NOSCALEOFFSET]_OcclusionMap ("Occlusion Map", 2D) = "white" {}
		_OcclusionStrength("Occlusion Map Strength", Range(0, 1.0)) = 1.0
		[ToggleUI]_TwoSided("Two-Sided Rendering", Int) = 0
		[HDR]_BackfaceColor("Backface Colour", Color) = (1,1,1,1)
		[HDR]_BackfaceEmission("Backface Emission", Color) = (0,0,0,1)


		//--------------------------------------------------------------------------------
		// MatCap
		[ToggleUI] _MatcapGroup("MatCap", Int) = 0
		[ToggleUI]_MatcapEnable ("Enable MatCap", Int) = 0
		[NOSCALEOFFSET]_Matcap("MatCap Texture", 2D) = "white" { }
		_MatcapColor("MatCap Map Colour", Color) = (1, 1, 1, 1)
		[Enum(No Mask,0,Use MatCap Mask Map (R),1,Use MatCap Texture (A),2,Use Albedo Map (A),3,Use Metallic (A),4,Use Metallic (R),5)] _MatcapTextureChannel ("MatCap Mask Source", Float) = 0
		[NOSCALEOFFSET]_MatcapMask("MatCap Mask Map", 2D) = "white" { }
		_MatcapFurMask("Blend Add Strength", Range(0, 1)) = 0
		_MatcapAdd("Blend Add Strength", Range(0, 1)) = 0
		_MatcapReplace("Blend Replace Strength", Range(0, 1)) = 0
		_MatcapEmission("Emission Strength", Range(0, 2)) = 0
		_MatcapSpecular("Specularity", Range(0, 1)) = 1


		//--------------------------------------------------------------------------------
		// Fur Shaping
		[ToggleUI] _FurShapeGroup("Fur Thickness and Overall Shape", Int) = 0
		[NOSCALEOFFSET]_FurShapeMap("Fur Shape Data Map", 2D) = "grey" {}
		[NOSCALEOFFSET]_FurShapeMask1("Height Masks 1", 2D) = "white" {}
		[NOSCALEOFFSET]_FurShapeMask2("Height Masks 2", 2D) = "white" {}
		_FurShapeMask1Bits("Height Masks 1 Bitfield", Int) = 0
		_FurShapeMask2Bits("Height Masks 2 Bitfield", Int) = 0
		[NOSCALEOFFSET]_FurGroomingMask("Fur Grooming: Copy Map", 2D) = "grey" {}
		_ScaleCalibration("Fur Thickness Calibration", Float) = -1
		_FurShellSpacing("Fur Thickness", Range(0.01, 1)) = 0.5
		_FurMinHeight("Fur Min Height Cutoff", Range(0.0, 1)) = 0.01
		_BodyShrinkOffset("Body Shrink Offset", Range(0, 1)) = 0
		_BodyExpansion("Body Expansion when Far", Range(0, 1)) = 1
		_BodyResizeCutoff("Body Shrink/Expand Cutoff", Range(0, 0.75)) = 0.2
		_FurCombStrength("Combing Strength", Range(0, 1)) = 0.35
		_FurCombCompression("Combing Compression", Range(0, 1)) = 0.35

		// Texture Utilities
		[ToggleUI] _UtilitiesGroup("Fur Shape Data Map Utilities", Int) = 0
		[NOSCALEOFFSET] _UtilitySourceMap("Source Map", 2D) = "black" {}
		[NOSCALEOFFSET] _UtilityTargetMap("Target Map", 2D) = "black" {}

		[Enum(Copy a Channel from Source to Target,0,Apply Source Mask to Target,1,Rescale Combing Strength,2,Rescale Length,3,Rescale Density,4,Fill Target Channel,5)] _UtilityFunction("Operation", Int) = 0
		[ToggleUI] _UtilityInvert("Invert the Channel", Int) = 0
		[Enum(Set Target Channel to 0 if Mask is Below Threshold,0,Set Target Channel to 0 if Mask is Above Threshold,1)] _UtilityMaskType("Mask Type", Int) = 0
		_UtilityMaskThreshold("Mask Threshold", Range(0, 1)) = 0.01
		[Enum(Red (Comb X),0,Green (Comb Y),1,Blue (Length),2,Alpha (Density),3)] _UtilitySourceMask("Source Mask Channel", Int) = 2
		[Enum(Red (Comb X),0,Green (Comb Y),1,Blue (Length),2,Alpha (Density),3)] _UtilitySourceChannel("Source Channel", Int) = 0
		[Enum(Red (Comb X),0,Green (Comb Y),1,Blue (Length),2,Alpha (Density),3)] _UtilityTargetChannel("Target Channel", Int) = 0
		[Enum(256 x 256,0,512 x 512,1,1024 x 1024,2,2048 x 2048,3)] _UtilityNewResolution("New Texture Resolution", Int) = 2
		[PowerSlider(10.0)] _UtilityReScale("Re-Scale Factor", Range(0.5, 2)) = 0.75
		_UtilityValue("Fill Value", Range(0, 1)) = 0


		//--------------------------------------------------------------------------------
		// Hairs
		[ToggleUI] _HairsGroup("Individual Hairs", Int) = 0
		[NOSCALEOFFSET]_HairMap("Hair Pattern Map (Fine)", 2D) = "grey" {}
		[NOSCALEOFFSET]_HairMapCoarse("Hair Pattern Map (Coarse)", 2D) = "black" {}
		_CoarseMapActive("", Int) = 0

		_HairDensity("Hair Density", Range(0.1, 6)) = 3.5
		_HairClipping("Hair Clipping", Range(0, 2)) = 0

		_HairMapCoarseStrength("Coarse Map Strength", Range(0, 2)) = 1

		[Enum(Blurry (Box Filter),0,Sharp (Kaiser Filter),1)] _HairMipType("Texture Filter Type", Int) = 1
		_HairSharpen("Sharpen Hairs", Range(0, 2)) = 0
		_HairBlur("Blur Hairs", Range(0, 2)) = 0.25

		[ToggleUI] _HairsColourGroup("Hair Colouring", Int) = 0
		_HairColourShift("Hair Colour Shift", Range(-5, 5)) = 0.2
		_HairHighlights("Hair Highlights", Range(-5, 5)) = 0.5

		[ToggleUI] _AdvancedHairColour("Enabled Advanced Colouring", Int) = 0
		[HDR]_HairRootColour("Root Colour", Color) = (1,1,1,1)
		[HDR]_HairMidColour("Mid Colour", Color) = (0.8,0.5,0.5,1)
		[HDR]_HairTipColour("Tip Colour", Color) = (0.5,0.4,0.1,1)

		_HairRootAlbedo("Root Albedo Map Strength", Range(0, 1)) = 1.0
		_HairMidAlbedo("Mid Albedo Map Strength", Range(0, 1)) = 1.0
		_HairTipAlbedo("Tips Albedo Map Strength", Range(0, 1)) = 1.0

		_HairRootMarkings("Root Markings Map Strength", Range(0, 1)) = 1.0
		_HairMidMarkings("Mid Markings Map Strength", Range(0, 1)) = 1.0
		_HairTipMarkings("Tip Markings Map Strength", Range(0, 1)) = 1.0

		_HairRootPoint("Root Fade Point", Range(0.000, 0.997)) = 0.5
		_HairMidLowPoint("Middle Low Fade Point", Range(0.001, 0.998)) = 0.6
		_HairMidHighPoint("Middle High Fade Point", Range(0.002, 0.999)) = 0.75
		_HairTipPoint("Tip Fade Point", Range(0.003, 1.000)) = 0.85

		_HairColourMinHeight("Minimum Fur Height", Range(0, 1)) = 0

		_HairCurls("Hair Curls", Range(-1, 1)) = 0
		[ToggleUI] _HairCurlsGroup("Hair Curls", Int) = 0
		[ToggleUI] _HairCurlsActive("Enable Hair Curls", Int) = 0
		[ToggleUI] _HairCurlsLockXY("Lock XY", Int) = 1
		_HairCurlXWidth("X Width", Range(0, 1)) = 0.5
		_HairCurlXTwists("X Twists", Range(1, 5)) = 3
		_HairCurlYTwists("Y Twists", Range(1, 5)) = 3
		_HairCurlYWidth("Y Width", Range(0, 1)) = 0.5
		_HairCurlXYOffset("XY Phase", Range(-45, 135)) = 45

		[ToggleUI] _HairRenderingGroup("Advanced Hair Rendering Adjustments", Int) = 0
		_TiltEdges("Tilt Edge Geometry", Range(0, 1)) = 1.0

		_HairMapAlphaFilter("(Fine) Alpha Re-Calibration", Range(0, 1)) = 1.0
		_HairMapMipFilter("(Fine) Mip-Level Compensation", Range(0, 1)) = 0.2

		_HairMapCoarseAlphaFilter("(Coarse) Alpha Re-Calibration", Range(0, 1)) = 0
		_HairMapCoarseMipFilter("(Coarse) Mip-Level Compensation", Range(0, 1)) = 1.0


		// Hairs Generator
		[ToggleUI] _GenerateHairGroup("Hair Pattern Map Generator", Int) = 0
		
		_GenGuardHairs("# of Guard Hairs", Range(0, 2500)) = 150
		[Enum(Uniform Thickness,0,Slightly Tapered,1,Heavily Tapered,2)]_GenGuardHairsTaper("Guard Hair Shape", Int) = 1
		_GenGuardHairMinHeight("Guard Hair Min Height", Range(0, 1)) = .7
		_GenGuardHairMaxHeight("Guard Hair Max Height", Range(0, 1)) = 1
		_GenGuardHairMinColourShift("Guard Hair Min Colour Shift", Range(-1, 1)) = -1
		_GenGuardHairMaxColourShift("Guard Hair Max Colour Shift", Range(-1, 1)) = 1
		_GenGuardHairMinHighlight("Guard Hair Min Highlight", Range(-1, 1)) = -1
		_GenGuardHairMaxHighlight("Guard Hair Max Highlight", Range(-1, 1)) = 1
		_GenMediumHairs("# of Medium Hairs", Range(0, 5000)) = 250
		[Enum(Uniform Thickness,0,Slightly Tapered,1,Heavily Tapered,2)]_GenMediumHairsTaper("Medium Hair Shape", Int) = 0
		_GenMediumHairMinHeight("Medium Hair Min Height", Range(0, 1)) = .7
		_GenMediumHairMaxHeight("Medium Hair Max Height", Range(0, 1)) = 1
		_GenMediumHairMinColourShift("Medium Hair Min Colour Shift", Range(-1, 1)) = -1
		_GenMediumHairMaxColourShift("Medium Hair Max Colour Shift", Range(-1, 1)) = 1
		_GenMediumHairMinHighlight("Medium Hair Min Highlight", Range(-1, 1)) = -1
		_GenMediumHairMaxHighlight("Medium Hair Max Highlight", Range(-1, 1)) = 1
		_GenFineHairs("# of Fine Hairs", Range(0, 10000)) = 5000
		[Enum(Uniform Thickness,0,Slightly Tapered,1,Heavily Tapered,2)]_GenFineHairsTaper("Fine Hair Shape", Int) = 0
		_GenFineHairMinHeight("Fine Hair Min Height", Range(0, 1)) = .7
		_GenFineHairMaxHeight("Fine Hair Max Height", Range(0, 1)) = 1
		_GenFineHairMinColourShift("Fine Hair Min Colour Shift", Range(-1, 1)) = -1
		_GenFineHairMaxColourShift("Fine Hair Max Colour Shift", Range(-1, 1)) = 1
		_GenFineHairMinHighlight("Fine Hair Min Highlight", Range(-1, 1)) = -1
		_GenFineHairMaxHighlight("Fine Hair Max Highlight", Range(-1, 1)) = 1


		//--------------------------------------------------------------------------------
		// Fur Markings
		[ToggleUI] _MarkingsGroup("Fur Patterns and Markings", Int) = 0
		[NOSCALEOFFSET]_MarkingsMap("Fur Markings Map", 2D) = "grey" {}
		_FurMarkingsActive("", Int) = 1
		[HDR]_MarkingsColour("Fur Markings Colour", Color) = (1,1,1,1)
		_MarkingsContrast("Fur Markings Contrast", Range(1, 4)) = 1
		_MarkingsDensity("Fur Markings Tile Density", Range(0.1, 25)) = 3.85
		_MarkingsRotation("Fur Markings Rotation", Range(0, 360)) = 0
		_MarkingsVisibility("Fur Markings Visibility", Range(0, 1)) = 0.3
		_MarkingsHeight("Fur Markings Height Offset", Range(-0.5, 0.5)) = 0.15
		_Sin("", Float) = 1
		_Cos("", Float) = 0

		// Fur Markings Generator
		[ToggleUI] _GenerateFurGroup("Fur Markings Generator Settings", Int) = 0
		_PigmentColour("Pigment Colour", Color) = (0,0,0,1)
		_BaseColour("Base Colour", Color) = (1,1,1,1)
		_TransitionalColour("Transitional Colour", Color) = (0.5,0.5,0.5,1)
		_Contrast("Contrast", Range(1, 10)) = 3
		_ActivatorHormoneRadius("Activator Hormone Radius", Range(1, 10)) = 3
		_InhibitorHormoneAdditionalRadius("Inhibitor Hormone Additional Radius", Range(0.1, 10)) = 3
		_InhibitorStrength("Inhibitor Strength", Range(0, 1)) = .3
		_InitialDensity("Starting Pigment Density", Range(0, 1)) = .5
		_MutationRate("Mutation Rate",Range(0, 0.5)) = 0.25
		_CellStretch("Cell Stretch", Range(0, 5)) = 0
		_Direction("Stretch Direction", Range(0, 180)) = 0
		[IntRange]_ActivatorCycles("Activator Cycles", Range(1,100)) = 10



		//--------------------------------------------------------------------------------
		// Lighting
		[ToggleUI]_LightingGroup("Lighting and Emission", Int) = 0

		[NOSCALEOFFSET]_EmissionMap("Emission Map", 2D) = "white" {}
		[HDR]_EmissionColor("Emission Colour", Color) = (1,1,1,1)
		[HDR]_EmissionColour("(obsolete)", Color) = (-1,-1,-1,-1)
		_EmissionMapStrength("Emission Map Strength", Range(0.0, 2.0)) = 1
		_AlbedoEmission("Emission from Albedo Map", Range(0.0, 2.0)) = 0

		// World Lighting
		[ToggleUI]_WorldLightGroup("Adjust World Lighting", Int) = 0
		_MaxBrightness("Maximum Light Brightness", Range(0, 10)) = 10
		_SoftenBrightness("Soften Brightness", Range(0, 1)) = 0.5
		_MinBrightness("(obsolete)", Range(0, 10)) = -1
		[HDR]_WorldLightReColour("Light Re-Colour", Color) = (1,1,1)
		_WorldLightReColourStrength("Re-Colour Strength", Range(0, 1)) = 0

		// Extra Light Sources
		[ToggleUI]_ExtraLightingGroup("Add Extra Light Sources", Int) = 0
		[ToggleUI]_FallbackLightEnable("Enable Fallback Directional Light", Int) = 0
		[HDR]_FallbackLightColor("Fallback Light Colour", Color) = (1,1,1,1)
		_FallbackLightStrength("Fallback Light Strength", Range(0, 2.0)) = 0.25
		_FallbackLightDirection("Horizontal Direction", Range(0, 360)) = 45
		_FallbackLightAngle("Vertical Angle", Range(-90, 90)) = -25
		
		[ToggleUI]_ExtraLightingEnable("Enable Extra Ambient Lighting", Int) = 1
		_ExtraLighting("Extra Ambient Brightness", Range(0.0, 2.0)) = 0
		_ExtraLightingRim("Extra Ambient Rim/Front", Range(-10.0, 1)) = 1
		[HDR]_ExtraLightingColor("Extra Ambient Colour", Color) = (1,1,1,1)
		[Enum(Always adds additional light,0,Only adds light in dark areas,1)]_ExtraLightingMode("Extra Ambient Mode", Int) = 0

		// Anisotropic
		[ToggleUI]_AnisotropicGroup("Anisotropic Fur Gloss", Int) = 0
		[ToggleUI]_FurAnisotropicEnable("Enable Anisotropic Lighting", Int) = 1
		_FurAnisotropicReflect("Surface Reflections", Range(0, 2)) = 0.5
		_FurAnisoReflectAngle("Reflection Angle", Range(0, 45)) = 1
		_FurAnisoReflectGloss("Reflection Gloss", Range(0, 1)) = 0.5
		_FurAnisoReflectMetallic("Metallic Reflections", Range(0, 1)) = 0.5
		[HDR]_FurAnisotropicReflectColor("Reflection Base Tint", Color) = (1,1,1,1)
		[HDR]_FurAnisotropicReflectColorNeg("Reflection Red Shift", Color) = (1,0,0,1)
		[HDR]_FurAnisotropicReflectColorPos("Reflection Blue Shift", Color) = (0,0,1,1)
		_FurAnisoReflectIridescenceStrength("Iridescence Strength", Range(0, 20)) = 10
		_FurAnisoReflectEmission("Reflection Emission", Range(0, 2)) = 0
		_FurAnisotropicRefract("Internal Refractions", Range(0, 2)) = 0.5
		_FurAnisoRefractAngle("Refraction Angle", Range(0, 45)) = 6
		_FurAnisoRefractGloss("Refraction Gloss", Range(0, 1)) = 0.5
		_FurAnisoRefractMetallic("Metallic Refractions", Range(0, 1)) = 1.0
		[HDR]_FurAnisotropicRefractColor("Refraction Tint", Color) = (1,1,1,1)
		_FurAnisoRefractEmission("Refraction Emission", Range(0, 2)) = 0
		_FurAnisoDepth("Anisotropy Depth into Fur", Range(0, 1)) = 0.5
		_FurAnisoSkin("Apply Anisotropy to Skin", Range(0, 1)) = 0
		_FurAnisoWindShimmer("Wind Shimmering", Range(0, 0.5)) = 0.1
		_FurAnisoFlat("Flatten Anisotropic Hair Angle", Range(0, 1)) = 0

		// Fur Lighting
		[ToggleUI]_FurLightingGroup("Supplemental Fur Lighting", Int) = 0
		_LightWraparound("Wraparound Sides", Range(0, 10)) = 1
		_SubsurfaceScattering("Subsurface Scattering", Range(0, 1)) = 0.5

		// Occlusion
		[ToggleUI]_OcclusionGroup("Occlusion and Shadow", Int) = 0
		_DeepFurOcclusionStrength("Deep Fur Occlusion Strength", Range(0, 2)) = 0.35
		_LightPenetrationDepth("Light Penetration Depth", Range(0, 1)) = 0.1
		_ProximityOcclusion("Proximity Occlusion Strength", Range(0, 1)) = 0.5
		_ProximityOcclusionRange("Proximity Occlusion Range", Range(0.01, 0.5)) = 0.2

		_FurShadowCastSize("Shadow Casting Size", Range(0, 1.0)) = 0.4
		_SoftenShadows("Soften Shadows", Range(0, 1.0)) = 0


		//--------------------------------------------------------------------------------
		// Toon Shading
		[ToggleUI]_ToonShadingGroup("Toon Shading", Int) = 0
		[ToggleUI]_ToonShading("Enable Toon Shading", Int) = 0

		[ToggleUI]_ToonColoursGroup("Adjust Toon Colours", Int) = 0
		[HDR]_ToonColour1("Toon Colour 1", Color) = (1,1,1,1)
		[HDR]_ToonColour2("Toon Colour 2", Color) = (0.5,0.5,0.5,1)
		[HDR]_ToonColour3("Toon Colour 3", Color) = (0,0,0,1)
		[HDR]_ToonColour4("Toon Colour 4", Color) = (1,0,0,1)
		[HDR]_ToonColour5("Toon Colour 5", Color) = (1,1,0,1)
		[HDR]_ToonColour6("Toon Colour 6", Color) = (0,1,0,1)
		[HDR]_ToonColour7("Toon Colour 7", Color) = (0,1,1,1)
		[HDR]_ToonColour8("Toon Colour 8", Color) = (0,0,1,1)
		[HDR]_ToonColour9("Toon Colour 9", Color) = (1,0,1,1)

		// Toon Lighting
		[ToggleUI]_ToonLightingGroup("Toon Lighting", Int) = 0
		[ToggleUI]_ToonLighting("Enable Toon Lighting", Int) = 0
		[HDR]_ToonLightingHigh("Toon Bright Light", Color) = (1,1,1,1)
		[HDR]_ToonLightingMid("Toon Normal Light", Color) = (0.75,0.75,0.75,1)
		[HDR]_ToonLightingShadow("Toon Shadow", Color) = (0.05,0.05,0.125,1)
		_ToonLightingHighLevel("Toon Bright Level", Range(0, 1)) = 0.7
		_ToonLightingHighSoftEdge("Toon Bright Soft Edge", Range(0, 1)) = 0.175
		_ToonLightingShadowLevel("Toon Shadow Level", Range(0, 1)) = 0.1
		_ToonLightingShadowSoftEdge("Toon Shadow Soft Edge", Range(0, 1)) = 0.45

		[ToggleUI]_ToonPostEffectsGroup("Toon Effects", Int) = 0
		[ToggleUI]_ToonHue("Re-mix Hue", Int) = 0
		_ToonHueRGB("Toon Hue RGB", Range(0, 1)) = 1.0
		_ToonHueGBR("Toon Hue RGB->GBR", Range(0, 1)) = 0.0
		_ToonHueBRG("Toon Hue RGB->BRG", Range(0, 1)) = 0.0
		_ToonHueRBG("Toon Hue RGB->RBG", Range(0, 1)) = 0.0
		_ToonHueGRB("Toon Hue RGB->GRB", Range(0, 1)) = 0.0
		_ToonHueBGR("Toon Hue RGB->BGR", Range(0, 1)) = 0.0
		_ToonBrightness("Toon Brightness Adjust", Range(-1, 1)) = 0.0
		_ToonWhiten("Toon Whitening", Range(0, 1)) = 0.0


		//--------------------------------------------------------------------------------
		// Dynamic Movements
		[ToggleUI] _DynamicsGroup("Gravity, Wind, and Movement", Int) = 0
		_HairStiffness("Flatten Hairs", Range(0, 1)) = 0.5
		_FurGravitySlider("Gravity Strength", Range(0, 1)) = 0.35
		_FurContactStrength("Contact Strength", Range(0, 1)) = 1
		[ToggleUI] _WindGroup("Wind Settings", Int) = 0
		[ToggleUI]_EnableWind("Enable Wind", Int) = 0
		_WindSpeed("Wind Speed", Range(0, 1)) = .35
		_WindDirection("Wind Horizontal Direction", Range(0, 360)) = 90
		_WindAngle("Wind Vertical Angle", Range(-90, 90)) = -10
		_WindTurbulenceStrength("Turbulence", Range(0, 10)) = 1.7
		_WindGustsStrength("Gusts", Range(0, 10)) = 2.5
		
		_CameraProximityTouch("Camera Proximity Touch", Range(0, 1)) = 0.5

		[ToggleUI] _MovementGroup("Movement Settings", Int) = 0
		_MovementStrength("Movement Strength", Range(0, 10)) = 1
		_VelocityX("Velocity X", Range(-1, 1)) = 0
		_VelocityY("Velocity Y", Range(-1, 1)) = 0
		_VelocityZ("Velocity Z", Range(-1, 1)) = 0


		//--------------------------------------------------------------------------------
		// Debugging
		[ToggleUI] _DebuggingGroup("Debugging and Internal Information", Int) = 0
		[ToggleUI] _DebuggingLog("Enable Debugging in Console", Int) = 0

		_MarkingsMapHash("Fur Markings Map Hash", Float) = 0
		_MarkingsMapPositiveCutoff("Fur Markings Map Positive Cutoff", Float) = 0
		_MarkingsMapNegativeCutoff("Fur Markings Map Negative Cutoff", Float) = 0
		_HairDensityThreshold("Hair Density Mid Threshold", Float) = -1
		_HairMapHash("Hair Pattern Map Hash", Float) = 0
		_HairMapScaling3("Hair Mip-Map 3 Scaling", Vector) = (1.4, 1.4, 0.0, 0)
		_HairMapScaling4("Hair Mip-Map 4 Scaling", Vector) = (1.8, 1.8, 0.0, 0)
		_HairMapScaling5("Hair Mip-Map 5 Scaling", Vector) = (2.2, 2.2, 2.2, 0)
		_WindDirectionActual("Wind Direction (Actual)", Vector) = (0,0,1,0)

		[ToggleUI]_FurDebugDistance("Show Distance Shells", Int) = 0
		[ToggleUI]_FurDebugMipMap("Show Mip Map Levels", Int) = 0
		[ToggleUI]_FurDebugHairMap("Show Hair Map Levels", Int) = 0
		[ToggleUI]_FurDebugVerticies("Show Vertices", Int) = 0
		[ToggleUI]_FurDebugTopLayer("Show Top Layer Only", Int) = 0
		[ToggleUI]_FurDebugUpperLimit("Show Upper Limits", Int) = 0
		[ToggleUI]_FurDebugDepth("Show Depth Levels", Int) = 0
		[ToggleUI]_FurDebugQuality("Show Quality Level", Int) = 0
		[ToggleUI]_FurDebugLength("Show Length Map", Int) = 0
		[ToggleUI]_FurDebugDensity("Show Density Map", Int) = 0
		[ToggleUI]_FurDebugCombing("Show Combing Map", Int) = 0

		_OverrideScale("Override Scale (DEFAULT: 1)", Float) = 1
		_OverrideQualityBias("Override Quality Bias (DEFAULT: 0)", Float) = 0
		_OverrideDistanceBias("Override Distance Bias (DEFAULT: 0)", Float) = 0
		_TS1("Test Slider", Range(-1, 1)) = 0
		_TS2("Test Slider", Range(-1, 1)) = 0
		_TS3("Test Slider", Range(-1, 1)) = 0
	}


	SubShader
	{
		//--------------------------------------------------------------------------------
		// The FORWARD_BASE_PASS renders one DIRECTIONAL (only) pixel light, 4 vertex lights, and all SH9 lights.
		// If there is no DIRECTIONAL light, it sets the light colour to all zeros.

		//--------------------------------------------------------------------------------
		// Skin - FORWARD_BASE_PASS
		Pass
		{
			Name "Forward_Skin"
			Tags { "LightMode" = "ForwardBase" "Queue" = "Opaque" }

			Cull [_Cull]
			Blend Off
			ZTest Less

			CGPROGRAM

			#define FORWARD_BASE_PASS

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma shader_feature_local _NORMALMAP
			#pragma shader_feature _EMISSION
			#pragma shader_feature_local _ALPHATEST_ON
			#pragma shader_feature_local _METALLICGLOSSMAP
			#pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
			#pragma shader_feature_local _GLOSSYREFLECTIONS_OFF

			#pragma shader_feature_local FASTFUR_DEBUGGING
			#pragma shader_feature_local FXAA // FASTFUR_TWOSIDED

			#define SKIN_LAYER

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "UnityPBSLighting.cginc"

			#include "FastFur-Functions.cginc"
			#include "FastFur-Defines.cginc"

			#pragma target 4.6

			#pragma vertex vert
			#pragma fragment frag

			#include "FastFur-Vert.cginc"
			#include "FastFur-Frag.cginc"

			ENDCG
		}


		//--------------------------------------------------------------------------------
		// Fur - FORWARD_BASE_PASS
		Pass
		{
			Name "Forward_Fur"
			Tags { "LightMode" = "ForwardBase" }
			
			Cull Off // The hull shader will handle culling
			Blend Off
			ZTest Less

			CGPROGRAM

			#define FORWARD_BASE_PASS

			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma shader_feature _EMISSION
			#pragma shader_feature_local _ALPHATEST_ON

			#pragma shader_feature_local FASTFUR_DEBUGGING

			#define LITEFUR

			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			#include "FastFur-Functions.cginc"
			#include "FastFur-Defines.cginc"

			#pragma target 4.6
			#pragma require geometry

			#pragma vertex vert
			#pragma hull hull
			#pragma domain doma
			#pragma geometry geom
			#pragma fragment frag

			#include "FastFur-Vert.cginc"
#if defined(PIPELINE1)
			#include "FastFur-Geom-V2.cginc"
#endif
#if defined(PIPELINE2)
			#include "FastFur-Hull-V2.cginc"
			#include "FastFur-Geom-V2.cginc"
#endif
#if defined(PIPELINE3)
			#include "FastFur-Hull-V3.cginc"
			#include "FastFur-Geom-V3.cginc"
#endif
			#include "FastFur-Frag.cginc"

			ENDCG
		}



		//--------------------------------------------------------------------------------
		// Shadow pass - Custom
		Pass {
			Name "Fur Shadows"
			Tags { "LightMode" = "ShadowCaster" }

			ZWrite On
			ZTest LEqual

			CGPROGRAM
			#pragma target 3.0

			// -------------------------------------

			#pragma shader_feature_local _ALPHATEST_ON
			#pragma shader_feature_local _METALLICGLOSSMAP
			#pragma shader_feature_local _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
			#pragma multi_compile_shadowcaster
			#pragma multi_compile_instancing

			#pragma vertex vertFurShadowCaster
			#pragma fragment fragShadowCaster

			#define UNITY_STANDARD_USE_SHADOW_UVS

			#include "UnityStandardShadow.cginc"

			float _SelectedUV;
			SamplerState my_point_repeat_sampler;
			Texture2D _FurShapeMap;
			float4 _FurShapeMap_TexelSize;

			Texture2D _FurShapeMask1;
			Texture2D _FurShapeMask2;

			uint _FurShapeMask1Bits;
			uint _FurShapeMask2Bits;

			float _CoarseMapActive;
			float _ScaleCalibration;
			float _OverrideScale;
			float _OverrideDistanceBias;
			float _FurShellSpacing;
			float _FurShadowCastSize;
			float _FurMinHeight;
			float _TS1;
			float _TS2;


			// This is a modified version of the vertShadowCaster from "UnityStandardShadow.cginc"
			void vertFurShadowCaster (VertexInput vi
				, out float4 opos : SV_POSITION
				#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
				, out VertexOutputShadowCaster o
				#endif
				#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
				, out VertexOutputStereoShadowCaster os
				#endif
			)
			{
				UNITY_SETUP_INSTANCE_ID(vi);
				#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(os);
				#endif
				// ----------------------------------------------------
				// This is code from our vert function, copy-pasted into the
				// middle of the Unity Standard Shadow Caster

				// TODO: We should add support for alternate uv maps.
				float2 uv = vi.uv0;
				float furShape = _FurShapeMap.SampleLevel(my_point_repeat_sampler, uv, 0).z;
				float4 furMask1 = float4(1,1,1,1);
				float4 furMask2 = float4(1,1,1,1);
				if(_FurShapeMask1Bits > 0) furMask1 = _FurShapeMask1.SampleLevel(my_point_repeat_sampler, uv, 0);
				if(_FurShapeMask2Bits > 0) furMask2 = _FurShapeMask2.SampleLevel(my_point_repeat_sampler, uv, 0);

				//--------------------------------------------------------------------------------
				// Apply optional height masks
				if(_FurShapeMask1Bits > 0)
				{
					if(_FurShapeMask1Bits & 1) furShape = min(furShape, furMask1.x);
					if(_FurShapeMask1Bits & 2) furShape = min(furShape, furMask1.y);
					if(_FurShapeMask1Bits & 4) furShape = min(furShape, furMask1.z);
					if(_FurShapeMask1Bits & 8) furShape = min(furShape, furMask1.w);
				}
				if(_FurShapeMask2Bits > 0)
				{
					if(_FurShapeMask2Bits & 1) furShape = min(furShape, furMask2.x);
					if(_FurShapeMask2Bits & 2) furShape = min(furShape, furMask2.y);
					if(_FurShapeMask2Bits & 4) furShape = min(furShape, furMask2.z);
					if(_FurShapeMask2Bits & 8) furShape = min(furShape, furMask2.w);
				}
				float thicknessSample = furShape < _FurMinHeight ? 0.0 : furShape;

				float3 worldPos = mul(unity_ObjectToWorld, vi.vertex);

				#if defined (USING_STEREO_MATRICES)
					float3 viewVector = (unity_StereoWorldSpaceCameraPos[0].xyz + unity_StereoWorldSpaceCameraPos[1].xyz) * 0.5 - worldPos;
				#else
					float3 viewVector = _WorldSpaceCameraPos.xyz - worldPos;
				#endif
				float viewDistance = max(0, length(viewVector) + _OverrideDistanceBias);

				// At extremly close ranges we need to shrink the shadows, otherwise the camera will be inside the shadow casting radius, which will cause glitches
				float shadowHeight = 0.95 * thicknessSample * _ScaleCalibration * _OverrideScale * _FurShellSpacing * _FurShadowCastSize * saturate((viewDistance - 0.035) * 20);

				VertexInput v = vi;
				v.vertex.xyz += v.normal * shadowHeight;

				// ----------------------------------------------------
				TRANSFER_SHADOW_CASTER_NOPOS(o,opos)
				#if defined(UNITY_STANDARD_USE_SHADOW_UVS)
					o.tex = TRANSFORM_TEX(v.uv0, _MainTex);

					#ifdef _PARALLAXMAP
						TANGENT_SPACE_ROTATION;
						o.viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
					#endif
				#endif


			}

			ENDCG
		}

		

	}


	CustomEditor "WarrensFastFur.CustomShaderGUI"
}
