#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System;

namespace WarrensFastFur
{

	public class CustomShaderGUI : ShaderGUI
	{
		private float lastValidationTime = 0;
		private bool runningOkay = false;

		private float furThickness = 0;
		private float layerDensity = 0;
		private float normalMagnitidue = 0;

		private enum Variant
		{
			Standard,
			Lite,
			UltraLite
		}
		private Variant variant;

		private enum Pipeline
		{
			Pipeline1,
			Pipeline2,
			Pipeline3
		}
		private Pipeline pipeline = Pipeline.Pipeline2;
		private Pipeline prevPipeline = (Pipeline)(-1);

		private enum MaskSelection
		{
			NONE = 0,
			R = 1,
			G = 2,
			B = 4,
			A = 8,
			ALL = ~0
		}

		public enum SmoothnessMapChannel
		{
			SpecularMetallicAlpha,
			AlbedoAlpha,
		}

		MaterialEditor editor;
		UnityEngine.Object[] materials;
		Material targetMat;
		MaterialProperty[] properties;
		SerializedObject serializedObject;

		bool initialized = false;

		private AssetImporter normalMapTI = null;
		private AssetImporter metallicMapTI = null;
		private AssetImporter specularMapTI = null;
		private AssetImporter furDataMapTI = null;
		private AssetImporter furDataMask1TI = null;
		private AssetImporter furDataMask2TI = null;
		private AssetImporter hairDataMapTI = null;
		private AssetImporter hairDataMapCoarseTI = null;
		private AssetImporter markingsMapTI = null;

		private float[] recentToonHues = new float[6];
		private float[] recentFadePoints = new float[4];


		public MaterialProperty GetProperty(string name)
		{
			try
			{
				return FindProperty(name, properties);
			}
			catch (Exception e)
			{
			}
			return null;
		}

		int FindPropertyIndex(string name)
		{
			return targetMat.shader.FindPropertyIndex(name);
		}

		void SetProperty(string name, float value)
		{
			GetProperty(name).floatValue = value;
		}
		void SetProperty(string name, Vector4 value)
		{
			GetProperty(name).vectorValue = value;
		}
		void SetProperty(string name, Color value)
		{
			GetProperty(name).colorValue = value;
		}

		void GUIProperty(string name, string helpText)
		{
			try
			{
				editor.ShaderProperty(GetProperty(name), EditorGUIUtility.TrTextContent(GetProperty(name).displayName, helpText));
			}
			catch (Exception e)
			{
			}
		}

		void DebugMessage(string message)
		{
			if (variant != Variant.UltraLite)
			{
				if (GetProperty("_DebuggingLog").floatValue > 0) Debug.Log("[WFFS] " + message);
			}
		}

		public GameObject furGrooming;

		//*************************************************************************************************************************************************
		// Handle the custom UI
		//*************************************************************************************************************************************************
		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
		{
			editor = materialEditor;
			this.properties = properties;
			targetMat = (Material)editor.target;
			GUIStyle headingStyle = new GUIStyle(EditorStyles.foldoutHeader);
			headingStyle.fontSize++;

			if (targetMat.shader.ToString().Contains("UltraLite")) variant = Variant.UltraLite;
			else if (targetMat.shader.ToString().Contains("Lite")) variant = Variant.Lite;
			else variant = Variant.Standard;

			GUIStyle titleStyle = new GUIStyle(EditorStyles.largeLabel);
			titleStyle.alignment = TextAnchor.MiddleCenter;
			string titleText = "Warren's Fast Fur Shader" + (variant == Variant.UltraLite ? " - UltraLite" : variant == Variant.Lite ? " - Lite" : "");
			titleText += " V4.0.0";

			if (variant != Variant.UltraLite)
			{
				pipeline = (Pipeline)GetProperty("_RenderPipeline").floatValue;

				if (pipeline == prevPipeline)
				{
					if (pipeline == Pipeline.Pipeline1) titleText += " (Fallback Pipeline)";
					if (pipeline == Pipeline.Pipeline2) titleText += " (Turbo Pipeline)";
					if (pipeline == Pipeline.Pipeline3) titleText += " (Super Pipeline)";
				}
				else
				{
					titleText += " (Pending change...)";
					initialized = false;
				}
			}

			EditorGUILayout.LabelField(titleText, titleStyle);

			if (variant != Variant.UltraLite)
			{
				if (pipeline == Pipeline.Pipeline1)
				{
					EditorGUILayout.HelpBox("The Fallback Pipeline is slower and has lower render quality. It should only be used if the target platform does not support Hull + Domain shaders.", MessageType.Info);
				}
				if (!Application.isPlaying && pipeline == Pipeline.Pipeline3 && GetProperty("_ConfirmPipeline").floatValue > 0)
				{
					EditorGUILayout.HelpBox("CRASH WARNING: The Super Pipeline uses complex Hull + Domain calculations that currently WILL crash on some AMD cards/drivers. DO NOT USE THIS VERSION PUBLICLY!", MessageType.Error);
					if (GUILayout.Button("Click here to switch to the Turbo Pipeline"))
					{
						SetProperty("_RenderPipeline", (float)Pipeline.Pipeline2);
						initialized = false;
					}
				}
			}

			if (!Application.isPlaying && furDataMapTI == null && runningOkay)
			{
				if (GUILayout.Button("Click here to generate a blank fur shape data map"))
				{
					GenerateFurDataMap();
					initialized = false;
				}
			}
			if (!Application.isPlaying && hairDataMapTI == null && runningOkay)
			{
				if (GUILayout.Button("Click here to generate hair maps"))
				{
					GenerateHairMap();
					initialized = false;
				}
			}
			if (!Application.isPlaying && (furDataMapTI == null || hairDataMapTI == null) && runningOkay)
			{
				EditorGUILayout.HelpBox("Required data textures are missing. Click the above buttons to generate blank versions of the required textures.", MessageType.Error);
			}

			if (!runningOkay)
			{
				EditorGUILayout.HelpBox("The shader GUI has failed to initialize!", MessageType.Error);
			}
			EditorGUILayout.Space();


			//--------------------------------------------------------------------------------
			// Main Maps
			SetProperty("_MainMapsGroup", EditorGUILayout.Foldout(GetProperty("_MainMapsGroup").floatValue == 1, GetProperty("_MainMapsGroup").displayName, true, headingStyle) ? 1 : 0);
			if (GetProperty("_MainMapsGroup").floatValue == 1)
			{
				EditorGUI.indentLevel++;

				if (variant != Variant.UltraLite)
				{
					EditorGUI.BeginDisabledGroup(Application.isPlaying);
					GUIProperty("_RenderPipeline", "V1 Pipeline is a standard geometry shader. V2 Pipeline uses a hull + domain shader to do culling. V3 Pipeline uses a hull + domain shader to do culling and also to generate triangles.");
					if (GetProperty("_RenderPipeline").floatValue == (float)Pipeline.Pipeline3)
					{
						if (GetProperty("_ConfirmPipeline").floatValue < 1)
						{
							EditorGUILayout.HelpBox("CRASH WARNING: The Super Pipeline uses complex Hull + Domain calculations that currently WILL crash on some AMD cards/drivers. DO NOT USE THIS VERSION PUBLICLY!", MessageType.Error);
							if (GUILayout.Button("Yes, I'm sure I want to enable the Super Pipeline"))
							{
								SetProperty("_ConfirmPipeline", 1);
								initialized = false;
							}
							if (GUILayout.Button("Cancel"))
							{
								SetProperty("_RenderPipeline", (float)Pipeline.Pipeline2);
								initialized = false;
							}
						}
					}
					else if (GetProperty("_ConfirmPipeline").floatValue > 0) SetProperty("_ConfirmPipeline", 0);
					EditorGUILayout.Space();
					EditorGUI.EndDisabledGroup();

					GUIProperty("_Mode", "Render mode.");
					if (GetProperty("_Mode").floatValue == 1)
					{
						GUIProperty("_Cutoff", "Alpha cutoff threshold when using Cutout rendering mode.");
					}
					EditorGUILayout.Space();
				}

				EditorGUI.BeginDisabledGroup(Application.isPlaying);
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MainTex").displayName, "Albedo (RGB) of the skin and fur"), GetProperty("_MainTex"), GetProperty("_Color"), GetProperty("_SelectedUV"));
				EditorGUI.EndDisabledGroup();
				GUIProperty("_HideSeams", "Disable combing when sampling the albedo map. Causes hairs to change colour mid-shaft, but reduces the appearance of seams.");
				if (variant != Variant.UltraLite)
				{
					if (GetProperty("_HideSeams").floatValue > 0) EditorGUILayout.HelpBox("Individual hairs may change colour mid-shaft when 'Aggressively Hide Seams' is enabled. The preferred way to hide seams is to add enough overpainting to the albedo map texture.", MessageType.Info);
    				EditorGUILayout.Space();
				}

				//--------------------------------------------------------------------------------
				// Quality Settings
				if (variant != Variant.UltraLite)
				{
					SetProperty("_QualityGroup", EditorGUILayout.Foldout(GetProperty("_QualityGroup").floatValue == 1, GetProperty("_QualityGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_QualityGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;
						if (pipeline != Pipeline.Pipeline3 && variant == Variant.Standard)
						{
							GUIProperty("_MaximumLayers", "Limits the maximum number of fur layers that can be rendered.");
							EditorGUILayout.Space();
						}
						GUIProperty("_V4QualityEditor", "Render quality when viewed in the Unity editor.");
						EditorGUILayout.Space();
						GUIProperty("_V4QualityVR", "Render quality when viewed in a VR application.");
						GUIProperty("_V4Quality2D", "Render quality when viewed in a non-VR application.");
						EditorGUILayout.Space();
						GUIProperty("_V4QualityVRMirror", "Render quality when viewed in a VR Chat mirror, in VR mode.");
						GUIProperty("_V4Quality2DMirror", "Render quality when viewed in a VR Chat mirror, in desktop mode.");
						EditorGUILayout.Space();
						GUIProperty("_V4QualityCameraView", "Render quality when viewed in the VR Chat camera viewfinder.");
						GUIProperty("_V4QualityStreamCamera", "Render quality when viewed in the VR Chat stream camera feed.");
						GUIProperty("_V4QualityCameraPhoto", "Render quality when taking a VR Chat camera photo.");
						EditorGUILayout.HelpBox("In order for the shader to detect it is rendering a camera photo, your VR Chat camera resolution needs to be set to a Y resolution of 1080 (the default), 1440, 2160, or 4320. A resolution of 720 will be interpreted as the 'Camera Viewfinder'. All other Y resolutions will be interpreted as the 'Stream Camera'.", MessageType.Info);
						EditorGUILayout.Space();
						GUIProperty("_V4QualityScreenshot", "Render quality when taking a VR Chat screenshot (either by using the in-game 'Screenshot' button, or by pressing CTRL+F12).");
						EditorGUI.indentLevel--;
					}
					EditorGUILayout.Space();

					//--------------------------------------------------------------------------------
					// Physically Based Shading
					SetProperty("_PBSSkinGroup", EditorGUILayout.Foldout(GetProperty("_PBSSkinGroup").floatValue == 1, GetProperty("_PBSSkinGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_PBSSkinGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;
						GUIProperty("_PBSSkin", "Enables Physically Based Shading for the skin layer.");
						EditorGUILayout.Space();

						EditorGUI.BeginDisabledGroup(GetProperty("_PBSSkin").floatValue == 0);
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_BumpMap").displayName, "Normal Map"), GetProperty("_BumpMap"), GetProperty("_BumpScale"));
						editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_OcclusionMap").displayName, "Occlusion Map"), GetProperty("_OcclusionMap"), GetProperty("_OcclusionStrength"));
						bool smoothnessScaled = (GetProperty("_SmoothnessTextureChannel").floatValue == 1);
						if (GetProperty("_MetallicGlossMap").textureValue != null)
						{
							editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MetallicGlossMap").displayName, "Metallic (R) and Smoothness (A)"), GetProperty("_MetallicGlossMap"));
							smoothnessScaled = true;
						}
						else
						{
							editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MetallicGlossMap").displayName, "Metallic (R) and Smoothness (A)"), GetProperty("_MetallicGlossMap"), GetProperty("_Metallic"));
						}
						GUIProperty("_SmoothnessTextureChannel", "Selects the source of the Smoothness channel. Smoothness affects how scattered light reflections are. It is only used for the skin layer.");
						if (smoothnessScaled)
						{
							GUIProperty("_GlossMapScale", "This multiplies the Smoothness channel. Smoothness affects how scattered light reflections are. It is only used for the skin layer.");
						}
						else
						{
							GUIProperty("_Glossiness", "Smoothness affects how scattered light reflections are. It is only used for the skin layer.");
						}
						EditorGUILayout.Space();

						GUIProperty("_SpecularHighlights", "Specular highlights  It is only used for the skin layer.");
						GUIProperty("_GlossyReflections", "Glossy reflections. It is only used for the skin layer.");

						EditorGUILayout.Space();
						GUIProperty("_TwoSided", "Render back-facing skin (the fur layers always render both sides)");
						EditorGUI.BeginDisabledGroup(GetProperty("_TwoSided").floatValue == 0);
						GUIProperty("_BackfaceColor", "Albedo colour of the backfacing skin");
						GUIProperty("_BackfaceEmission", "Emissive colour of the backfacing skin");
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.Space();

						//--------------------------------------------------------------------------------
						// MatCap
						SetProperty("_MatcapGroup", EditorGUILayout.Foldout(GetProperty("_MatcapGroup").floatValue == 1, GetProperty("_MatcapGroup").displayName, true) ? 1 : 0);
						if (GetProperty("_MatcapGroup").floatValue == 1)
						{
							EditorGUI.indentLevel++;
							GUIProperty("_MatcapEnable", "Enable Material Capture.");
							bool matcapEnabled = GetProperty("_MatcapEnable").floatValue > 0;
							EditorGUI.BeginDisabledGroup(!matcapEnabled);
							editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_Matcap").displayName, "The MatCap texture is a spherical texture that is directionally projected onto the surface. It roughly simulates reflections."), GetProperty("_Matcap"), GetProperty("_MatcapColor"));
							GUIProperty("_MatcapTextureChannel", "This is the spherical MatCap texture.");
							if (matcapEnabled) EditorGUI.BeginDisabledGroup(GetProperty("_MatcapTextureChannel").floatValue != 1);
							editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MatcapMask").displayName, "The MatCap mask is optional, and can be used to filter where the MatCap is visible."), GetProperty("_MatcapMask"));
							if (matcapEnabled) EditorGUI.EndDisabledGroup();
							EditorGUILayout.Space();
							GUIProperty("_MatcapAdd", "This blends in the MatCap by adding it to the albedo.");
							GUIProperty("_MatcapReplace", "This blends in the MatCap by replacing the albedo.");
							GUIProperty("_MatcapEmission", "This applies the MatCap as emission.");
							EditorGUILayout.Space();
							GUIProperty("_MatcapSpecular", "Should the MatCap be applied as diffuse or specular light?");

							EditorGUI.EndDisabledGroup();
							EditorGUI.indentLevel--;
						}
						EditorGUILayout.Space();

						EditorGUI.EndDisabledGroup();
						EditorGUI.indentLevel--;
					}

				}
				EditorGUI.indentLevel--;
				EditorGUILayout.Space();
			}
			EditorGUILayout.Space();
			EditorGUILayout.Space();


			//--------------------------------------------------------------------------------
			// Fur Shape
			SetProperty("_FurShapeGroup", EditorGUILayout.Foldout(GetProperty("_FurShapeGroup").floatValue == 1, GetProperty("_FurShapeGroup").displayName, true, headingStyle) ? 1 : 0);
			if (GetProperty("_FurShapeGroup").floatValue == 1)
			{
				EditorGUI.indentLevel++;

				EditorGUI.BeginDisabledGroup(Application.isPlaying);
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurShapeMap").displayName, "Encoded map of fur combing (RG), height (B), and density (A)"), GetProperty("_FurShapeMap"));
				EditorGUI.EndDisabledGroup();
				if (furDataMapTI == null && !Application.isPlaying)
				{
					if (GUILayout.Button("Click here to generate a blank fur shape data map")) GenerateFurDataMap();
					EditorGUI.BeginDisabledGroup(true);
				}

				if (variant != Variant.UltraLite)
				{
					GUILayout.BeginHorizontal();
					editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurShapeMask1").displayName, "Contains up to 4 optional height masks"), GetProperty("_FurShapeMask1"));
					MaskSelection bits = (MaskSelection)GetProperty("_FurShapeMask1Bits").floatValue;
					MaskSelection newBits = (MaskSelection)EditorGUILayout.EnumFlagsField(bits);
					if (bits != newBits) SetProperty("_FurShapeMask1Bits", (float)newBits);
					GUILayout.EndHorizontal();

					GUILayout.BeginHorizontal();
					editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurShapeMask2").displayName, "Contains up to 4 optional height masks"), GetProperty("_FurShapeMask2"));
					bits = (MaskSelection)GetProperty("_FurShapeMask2Bits").floatValue;
					newBits = (MaskSelection)EditorGUILayout.EnumFlagsField(bits);
					if (bits != newBits) SetProperty("_FurShapeMask2Bits", (float)newBits);
					GUILayout.EndHorizontal();
				}

				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_FurGroomingMask").displayName, "Used during Fur Grooming. It can be used as a length mask, or it can be copied from."), GetProperty("_FurGroomingMask"));

				EditorGUILayout.Space();

				if (variant != Variant.UltraLite) EditorGUILayout.LabelField("Max fur: " + Mathf.Round(furThickness * 1000f) + "mm thick, " + Mathf.Round(layerDensity * 100f) + "% density", titleStyle);
				else EditorGUILayout.LabelField("Max fur: " + Mathf.Round(furThickness * 1000f) + "mm thick", titleStyle);

				GUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				GUIProperty("_ScaleCalibration", "Calibrates the length of the fur relative to the avatar scaling");
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(Application.isPlaying);
				if (GUILayout.Button("Increase")) SetProperty("_ScaleCalibration", GetProperty("_ScaleCalibration").floatValue * 2f);
				if (GUILayout.Button("Decrease")) SetProperty("_ScaleCalibration", GetProperty("_ScaleCalibration").floatValue * 0.5f);
				if (GUILayout.Button("Reset"))
				{
					SetProperty("_ScaleCalibration", -1f);
					initialized = false;
				}
				EditorGUI.EndDisabledGroup();

				GUILayout.EndHorizontal();
				GUIProperty("_FurShellSpacing", "Spacing between each rendered layer of fur");
				GUIProperty("_FurMinHeight", "Hides any hair shorter than the cutoff");
				if (variant != Variant.UltraLite)
				{
					if (GetProperty("_FurMinHeight").floatValue < 0.01f)
					{
						EditorGUILayout.HelpBox("Setting the minimum fur height below 0.01 may cause the Fur Grooming masking to behave incorrectly, due to texture compression artifacts.", MessageType.Info);
					}
				}
				if (variant != Variant.UltraLite) EditorGUILayout.Space();
				GUIProperty("_FurCombStrength", "Base strength of fur combing");
				//            GUIProperty("_FurCombCompression","When enabled, this compresses the fur when it is combed");
				EditorGUILayout.Space();

				GUIProperty("_BodyShrinkOffset", "Shrinks the body proportionally to the length of the fur");
				GUIProperty("_BodyExpansion", "If enabled, the body layer will expand when far away");
				GUIProperty("_BodyResizeCutoff", "Don't resize the body if the fur thickness is below the cutoff");

				EditorGUI.EndDisabledGroup();

				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();
			EditorGUILayout.Space();


			//--------------------------------------------------------------------------------
			// Hairs
			SetProperty("_HairsGroup", EditorGUILayout.Foldout(GetProperty("_HairsGroup").floatValue == 1, GetProperty("_HairsGroup").displayName, true, headingStyle) ? 1 : 0);
			if (GetProperty("_HairsGroup").floatValue == 1)
			{
				EditorGUI.indentLevel++;

				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_HairMap").displayName, "Encoded map of individual hair hightlights (R), tinting (G), and height (B)"), GetProperty("_HairMap"));
				if (variant != Variant.UltraLite) editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_HairMapCoarse").displayName, "A coarse version of the hair map that does not include fine hairs. This version of the map is used at longer ranges, or when the viewing angle is sharp."), GetProperty("_HairMapCoarse"));

				if (hairDataMapTI == null)
				{
					if (GUILayout.Button("Click here to generate hair maps")) GenerateHairMap();
					EditorGUI.BeginDisabledGroup(true);
				}
				else if (hairDataMapCoarseTI == null && variant != Variant.UltraLite)
				{
					if (GUILayout.Button("Click here to generate new hair maps (will overwrite existing maps!)")) GenerateHairMap();
				}

				EditorGUILayout.Space();
				GUIProperty("_HairDensity", "Base density of individual hairs");
				GUIProperty("_HairClipping", "Makes hairs longer, but the geometric shells do not move, so the tops of tall hairs are clipped off");

				EditorGUILayout.Space();
				EditorGUI.BeginDisabledGroup(hairDataMapCoarseTI == null);
				GUIProperty("_HairMapCoarseStrength", "Strength (ie. height) of the Coarse Hair Map");
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.Space();

				GUIProperty("_HairMipType", "Chooses either 'Box' filtering (blurry), or 'Kaiser' filtering (sharp) for the mip-maps");
				if (variant != Variant.UltraLite)
				{
					if (GetProperty("_HairSharpen").floatValue > 0 && GetProperty("_HairBlur").floatValue > 0) SetProperty("_HairBlur", 0);
					EditorGUI.BeginDisabledGroup(GetProperty("_HairBlur").floatValue > 0);
					GUIProperty("_HairSharpen", "Sharpens the appearance of the hairs, but also causes visual noise");
					EditorGUI.EndDisabledGroup();
					EditorGUI.BeginDisabledGroup(GetProperty("_HairSharpen").floatValue > 0);
					GUIProperty("_HairBlur", "Blurs the appearance of the hairs, and also reduces visual noise");
					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.Space();
				GUIProperty("_HairStiffness", "Controls how easily hairs bend (due to gravity, wind, and movement). Note that stiff hairs will bend more at the roots than flexible hairs, thus flattening their appearance.");
				EditorGUILayout.Space();

				SetProperty("_HairsColourGroup", EditorGUILayout.Foldout(GetProperty("_HairsColourGroup").floatValue == 1, GetProperty("_HairsColourGroup").displayName, true) ? 1 : 0);
				if (GetProperty("_HairsColourGroup").floatValue == 1)
				{
					EditorGUI.indentLevel++;
					GUIProperty("_HairHighlights", "Strength of individual hair highlights");
					GUIProperty("_HairColourShift", "Strength of individual hair tinting");
					if (variant != Variant.UltraLite)
					{
						EditorGUILayout.Space();
						GUIProperty("_AdvancedHairColour", "Enable hairs to have different colours along their length");

						if (GetProperty("_AdvancedHairColour").floatValue < 0.5) EditorGUI.BeginDisabledGroup(true);
						else EditorGUILayout.HelpBox("This feature is still in development, and should be considered experimental and subject to change. It currently doesn't fade in correctly over distance.", MessageType.Warning);

						GUIProperty("_HairRootColour", "Base colour of the roots of the hairs");
						GUIProperty("_HairMidColour", "Base colour of the middle of the hairs");
						GUIProperty("_HairTipColour", "Base colour of the tips of the hairs");
						EditorGUILayout.Space();
						GUIProperty("_HairRootAlbedo", "How much does the albedo map affect the roots of the hairs");
						GUIProperty("_HairMidAlbedo", "How much does the albedo map affect the middle of the hairs");
						GUIProperty("_HairTipAlbedo", "How much does the albedo map affect the tips of the hairs");
						EditorGUILayout.Space();
						GUIProperty("_HairRootMarkings", "How much does the markings map affect the roots of the hairs");
						GUIProperty("_HairMidMarkings", "How much does the markings map affect the middle of the hairs");
						GUIProperty("_HairTipMarkings", "How much does the markings map affect the tips of the hairs");
						EditorGUILayout.Space();
						GUIProperty("_HairRootPoint", "Sets where the root of the hair starts to fade into the middle");
						GUIProperty("_HairMidLowPoint", "Sets where the middle of the hair starts to fade into the root");
						GUIProperty("_HairMidHighPoint", "Sets where the middle of the hair starts to fade into the tip");
						GUIProperty("_HairTipPoint", "Sets where the tip of the hair starts to fade into the middle");
						EditorGUILayout.Space();
						GUIProperty("_HairColourMinHeight", "Only apply advanced colouring to fur thicker than the minimum height");

						if (GetProperty("_AdvancedHairColour").floatValue > 0.5)
						{
							String[] hairColourProperties = {"_HairRootColour","_HairMidColour","_HairTipColour","_HairRootAlbedo","_HairMidAlbedo","_HairTipAlbedo",
							"_HairRootMarkings","_HairMidMarkings","_HairTipMarkings","_HairRootPoint","_HairMidLowPoint","_HairMidHighPoint","_HairTipPoint","_HairColourMinHeight"};
							if (!CheckDefaults(hairColourProperties))
							{
								if (GUILayout.Button("Click here to reset advanced hair colour settings to defaults"))
								{
									SetDefaults(hairColourProperties);
								}
							}
						}
						EditorGUI.EndDisabledGroup();
					}

					EditorGUI.indentLevel--;
				}

				EditorGUILayout.Space();

				if (variant != Variant.UltraLite)
				{
					SetProperty("_HairCurlsGroup", EditorGUILayout.Foldout(GetProperty("_HairCurlsGroup").floatValue == 1, GetProperty("_HairCurlsGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_HairCurlsGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;
						GUIProperty("_HairCurlsActive", "Enables Hair Curls");
						bool curlsActive = GetProperty("_HairCurlsActive").floatValue > 0;
						EditorGUI.BeginDisabledGroup(!curlsActive);
						bool lockActive = GetProperty("_HairCurlsLockXY").floatValue > 0;
						GUIProperty("_HairCurlsLockXY", "Lock the X and Y axis settings");
						GUIProperty("_HairCurlXWidth", "How wide are the hair curls on the X axis");
						GUIProperty("_HairCurlXTwists", "How many twists are there in the X axis");
						EditorGUI.EndDisabledGroup();
						EditorGUI.BeginDisabledGroup(!curlsActive || lockActive);
						GUIProperty("_HairCurlYWidth", "How wide are the hair curls on the Y axis");
						GUIProperty("_HairCurlYTwists", "How many twists are there in the Y axis");
						EditorGUI.EndDisabledGroup();
						EditorGUI.BeginDisabledGroup(!curlsActive || hairDataMapTI == null);
						GUIProperty("_HairCurlXYOffset", "The phase shift between the X and Y axis");
						EditorGUILayout.Space();
						if (lockActive)
						{
							SetProperty("_HairCurlYWidth", GetProperty("_HairCurlXWidth").floatValue);
							SetProperty("_HairCurlYTwists", GetProperty("_HairCurlXTwists").floatValue);
						}
						EditorGUI.EndDisabledGroup();
						EditorGUI.indentLevel--;
					}
					EditorGUILayout.Space();
				}

				if (variant != Variant.UltraLite)
				{
					SetProperty("_HairRenderingGroup", EditorGUILayout.Foldout(GetProperty("_HairRenderingGroup").floatValue == 1, GetProperty("_HairRenderingGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_HairRenderingGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;

						GUIProperty("_TiltEdges", "Tilts the edges of the fur geometry towards the camera. This can reduce edge artifacts on some avatars, but may cause problems on others.");
						EditorGUILayout.Space();

						GUIProperty("_HairMapAlphaFilter", "Boosts the lengths of the hairs using the alpha channel for length calibration");
						GUIProperty("_HairMapMipFilter", "Boosts the lengths of the hairs based on the mip map level");

						EditorGUILayout.Space();
						EditorGUI.BeginDisabledGroup(hairDataMapCoarseTI == null);
						GUIProperty("_HairMapCoarseAlphaFilter", "Boosts the lengths of the hairs using the alpha channel for length calibration");
						GUIProperty("_HairMapCoarseMipFilter", "Boosts the lengths of the hairs based on the mip map level");
						EditorGUI.EndDisabledGroup();

						String[] hairRenderProperties = { "_TiltEdges", "_HairMapAlphaFilter", "_HairMapMipFilter", "_HairMapCoarseAlphaFilter", "_HairMapCoarseMipFilter" };
						if (!CheckDefaults(hairRenderProperties))
						{
							if (GUILayout.Button("Click here to reset Advanced Hair Rendering Adjustments to defaults"))
							{
								SetDefaults(hairRenderProperties);
							}
						}
						EditorGUI.indentLevel--;
					}
				}

				EditorGUI.EndDisabledGroup();

				EditorGUILayout.Space();

				// Hair Map Generation
				SetProperty("_GenerateHairGroup", EditorGUILayout.Foldout(GetProperty("_GenerateHairGroup").floatValue == 1, GetProperty("_GenerateHairGroup").displayName, true) ? 1 : 0);
				if (GetProperty("_GenerateHairGroup").floatValue == 1)
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.Space();

					if (!Application.isPlaying)
					{
						if (hairDataMapTI == null)
						{
							if (GUILayout.Button("Click here to generate hair maps")) GenerateHairMap();
						}
						else
						{
							if (GUILayout.Button("Click here to generate new hair maps (will overwrite existing maps!)")) GenerateHairMap();
						}
						EditorGUILayout.Space();
					}

					GUIProperty("_GenGuardHairs", "Maximum number of Guard Hairs");
					GUIProperty("_GenGuardHairsTaper", "The shape of the Guard Hairs");
					GUIProperty("_GenGuardHairMinHeight", "Minimum Guard Hair Height");
					GUIProperty("_GenGuardHairMaxHeight", "Maximum Guard Hair Height");
					GUIProperty("_GenGuardHairMinColourShift", "Minimum Guard Hair ColourShift");
					GUIProperty("_GenGuardHairMaxColourShift", "Maximum Guard Hair ColourShift");
					GUIProperty("_GenGuardHairMinHighlight", "Minimum Guard Hair Highlight");
					GUIProperty("_GenGuardHairMaxHighlight", "Maximum Guard Hair Highlight");
					EditorGUILayout.Space();

					GUIProperty("_GenMediumHairs", "Maximum number of Medium Hairs");
					GUIProperty("_GenMediumHairsTaper", "The shape of the Medium Hairs");
					GUIProperty("_GenMediumHairMinHeight", "Minimum Medium Hair Height");
					GUIProperty("_GenMediumHairMaxHeight", "Maximum Medium Hair Height");
					GUIProperty("_GenMediumHairMinColourShift", "Minimum Medium Hair ColourShift");
					GUIProperty("_GenMediumHairMaxColourShift", "Maximum Medium Hair ColourShift");
					GUIProperty("_GenMediumHairMinHighlight", "Minimum Medium Hair Highlight");
					GUIProperty("_GenMediumHairMaxHighlight", "Maximum Medium Hair Highlight");
					EditorGUILayout.Space();

					GUIProperty("_GenFineHairs", "Maximum number of Fine Hairs");
					GUIProperty("_GenFineHairsTaper", "The shape of the Fine Hairs");
					GUIProperty("_GenFineHairMinHeight", "Minimum Fine Hair Height");
					GUIProperty("_GenFineHairMaxHeight", "Maximum Fine Hair Height");
					GUIProperty("_GenFineHairMinColourShift", "Minimum Fine Hair ColourShift");
					GUIProperty("_GenFineHairMaxColourShift", "Maximum Fine Hair ColourShift");
					GUIProperty("_GenFineHairMinHighlight", "Minimum Fine Hair Highlight");
					GUIProperty("_GenFineHairMaxHighlight", "Maximum Fine Hair Highlight");
					EditorGUI.indentLevel--;
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();
			EditorGUILayout.Space();


			//--------------------------------------------------------------------------------
			// Tiled Fur Markings
			if (variant != Variant.UltraLite)
			{
				SetProperty("_MarkingsGroup", EditorGUILayout.Foldout(GetProperty("_MarkingsGroup").floatValue == 1, GetProperty("_MarkingsGroup").displayName, true, headingStyle) ? 1 : 0);
				if (GetProperty("_MarkingsGroup").floatValue == 1)
				{
					EditorGUI.indentLevel++;

					editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_MarkingsMap").displayName, "Tiled, secondary albedo (RGB) map of the fur. Does not affect the skin."), GetProperty("_MarkingsMap"), GetProperty("_MarkingsColour"));


					if (!Application.isPlaying)
					{
						string buttonText = "Click here to generate random fur markings";
						if (markingsMapTI != null) buttonText = "Generate random fur markings (will overwrite existing texture!)";

						if (GUILayout.Button(buttonText))
						{
							GenerateFunctions gen = new GenerateFunctions();
							string assetPath = "Assets/FastFur_" + this.editor.target.name + "_Markings.png";
							if (markingsMapTI != null) assetPath = markingsMapTI.assetPath;

							gen.GenerateFurMarkings(this, assetPath);
							Texture2D myTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
							((Material)this.editor.target).SetTexture("_MarkingsMap", myTexture);

							CheckImports();
						}
					}

					EditorGUI.BeginDisabledGroup(markingsMapTI == null);
					GUIProperty("_MarkingsDensity", "Tile density of the fur markings");
					GUIProperty("_MarkingsRotation", "Rotation of fur markings");
					EditorGUILayout.Space();
					GUIProperty("_MarkingsVisibility", "Visibility of the fur markings albedo");
					GUIProperty("_MarkingsHeight", "If positive, brighter coloured areas will raise the fur height, while darker coloured areas will lower the fur height");
					EditorGUILayout.Space();
					EditorGUI.EndDisabledGroup();

					// Fur Markings Generation
					SetProperty("_GenerateFurGroup", EditorGUILayout.Foldout(GetProperty("_GenerateFurGroup").floatValue == 1, GetProperty("_GenerateFurGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_GenerateFurGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.Space();

						GUIProperty("_PigmentColour", "Colour of pigmented cells (spots/stripes/etc...)");
						GUIProperty("_TransitionalColour", "Colour of transitional cells");
						GUIProperty("_BaseColour", "Colour of non-pigmented cells");
						GUIProperty("_MarkingsContrast", "Contrast between pigmented and non-pigmented cells");
						EditorGUILayout.Space();
						GUIProperty("_ActivatorHormoneRadius", "Inner-circle radius of pigmented cell activator hormones");
						GUIProperty("_InhibitorHormoneAdditionalRadius", "Additional outer-ring radius of pigmented cell inhibitor hormones.");
						GUIProperty("_InhibitorStrength", "Strength of inhibitor hormones");
						EditorGUILayout.Space();
						GUIProperty("_CellStretch", "Cell elliptical stretching");
						//GUIProperty("_InitialDensity","Starting density of pigmented cells");
						GUIProperty("_MutationRate", "Rate of random cell mutations");
						GUIProperty("_ActivatorCycles", "Number of cell activator cycles");
						//GUIProperty("_GrowthCycles","Number of cell growth cycles (happens after activator cycles)");

						EditorGUI.indentLevel--;
					}
					EditorGUILayout.Space();
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}


			//--------------------------------------------------------------------------------
			// Light Settings
			SetProperty("_LightingGroup", EditorGUILayout.Foldout(GetProperty("_LightingGroup").floatValue == 1, GetProperty("_LightingGroup").displayName, true, headingStyle) ? 1 : 0);
			if (GetProperty("_LightingGroup").floatValue == 1)
			{
				EditorGUI.indentLevel++;

				if (materialEditor.EmissionEnabledProperty())
				{
					targetMat.EnableKeyword("_EMISSION");
					EditorGUI.indentLevel++;
					editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_EmissionMap").displayName, "Emission (RGB) of the skin and fur"), GetProperty("_EmissionMap"), GetProperty("_EmissionColor"));
					GUIProperty("_EmissionMapStrength", "Overall strength of the Emission Map");
					EditorGUILayout.Space();
					GUIProperty("_AlbedoEmission", "Emission that is the same colour as the Albedo Map.");
					EditorGUILayout.Space();
					EditorGUI.indentLevel--;
				}
				else targetMat.DisableKeyword("_EMISSION");
				EditorGUILayout.Space();


				if (variant != Variant.UltraLite)
				{
					SetProperty("_WorldLightGroup", EditorGUILayout.Foldout(GetProperty("_WorldLightGroup").floatValue == 1, GetProperty("_WorldLightGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_WorldLightGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;
						GUIProperty("_MaxBrightness", "Applies a hard-limit to the maximum brightness of each light source.");
						GUIProperty("_SoftenBrightness", "Softens the brightness of light sources with an intensity greater than 0.75, but doesn't impose a hard-limit.");
						GUIProperty("_WorldLightReColour", "Re-colour world lighting.");
						GUIProperty("_WorldLightReColourStrength", "Re-colour strength.");
						EditorGUI.indentLevel--;
					}
					EditorGUILayout.Space();


					// This is for backwards-compatibility with the older "_MinBrightness" setting
					float minBrightness = GetProperty("_MinBrightness").floatValue;
					if (minBrightness > 0)
					{
						SetProperty("_MinBrightness", 0f);
						SetProperty("_ExtraLightingEnable", 1f);
						SetProperty("_ExtraLighting", minBrightness);
						SetProperty("_ExtraLightingRim", 0f);
						SetProperty("_ExtraLightingMode", 1f);
					}

					SetProperty("_ExtraLightingGroup", EditorGUILayout.Foldout(GetProperty("_ExtraLightingGroup").floatValue == 1, GetProperty("_ExtraLightingGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_ExtraLightingGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;
						GUIProperty("_FallbackLightEnable", "Enable a fallback directional light if a world does not have one.");
						EditorGUI.BeginDisabledGroup(GetProperty("_FallbackLightEnable").floatValue == 0);
						EditorGUILayout.Space();
						GUIProperty("_FallbackLightColor", "Fallback directional lighting colour.");
						GUIProperty("_FallbackLightStrength", "Fallback directional lighting brightness.");
						GUIProperty("_FallbackLightDirection", "Fallback directional lighting horizontal direction.");
						GUIProperty("_FallbackLightAngle", "Fallback directional vertical direction.");
						EditorGUI.EndDisabledGroup();
						EditorGUILayout.Space();
						GUIProperty("_ExtraLightingEnable", "Enable extra lighting.");
						EditorGUI.BeginDisabledGroup(GetProperty("_ExtraLightingEnable").floatValue == 0);
						EditorGUILayout.Space();
						GUIProperty("_ExtraLighting", "Strength of the extra ambient lighting.");
						GUIProperty("_ExtraLightingRim", "Does the extra ambient lighting appear to come from behind or from the front?");
						GUIProperty("_ExtraLightingColor", "Extra ambient lighting colour.");
						GUIProperty("_ExtraLightingMode", "Determines how the extra ambient lighting is applied. It can be set to either always add extra light, or it can only add enough extra light when-needed to meet a minumum lighting level.");
						EditorGUI.EndDisabledGroup();
						EditorGUI.indentLevel--;
					}
					EditorGUILayout.Space();


					SetProperty("_AnisotropicGroup", EditorGUILayout.Foldout(GetProperty("_AnisotropicGroup").floatValue == 1, GetProperty("_AnisotropicGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_AnisotropicGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;
						GUIProperty("_FurAnisotropicEnable", "Simulates the way light reacts with individual strands of hair");
						EditorGUI.BeginDisabledGroup(GetProperty("_FurAnisotropicEnable").floatValue == 0);

						EditorGUILayout.Space();
						GUIProperty("_FurAnisotropicReflect", "Simulates light reflecting off of the front surface of the hairs");
						GUIProperty("_FurAnisoReflectAngle", "Sets the angle at which light reflects off of the front surface of the hairs");
						GUIProperty("_FurAnisoReflectGloss", "How glossy (ie. concentrated) are the anisotropic reflections");
						GUIProperty("_FurAnisoReflectMetallic", "How metallic (ie. the light changes colour to match the surface colour) are the anisotropic reflections");
						GUIProperty("_FurAnisotropicReflectColor", "Applies a colour tint to the anisotropic reflected light");
						GUIProperty("_FurAnisotropicReflectColorNeg", "Applies a colour tint to the anisotropic reflected light when it is 'red-shifted' by iridescence");
						GUIProperty("_FurAnisotropicReflectColorPos", "Applies a colour tint to the anisotropic reflected light when it is 'blue-shifted' by iridescence");
						GUIProperty("_FurAnisoReflectIridescenceStrength", "How vibrant the iridescence should be");
						GUIProperty("_FurAnisoReflectEmission", "Adds emission to the anisotropic reflections");
						EditorGUILayout.Space();
						GUIProperty("_FurAnisotropicRefract", "Simulates light entering the hairs, bouncing off of the back surface, and re-emerging out the front at a refracted angle");
						GUIProperty("_FurAnisoRefractAngle", "Sets the angle at which light refracts when bouncing off of the back surface of the hairs");
						GUIProperty("_FurAnisoRefractGloss", "How glossy (ie. concentrated) are the anisotropic refractions");
						GUIProperty("_FurAnisoRefractMetallic", "How metallic (ie. the light changes colour to match the surface colour) are the anisotropic refractions");
						GUIProperty("_FurAnisotropicRefractColor", "Applies a colour tint to the anisotropic refracted light");
						GUIProperty("_FurAnisoRefractEmission", "Adds emission to the anisotropic refractions");
						EditorGUILayout.Space();
						GUIProperty("_FurAnisoDepth", "How deep do anisotropic reflections go into the fur");
						GUIProperty("_FurAnisoSkin", "Adds a baseline amount of anisotropic light to both skin and fur, regardless of depth");
						GUIProperty("_FurAnisoWindShimmer", "Controls the amount that wind will cause the anisotropic lighting to shimmer.");
						GUIProperty("_FurAnisoFlat", "Changes the appearance of the anisotropic lighting by artificially flattening the hairs against the skin before calculating the anisotropic reflection/refraction angles.");
						EditorGUILayout.Space();

						String[] anisoProperties = {"_FurAnisotropicReflect","_FurAnisoReflectAngle","_FurAnisoReflectGloss","_FurAnisoReflectMetallic","_FurAnisotropicReflectColor","_FurAnisotropicReflectColorPos","_FurAnisotropicReflectColorNeg","_FurAnisoReflectIridescenceStrength",
						"_FurAnisoReflectEmission","_FurAnisoRefractEmission","_FurAnisotropicRefract","_FurAnisotropicRefractColor","_FurAnisoRefractAngle","_FurAnisoRefractGloss","_FurAnisoRefractMetallic","_FurAnisoDepth","_FurAnisoSkin","_FurAnisoWindShimmer","_FurAnisoFlat"};
						if (!CheckDefaults(anisoProperties))
						{
							if (GUILayout.Button("Click here to reset anisotropic settings to defaults"))
							{
								SetDefaults(anisoProperties);
							}
						}
						EditorGUI.EndDisabledGroup();
						EditorGUI.indentLevel--;
					}
					EditorGUILayout.Space();
				}


				SetProperty("_FurLightingGroup", EditorGUILayout.Foldout(GetProperty("_FurLightingGroup").floatValue == 1, GetProperty("_FurLightingGroup").displayName, true) ? 1 : 0);
				if (GetProperty("_FurLightingGroup").floatValue == 1)
				{
					EditorGUI.indentLevel++;
					GUIProperty("_LightWraparound", "Simulates light passing along the surface at a steep angle and being caught by the tips of the fur");
					GUIProperty("_SubsurfaceScattering", "Simulates the light being absorbed and scattered internally before passing out of the fur, thus allowing some of it to be visible from behind");
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.Space();


				SetProperty("_OcclusionGroup", EditorGUILayout.Foldout(GetProperty("_OcclusionGroup").floatValue == 1, GetProperty("_OcclusionGroup").displayName, true) ? 1 : 0);
				if (GetProperty("_OcclusionGroup").floatValue == 1)
				{
					EditorGUI.indentLevel++;
					GUIProperty("_DeepFurOcclusionStrength", "Strength of darkening as light penetrates deeper into fur");
					GUIProperty("_LightPenetrationDepth", "How far does the light penetrate before any occlusion is applied");
					EditorGUILayout.Space();
					GUIProperty("_ProximityOcclusion", "How much does the light get occluded when the camera is close");
					GUIProperty("_ProximityOcclusionRange", "How far away can the camera be to cause occlusion");
					if (variant != Variant.UltraLite) EditorGUILayout.Space();
					GUIProperty("_FurShadowCastSize", "How much of the fur should block light sources and cast shadows");
					GUIProperty("_SoftenShadows", "Converts a percentage of world lighting into ambient lighting.");
					EditorGUI.indentLevel--;
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();
			EditorGUILayout.Space();


			//--------------------------------------------------------------------------------
			// Gravity, Wind, and Movement
			SetProperty("_DynamicsGroup", EditorGUILayout.Foldout(GetProperty("_DynamicsGroup").floatValue == 1, GetProperty("_DynamicsGroup").displayName, true, headingStyle) ? 1 : 0);
			if (GetProperty("_DynamicsGroup").floatValue == 1)
			{
				EditorGUI.indentLevel++;
				GUIProperty("_FurGravitySlider", "Gravity strength");
				EditorGUILayout.Space();
				GUIProperty("_CameraProximityTouch", "The range at which fur will try to move out of the way of the camera");
				if (variant != Variant.UltraLite) EditorGUILayout.Space();
				//GUIProperty("_FurContactStrength","Strength of contact detection");
				//EditorGUILayout.Space();
				SetProperty("_WindGroup", EditorGUILayout.Foldout(GetProperty("_WindGroup").floatValue == 1, GetProperty("_WindGroup").displayName, true) ? 1 : 0);
				if (GetProperty("_WindGroup").floatValue == 1)
				{
					EditorGUI.indentLevel++;
					GUIProperty("_EnableWind", "Enables wind");
					EditorGUI.BeginDisabledGroup(GetProperty("_EnableWind").floatValue == 0);
					EditorGUILayout.Space();

					GUIProperty("_WindSpeed", "Overall wind speed");
					EditorGUILayout.Space();
					GUIProperty("_WindDirection", "The horizonal direction of the wind");
					GUIProperty("_WindAngle", "The vertical direction of the wind");
					EditorGUILayout.Space();
					GUIProperty("_WindTurbulenceStrength", "Strength and frequency of random turbulence");
					GUIProperty("_WindGustsStrength", "Strength and frequency of gusts of wind");
					EditorGUI.EndDisabledGroup();
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.Space();

				if (variant != Variant.UltraLite)
				{
					SetProperty("_MovementGroup", EditorGUILayout.Foldout(GetProperty("_MovementGroup").floatValue == 1, GetProperty("_MovementGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_MovementGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;
						EditorGUILayout.HelpBox("This feature allows the fur to react to movement, but unfortunately it requires some rather advanced animation controller configuration. Currently, this must be done manually for each avatar.", MessageType.Info);
						//if (GUILayout.Button("Click here to create or update VR Chat movement animations"));// CreateAnimations(properties);
						GUIProperty("_MovementStrength", "How much does the fur bend when the avatar moves");
						GUIProperty("_VelocityX", "Simulates movement in the X dimension");
						GUIProperty("_VelocityY", "Simulates movement in the Y dimension");
						GUIProperty("_VelocityZ", "Simulates movement in the Z dimension");
						EditorGUI.indentLevel--;
					}
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();
			EditorGUILayout.Space();


			//--------------------------------------------------------------------------------
			// Toon Shading
			if (variant != Variant.UltraLite)
			{
				SetProperty("_ToonShadingGroup", EditorGUILayout.Foldout(GetProperty("_ToonShadingGroup").floatValue == 1, GetProperty("_ToonShadingGroup").displayName, true, headingStyle) ? 1 : 0);
				if (GetProperty("_ToonShadingGroup").floatValue == 1)
				{
					EditorGUI.indentLevel++;

					GUIProperty("_ToonShading", "Simulates a toon effect by limiting the fur colours into discrete steps");
					SetProperty("_ToonColoursGroup", EditorGUILayout.Foldout(GetProperty("_ToonColoursGroup").floatValue == 1, GetProperty("_ToonColoursGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_ToonColoursGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;

						GUIProperty("_ToonColour1", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour2", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour3", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour4", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour5", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour6", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour7", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour8", "The original colour will be substituted by whichever colour substitution is closest");
						GUIProperty("_ToonColour9", "The original colour will be substituted by whichever colour substitution is closest");

						EditorGUI.indentLevel--;
					}
					EditorGUI.BeginDisabledGroup(GetProperty("_ToonShading").floatValue == 0);
					EditorGUILayout.Space();

					SetProperty("_ToonPostEffectsGroup", EditorGUILayout.Foldout(GetProperty("_ToonPostEffectsGroup").floatValue == 1, GetProperty("_ToonPostEffectsGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_ToonPostEffectsGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;

						SetProperty("_ToonHue", EditorGUILayout.Foldout(GetProperty("_ToonHue").floatValue == 1, GetProperty("_ToonHue").displayName, true) ? 1 : 0);
						if (GetProperty("_ToonHue").floatValue == 1)
						{
							EditorGUI.indentLevel++;

							GUIProperty("_ToonHueRGB", "Standard Colour Mix");
							GUIProperty("_ToonHueGBR", "Shifted Colour Mix");
							GUIProperty("_ToonHueBRG", "Shifted Colour Mix");
							GUIProperty("_ToonHueRBG", "Shifted Colour Mix");
							GUIProperty("_ToonHueGRB", "Shifted Colour Mix");
							GUIProperty("_ToonHueBGR", "Shifted Colour Mix");
							EditorGUILayout.Space();

							EditorGUI.indentLevel--;
						}
						GUIProperty("_ToonBrightness", "Adjusts the overall brightness");
						GUIProperty("_ToonWhiten", "Blends in some subtle white shading to dark areas");

						EditorGUI.indentLevel--;
					}
					EditorGUILayout.Space();

					SetProperty("_ToonLightingGroup", EditorGUILayout.Foldout(GetProperty("_ToonLightingGroup").floatValue == 1, GetProperty("_ToonLightingGroup").displayName, true) ? 1 : 0);
					if (GetProperty("_ToonLightingGroup").floatValue == 1)
					{
						EditorGUI.indentLevel++;

						GUIProperty("_ToonLighting", "Simulates a toon lighting effect by reducing the lighting to 3 discrete steps");
						EditorGUI.BeginDisabledGroup(GetProperty("_ToonLighting").floatValue == 0);
						GUIProperty("_ToonLightingHigh", "The bright light colour");
						GUIProperty("_ToonLightingMid", "The normal light colour");
						GUIProperty("_ToonLightingShadow", "The shadow colour");
						EditorGUILayout.Space();
						GUIProperty("_ToonLightingHighLevel", "The threshold between normal and bright light");
						GUIProperty("_ToonLightingHighSoftEdge", "Blur the toon lighting bright transitional edge");
						GUIProperty("_ToonLightingShadowLevel", "The threshold between light and shadow");
						GUIProperty("_ToonLightingShadowSoftEdge", "Blur the toon lighting shadow transitional edge");
						EditorGUI.EndDisabledGroup();

						EditorGUI.indentLevel--;
					}
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.Space();

					EditorGUI.indentLevel--;
				}
				EditorGUILayout.Space();
				EditorGUILayout.Space();
			}


			//--------------------------------------------------------------------------------
			// Texture Utilities
			SetProperty("_UtilitiesGroup", EditorGUILayout.Foldout(GetProperty("_UtilitiesGroup").floatValue == 1, GetProperty("_UtilitiesGroup").displayName, true, headingStyle) ? 1 : 0);
			if (GetProperty("_UtilitiesGroup").floatValue == 1)
			{
				EditorGUI.indentLevel++;
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_UtilitySourceMap").displayName, "The source map"), GetProperty("_UtilitySourceMap"));
				editor.TexturePropertySingleLine(EditorGUIUtility.TrTextContent(GetProperty("_UtilityTargetMap").displayName, "The target map"), GetProperty("_UtilityTargetMap"));
				bool sourceNull = GetProperty("_UtilitySourceMap").textureValue == null;

				if (GetProperty("_UtilityTargetMap").textureValue == null)
				{
					EditorGUILayout.Space();
					GUIProperty("_UtilityNewResolution", "New Copy Resolution");
					EditorGUILayout.Space();
					//--------------------------------------------------------------------------------
					// Make a new texture copy
					//--------------------------------------------------------------------------------

					if (GUILayout.Button(sourceNull ? "Click here to create a blank white target texture" : "Click here to create a new copy of the source texture"))
					{
						int resolution = 256 * (int)(Mathf.Pow(2, GetProperty("_UtilityNewResolution").floatValue));
						string newAssetPath = "Assets/FastFur_" + this.editor.target.name + "_BlankMap";
						byte[] bytes;

						if (!sourceNull)
						{
							RenderTexture newRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(GetProperty("_UtilitySourceMap").textureValue, newRenderTexture);

							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							outputTexture.filterMode = FilterMode.Point;
							RenderTexture.active = newRenderTexture;
							outputTexture.ReadPixels(new Rect(0, 0, outputTexture.width, outputTexture.height), 0, 0);
							outputTexture.Apply();
							RenderTexture.active = null;

							bytes = outputTexture.EncodeToPNG();

							string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilitySourceMap").textureValue.GetInstanceID());
							newAssetPath = assetPath.Substring(0, assetPath.IndexOf(".", assetPath.Length - 4)) + "_Copy";
						}
						else
						{
							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);

							var newPixels = outputTexture.GetPixels();
							var newColour = new Color(1f, 1f, 1f, 1f);
							for (int x = 0; x < newPixels.Length; x++) newPixels[x] = newColour;

							outputTexture.SetPixels(newPixels);

							bytes = outputTexture.EncodeToPNG();
						}

						int tries = 0;
						while (File.Exists(newAssetPath + ".png") && tries < 20)
						{
							newAssetPath += "_Copy";
							tries++;
						}
						if (tries < 20)
						{
							System.IO.File.WriteAllBytes(newAssetPath + ".png", bytes);
							AssetDatabase.Refresh();

							Texture2D myTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newAssetPath + ".png");
							((Material)this.editor.target).SetTexture("_UtilityTargetMap", myTexture);
						}
					}
				}

				else if (!sourceNull)
				{
					EditorGUILayout.HelpBox("Warning! The target texture will be overwritten and converted to ARGB32 format! If an error occurs you may lose the contents permanently! DO NOT CONTINUE without making a backup first!", MessageType.Warning);

					EditorGUILayout.Space();
					GUIProperty("_UtilityFunction", "Function to perform");
					int function = (int)GetProperty("_UtilityFunction").floatValue;
					EditorGUILayout.Space();

					if (function == 0)
					{
						//--------------------------------------------------------------------------------
						// Copy a channel
						//--------------------------------------------------------------------------------
						GUIProperty("_UtilityInvert", "Inverts the copy, so that 0=1 and 1=0");
						GUIProperty("_UtilitySourceChannel", "Source channel");
						GUIProperty("_UtilityTargetChannel", "Target channel");
						EditorGUILayout.Space();

						if (GUILayout.Button("Click here to copy the Source Channel to the Target Channel"))
						{
							int resolution = GetProperty("_UtilityTargetMap").textureValue.width;
							RenderTexture sourceRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							RenderTexture targetRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(GetProperty("_UtilitySourceMap").textureValue, sourceRenderTexture);
							Graphics.Blit(GetProperty("_UtilityTargetMap").textureValue, targetRenderTexture);

							Texture2D sourceTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							Texture2D targetTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);

							sourceTexture.filterMode = FilterMode.Point;
							targetTexture.filterMode = FilterMode.Point;

							RenderTexture.active = sourceRenderTexture;
							sourceTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							sourceTexture.Apply();
							RenderTexture.active = targetRenderTexture;
							targetTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							targetTexture.Apply();
							RenderTexture.active = null;

							Color[] sourcePixels = sourceTexture.GetPixels(0, 0, resolution, resolution);
							Color[] targetPixels = targetTexture.GetPixels(0, 0, resolution, resolution);

							int sourceChannel = (int)GetProperty("_UtilitySourceChannel").floatValue;
							int targetChannel = (int)GetProperty("_UtilityTargetChannel").floatValue;
							bool invert = GetProperty("_UtilityInvert").floatValue > 0;
							for (int x = 0; x < resolution; x++)
							{
								for (int y = 0; y < resolution; y++)
								{
									int index = x + y * resolution;

									float value = 0;
									if (sourceChannel == 0) value = sourcePixels[index].r;
									if (sourceChannel == 1) value = sourcePixels[index].g;
									if (sourceChannel == 2) value = sourcePixels[index].b;
									if (sourceChannel == 3) value = sourcePixels[index].a;

									if (invert) value = 1 - value;

									if (targetChannel == 0) targetPixels[index].r = value;
									if (targetChannel == 1) targetPixels[index].g = value;
									if (targetChannel == 2) targetPixels[index].b = value;
									if (targetChannel == 3) targetPixels[index].a = value;
								}
							}


							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							outputTexture.SetPixels(targetPixels);

							byte[] bytes = outputTexture.EncodeToPNG();
							if (bytes.Length > 128)
							{
								string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilityTargetMap").textureValue.GetInstanceID());
								System.IO.File.WriteAllBytes(assetPath, bytes);
								AssetDatabase.Refresh();
							}
						}
					}
					else if (function == 1)
					{
						//--------------------------------------------------------------------------------
						// Apply a mask
						//--------------------------------------------------------------------------------
						GUIProperty("_UtilitySourceMask", "Source mask channel");
						GUIProperty("_UtilityMaskType", "Target channel");
						GUIProperty("_UtilityMaskThreshold", "Mask cutoff threshold");
						GUIProperty("_UtilityTargetChannel", "Target channel");
						EditorGUILayout.Space();
						if (GUILayout.Button("Click here to apply the Source Mask to the Target Channel"))
						{
							int resolution = GetProperty("_UtilityTargetMap").textureValue.width;
							RenderTexture sourceRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							RenderTexture targetRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(GetProperty("_UtilitySourceMap").textureValue, sourceRenderTexture);
							Graphics.Blit(GetProperty("_UtilityTargetMap").textureValue, targetRenderTexture);

							Texture2D sourceTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							Texture2D targetTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);

							sourceTexture.filterMode = FilterMode.Point;
							targetTexture.filterMode = FilterMode.Point;

							RenderTexture.active = sourceRenderTexture;
							sourceTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							sourceTexture.Apply();
							RenderTexture.active = targetRenderTexture;
							targetTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							targetTexture.Apply();
							RenderTexture.active = null;

							Color[] sourcePixels = sourceTexture.GetPixels(0, 0, resolution, resolution);
							Color[] targetPixels = targetTexture.GetPixels(0, 0, resolution, resolution);

							int channel = (int)GetProperty("_UtilitySourceMask").floatValue;
							float threshold = GetProperty("_UtilityMaskThreshold").floatValue;
							bool ifAbove = GetProperty("_UtilityMaskType").floatValue > 0.5;
							int targetChannel = (int)GetProperty("_UtilityTargetChannel").floatValue;

							for (int x = 0; x < resolution; x++)
							{
								for (int y = 0; y < resolution; y++)
								{
									int index = x + y * resolution;

									float maskValue = 0;
									if (channel == 0) maskValue = sourcePixels[index].r;
									if (channel == 1) maskValue = sourcePixels[index].g;
									if (channel == 2) maskValue = sourcePixels[index].b;
									if (channel == 3) maskValue = sourcePixels[index].a;

									if (maskValue <= threshold ^ ifAbove)
									{
										if (targetChannel == 0) targetPixels[index].r = 0;
										if (targetChannel == 1) targetPixels[index].g = 0;
										if (targetChannel == 2) targetPixels[index].b = 0;
										if (targetChannel == 3) targetPixels[index].a = 0;
									}
								}
							}

							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							outputTexture.SetPixels(targetPixels);

							byte[] bytes = outputTexture.EncodeToPNG();
							if (bytes.Length > 128)
							{
								string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilityTargetMap").textureValue.GetInstanceID());
								System.IO.File.WriteAllBytes(assetPath, bytes);
								AssetDatabase.Refresh();
							}
						}
					}
					else if (function <= 4)
					{
						//--------------------------------------------------------------------------------
						// Re-scale a channel
						//--------------------------------------------------------------------------------
						GUIProperty("_UtilityReScale", "Re-scaling factor");
						EditorGUILayout.Space();
						if (GUILayout.Button("Click here to re-scale the Source " + (function == 2 ? "Combing Strength" : function == 3 ? "Length" : "Density") + " onto the Target"))
						{
							int resolution = GetProperty("_UtilityTargetMap").textureValue.width;
							RenderTexture sourceRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							RenderTexture targetRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(GetProperty("_UtilitySourceMap").textureValue, sourceRenderTexture);
							Graphics.Blit(GetProperty("_UtilityTargetMap").textureValue, targetRenderTexture);

							Texture2D sourceTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							Texture2D targetTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);

							sourceTexture.filterMode = FilterMode.Point;
							targetTexture.filterMode = FilterMode.Point;

							RenderTexture.active = sourceRenderTexture;
							sourceTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							sourceTexture.Apply();
							RenderTexture.active = targetRenderTexture;
							targetTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							targetTexture.Apply();
							RenderTexture.active = null;

							Color[] sourcePixels = sourceTexture.GetPixels(0, 0, resolution, resolution);
							Color[] targetPixels = targetTexture.GetPixels(0, 0, resolution, resolution);

							float scale = GetProperty("_UtilityReScale").floatValue;

							for (int x = 0; x < resolution; x++)
							{
								for (int y = 0; y < resolution; y++)
								{
									int index = x + y * resolution;

									if (function == 3) targetPixels[index].b = sourcePixels[index].b * scale;
									if (function == 4) targetPixels[index].a = Mathf.Round(sourcePixels[index].a * scale * 64) / 64;
									if (function == 2)
									{
										Vector2 combing = new Vector2(sourcePixels[index].r * 2 - 1, sourcePixels[index].g * 2 - 1);
										combing = combing.normalized * Mathf.Min(1, combing.magnitude * scale);
										targetPixels[index].r = combing.x * 0.5f + 0.5f;
										targetPixels[index].g = combing.y * 0.5f + 0.5f;
									}
								}
							}

							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							outputTexture.SetPixels(targetPixels);

							byte[] bytes = outputTexture.EncodeToPNG();
							if (bytes.Length > 128)
							{
								string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilityTargetMap").textureValue.GetInstanceID());
								System.IO.File.WriteAllBytes(assetPath, bytes);
								AssetDatabase.Refresh();
							}
						}
					}
					else if (function == 5)
					{
						//--------------------------------------------------------------------------------
						// Fill Target Channel
						//--------------------------------------------------------------------------------
						GUIProperty("_UtilityTargetChannel", "Target channel");
						GUIProperty("_UtilityValue", "Value to write into the target channel");
						float value = GetProperty("_UtilityValue").floatValue;
						EditorGUILayout.Space();
						if (GUILayout.Button("Click here to Fill the Target Channel"))
						{
							int resolution = GetProperty("_UtilityTargetMap").textureValue.width;

							RenderTexture targetRenderTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
							Graphics.Blit(GetProperty("_UtilityTargetMap").textureValue, targetRenderTexture);

							Texture2D targetTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							targetTexture.filterMode = FilterMode.Point;

							RenderTexture.active = targetRenderTexture;
							targetTexture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
							targetTexture.Apply();
							RenderTexture.active = null;
							Color[] targetPixels = targetTexture.GetPixels(0, 0, resolution, resolution);

							int targetChannel = (int)GetProperty("_UtilityTargetChannel").floatValue;
							for (int x = 0; x < resolution; x++)
							{
								for (int y = 0; y < resolution; y++)
								{
									int index = x + y * resolution;

									if (targetChannel == 0) targetPixels[index].r = value;
									if (targetChannel == 1) targetPixels[index].g = value;
									if (targetChannel == 2) targetPixels[index].b = value;
									if (targetChannel == 3) targetPixels[index].a = value;
								}
							}

							Texture2D outputTexture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, true);
							outputTexture.SetPixels(targetPixels);

							byte[] bytes = outputTexture.EncodeToPNG();
							if (bytes.Length > 128)
							{
								string assetPath = AssetDatabase.GetAssetPath(GetProperty("_UtilityTargetMap").textureValue.GetInstanceID());
								System.IO.File.WriteAllBytes(assetPath, bytes);
								AssetDatabase.Refresh();
							}
						}
					}
				}
				EditorGUI.indentLevel--;
			}
			EditorGUILayout.Space();
			EditorGUILayout.Space();


			//--------------------------------------------------------------------------------
			// Debugging
			if (variant != Variant.UltraLite)
			{
				SetProperty("_DebuggingGroup", EditorGUILayout.Foldout(GetProperty("_DebuggingGroup").floatValue == 1, GetProperty("_DebuggingGroup").displayName, true, headingStyle) ? 1 : 0);
				if (GetProperty("_DebuggingGroup").floatValue == 1)
				{
					EditorGUI.indentLevel++;
					GUIProperty("_DebuggingLog", "Send debugging messages to the console.");
					EditorGUILayout.Space();

					GUIProperty("_FurDebugDistance", "Used for debugging. The colours indicate the number of layers being rendered.");
					GUIProperty("_FurDebugMipMap", "Used for debugging. The colours indicate which mipmap level the video card is using.");
					GUIProperty("_FurDebugHairMap", "Used for debugging. The colours indicate which hair map (fine, coarse, very coarse) is being used.");
					GUIProperty("_FurDebugVerticies", "Used for debugging. Show the locations of the vertices.");
					GUIProperty("_FurDebugTopLayer", "Used for debugging. Only render the top layer.");
					GUIProperty("_FurDebugUpperLimit", "Used for debugging. Show outline of the highest rendered layer.");
					GUIProperty("_FurDebugDepth", "Used for debugging. The colours indicate the layer depth levels.");
					GUIProperty("_FurDebugQuality", "The colours indicate quality level, as reported by VR Chat.");
					GUIProperty("_FurDebugLength", "The colours indicate different levels of the fur length data.");
					GUIProperty("_FurDebugDensity", "The colours indicate different levels of the fur density data.");
					GUIProperty("_FurDebugCombing", "The colours indicate different levels of the fur combing data.");
					EditorGUILayout.Space();

					EditorGUI.BeginDisabledGroup(false);
					//GUIProperty("_HairDensityThreshold","The height level where 75% of the hairs are shorter.");
					//GUIProperty("_WindDirectionActual","Pre-calculated Vector4 wind direction");
					//GUIProperty("_MarkingsMapHash","Hash code (to check if texture has changed)");
					//GUIProperty("_MarkingsMapPositiveCutoff","The maximum height of 99% of hairs when height is positive");
					//GUIProperty("_MarkingsMapNegativeCutoff","The maximum height of 99% of hairs when height is negative");
					//GUIProperty("_FurShapeMapHash","Hash code (to check if texture has changed)");
					//GUIProperty("_HairMapScaling3","Hash Map scaling for mip map 3");
					//GUIProperty("_HairMapScaling4","Hash Map scaling for mip map 4");
					//GUIProperty("_HairMapScaling5","Hash Map scaling for mip map 5");
					//GUIProperty("_HairMapHash","Hash code (to check if texture has changed)");
					EditorGUILayout.Space();

					EditorGUI.EndDisabledGroup();
					GUIProperty("_OverrideScale", "Overrides the fur thickness scaling by applying a multiplier");
					GUIProperty("_OverrideQualityBias", "Overrides the quality by adding extra level of detail");
					GUIProperty("_OverrideDistanceBias", "Overrides the view distance by adding extra distance");
					if (GetProperty("_OverrideDistanceBias").floatValue < 0 || GetProperty("_OverrideQualityBias").floatValue > 0)
					{
						EditorGUILayout.HelpBox("Please do not use Higher Quality versions of the shader when you are in public game lobbies!", MessageType.Warning);
					}

					EditorGUILayout.Space();
					GUIProperty("_TS1", "This probably does nothing. I use it when I need a slider for testing.");
					GUIProperty("_TS2", "This probably does nothing. I use it when I need a slider for testing.");
					GUIProperty("_TS3", "This probably does nothing. I use it when I need a slider for testing.");

					EditorGUI.indentLevel--;
				}
				EditorGUILayout.Space();

				if (GetProperty("_OverrideQualityBias").floatValue != 0 || GetProperty("_OverrideDistanceBias").floatValue != 0 || GetProperty("_OverrideScale").floatValue != 1)
				{
					EditorGUILayout.HelpBox("Debugging overrides are active! Setting these incorrectly can cause visual errors and/or performance drop.", MessageType.Warning);

					if (GUILayout.Button("Click here to reset debugging overrides"))
					{
						SetProperty("_OverrideScale", 1.0f);
						SetProperty("_OverrideQualityBias", 0.0f);
						SetProperty("_OverrideDistanceBias", 0.0f);
					}
				}

				EditorGUILayout.Space();
			}

			if (!Application.isPlaying || !initialized) CheckImports();
			CalculateQuickStuff();
			CheckKeywords(targetMat);
			Shader.SetGlobalFloat("_VRChatCameraMode", -1f);

			//--------------------------------------------------------------------------------
			// Check various imports and calculations. Limit this to once per second.
			if (!initialized || Mathf.Abs(Time.realtimeSinceStartup - lastValidationTime) > 1.0)
			{
				CalculateSlowStuff();

				if (File.Exists("Assets/FastFurShader/Fur Grooming (Drag this onto your skinned mesh object).prefab"))
				{
					File.Delete("Assets/FastFurShader/Fur Grooming (Drag this onto your skinned mesh object).prefab");
					AssetDatabase.Refresh();
				}

				initialized = true;
				lastValidationTime = Time.realtimeSinceStartup;
			}

			runningOkay = true;
		}



		//*************************************************************************************************************************************************
		// Generate Hair Maps
		//*************************************************************************************************************************************************
		private void GenerateHairMap()
		{
			GenerateFunctions gen = new GenerateFunctions();

			string assetPathA = "Assets/FastFur_" + this.editor.target.name + "_HairMap.png";
			if (hairDataMapTI != null) assetPathA = hairDataMapTI.assetPath;
			string assetPathB = assetPathA.Replace(".png", "Coarse.png");
			if (hairDataMapCoarseTI != null) assetPathB = hairDataMapCoarseTI.assetPath;

			gen.GenerateHairMap(this, assetPathA, assetPathB);

			AssetDatabase.Refresh();
			Texture2D myTextureA = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPathA);
			((Material)this.editor.target).SetTexture("_HairMap", myTextureA);
			Texture2D myTextureB = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPathB);
			((Material)this.editor.target).SetTexture("_HairMapCoarse", myTextureB);

			CheckImports();
		}



		//*************************************************************************************************************************************************
		// Generate a Fur Data Map
		//*************************************************************************************************************************************************
		public void GenerateFurDataMap()
		{
			Texture2D newTexture = new Texture2D(1024, 1024, TextureFormat.ARGB32, false, true);

			var newPixels = newTexture.GetPixels();
			var newColour = new Color(0.498f, 0.498f, 0.7f, 0.498f);
			for (int x = 0; x < newPixels.Length; x++) newPixels[x] = newColour;

			newTexture.SetPixels(newPixels);
			newTexture.Apply();

			byte[] bytes = newTexture.EncodeToPNG();
			string assetPath = "Assets/FastFur_" + this.editor.target.name + "_FurShape.png";
			if (furDataMapTI != null) assetPath = furDataMapTI.assetPath;

			System.IO.File.WriteAllBytes(assetPath, bytes);

			AssetDatabase.Refresh();
			Texture2D myTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
			((Material)this.editor.target).SetTexture("_FurShapeMap", myTexture);

			CheckImports();
		}



		//*************************************************************************************************************************************************
		// Check if various features are on or off. If not, then use a variant that strips out the unused code.
		//*************************************************************************************************************************************************
		private void CheckKeywords(Material targetMat)
		{
			if (variant == Variant.UltraLite) return;

			// Emission is handled during the GUI processing, because we want it to behave like the Unity Standard shader GUI.
			// Other "Standard" shader keywords, such as _GLOSSYREFLECTIONS_OFF, are handled by using [ToggleOff] in the shader properties.

			if (markingsMapTI != null) SetProperty("_FurMarkingsActive", 1);
			else SetProperty("_FurMarkingsActive", 0);

			if (hairDataMapCoarseTI != null && GetProperty("_HairMapCoarseStrength").floatValue > 0) SetProperty("_CoarseMapActive", 1);
			else SetProperty("_CoarseMapActive", 0);

			bool debugging = GetProperty("_FurDebugDistance").floatValue == 1;
			debugging |= GetProperty("_FurDebugVerticies").floatValue == 1;
			debugging |= GetProperty("_FurDebugMipMap").floatValue == 1;
			debugging |= GetProperty("_FurDebugHairMap").floatValue == 1;
			debugging |= GetProperty("_FurDebugDensity").floatValue == 1;
			debugging |= GetProperty("_FurDebugLength").floatValue == 1;
			debugging |= GetProperty("_FurDebugDepth").floatValue == 1;
			debugging |= GetProperty("_FurDebugTopLayer").floatValue == 1;
			debugging |= GetProperty("_FurDebugUpperLimit").floatValue == 1;
			debugging |= GetProperty("_FurDebugCombing").floatValue == 1;
			debugging |= GetProperty("_FurDebugQuality").floatValue == 1;

			if (debugging) targetMat.EnableKeyword("FASTFUR_DEBUGGING");
			else targetMat.DisableKeyword("FASTFUR_DEBUGGING");

			if (normalMapTI != null && GetProperty("_PBSSkin").floatValue > 0) targetMat.EnableKeyword("_NORMALMAP");
			else targetMat.DisableKeyword("_NORMALMAP");

			if (metallicMapTI != null) targetMat.EnableKeyword("_METALLICGLOSSMAP");
			else targetMat.DisableKeyword("_METALLICGLOSSMAP");

			if (specularMapTI != null) targetMat.EnableKeyword("_SPECGLOSSMAP");
			else targetMat.DisableKeyword("_SPECGLOSSMAP");

			if (specularMapTI != null) targetMat.EnableKeyword("_SPECGLOSSMAP");
			else targetMat.DisableKeyword("_SPECGLOSSMAP");

			if (GetProperty("_SmoothnessTextureChannel").floatValue == (float)SmoothnessMapChannel.AlbedoAlpha) targetMat.EnableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
			else targetMat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");

			if (GetProperty("_TwoSided").floatValue > 0)
			{
				targetMat.EnableKeyword("FXAA");
				SetProperty("_Cull", 0);
			}
			else
			{
				targetMat.DisableKeyword("FXAA");
				SetProperty("_Cull", 2);
			}

			int maxLayers = (int)GetProperty("_MaximumLayers").floatValue;
			if (pipeline == Pipeline.Pipeline3 || maxLayers == 2)
			{
				targetMat.DisableKeyword("FXAA_LOW");
				targetMat.DisableKeyword("FXAA_KEEP_ALPHA");
			}
			else if (maxLayers == 0)
			{
				targetMat.EnableKeyword("FXAA_LOW");
				targetMat.DisableKeyword("FXAA_KEEP_ALPHA");
			}
			else
			{
				targetMat.DisableKeyword("FXAA_LOW");
				targetMat.EnableKeyword("FXAA_KEEP_ALPHA");
			}

			// Only Opaque and Cutout modes are supported
			if (GetProperty("_Mode").floatValue > 1) SetProperty("_Mode", 0);
			if (GetProperty("_Mode").floatValue == 0)
			{
				targetMat.DisableKeyword("_ALPHATEST_ON");
				targetMat.DisableKeyword("_ALPHABLEND_ON");
				targetMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			}
			else
			{
				targetMat.EnableKeyword("_ALPHATEST_ON");
				targetMat.DisableKeyword("_ALPHABLEND_ON");
				targetMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			}
		}




		//*************************************************************************************************************************************************
		// Check that the various texture imports are configured correctly (ex. some need to be linear, some need to be no compression)
		//*************************************************************************************************************************************************
		private void CheckImports()
		{
			furDataMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_FurShapeMap").textureValue));
			hairDataMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_HairMap").textureValue));
			if (variant != Variant.UltraLite)
			{
				furDataMask1TI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_FurShapeMask1").textureValue));
				furDataMask2TI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_FurShapeMask2").textureValue));
				hairDataMapCoarseTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_HairMapCoarse").textureValue));
				markingsMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_MarkingsMap").textureValue));
				normalMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_BumpMap").textureValue));
				metallicMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_MetallicGlossMap").textureValue));
				specularMapTI = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(GetProperty("_SpecGlossMap").textureValue));
			}

			// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
			if (furDataMapTI is TextureImporter && furDataMapTI != null)
			{
				if (((TextureImporter)furDataMapTI).sRGBTexture || ((TextureImporter)furDataMapTI).crunchedCompression || ((TextureImporter)furDataMapTI).textureCompression != TextureImporterCompression.Compressed
				|| !((TextureImporter)furDataMapTI).streamingMipmaps)
				{
					((TextureImporter)furDataMapTI).sRGBTexture = false;
					((TextureImporter)furDataMapTI).crunchedCompression = false;
					((TextureImporter)furDataMapTI).textureCompression = TextureImporterCompression.Compressed;
					((TextureImporter)furDataMapTI).streamingMipmaps = true;
					furDataMapTI.SaveAndReimport();
				}
			}

			if (variant != Variant.UltraLite)
			{
				// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
				if (furDataMask1TI is TextureImporter && furDataMask1TI != null)
				{
					if (((TextureImporter)furDataMask1TI).sRGBTexture || ((TextureImporter)furDataMask1TI).crunchedCompression || ((TextureImporter)furDataMask1TI).textureCompression != TextureImporterCompression.Compressed
					|| !((TextureImporter)furDataMask1TI).streamingMipmaps)
					{
						((TextureImporter)furDataMask1TI).sRGBTexture = false;
						((TextureImporter)furDataMask1TI).crunchedCompression = false;
						((TextureImporter)furDataMask1TI).textureCompression = TextureImporterCompression.Compressed;
						((TextureImporter)furDataMask1TI).streamingMipmaps = true;
						furDataMask1TI.SaveAndReimport();
					}
				}

				// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
				if (furDataMask2TI is TextureImporter && furDataMask2TI != null)
				{
					if (((TextureImporter)furDataMask2TI).sRGBTexture || ((TextureImporter)furDataMask2TI).crunchedCompression || ((TextureImporter)furDataMask2TI).textureCompression != TextureImporterCompression.Compressed
					|| !((TextureImporter)furDataMask2TI).streamingMipmaps)
					{
						((TextureImporter)furDataMask2TI).sRGBTexture = false;
						((TextureImporter)furDataMask2TI).crunchedCompression = false;
						((TextureImporter)furDataMask2TI).textureCompression = TextureImporterCompression.Compressed;
						((TextureImporter)furDataMask2TI).streamingMipmaps = true;
						furDataMask2TI.SaveAndReimport();
					}
				}
			}


			int mipType = (int)GetProperty("_HairMipType").floatValue; // 0 = Box, 1 = Kaiser

			// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
			if (hairDataMapTI is TextureImporter && hairDataMapTI != null)
			{
				if (((TextureImporter)hairDataMapTI).sRGBTexture || ((TextureImporter)hairDataMapTI).crunchedCompression || ((TextureImporter)hairDataMapTI).textureCompression != TextureImporterCompression.CompressedHQ
				|| !((TextureImporter)hairDataMapTI).streamingMipmaps || ((TextureImporter)hairDataMapTI).mipmapFilter != (TextureImporterMipFilter)mipType || !((TextureImporter)hairDataMapTI).isReadable)
				{
					((TextureImporter)hairDataMapTI).sRGBTexture = false;
					((TextureImporter)hairDataMapTI).textureCompression = TextureImporterCompression.CompressedHQ;
					((TextureImporter)hairDataMapTI).crunchedCompression = false;
					((TextureImporter)hairDataMapTI).streamingMipmaps = true;
					((TextureImporter)hairDataMapTI).mipmapFilter = (TextureImporterMipFilter)mipType;
					((TextureImporter)hairDataMapTI).isReadable = true;
					hairDataMapTI.SaveAndReimport();
				}
			}

			// This should always be a TextureImporter, but we check, just in case somebody makes a weird texture type. If they do, then it's up to them to set this stuff correctly.
			if (variant != Variant.UltraLite)
			{
				if (hairDataMapCoarseTI is TextureImporter && hairDataMapCoarseTI != null)
				{
					if (((TextureImporter)hairDataMapCoarseTI).sRGBTexture || ((TextureImporter)hairDataMapCoarseTI).crunchedCompression || ((TextureImporter)hairDataMapCoarseTI).textureCompression != TextureImporterCompression.CompressedHQ
					|| !((TextureImporter)hairDataMapCoarseTI).streamingMipmaps || ((TextureImporter)hairDataMapCoarseTI).mipmapFilter != (TextureImporterMipFilter)mipType)
					{
						((TextureImporter)hairDataMapCoarseTI).sRGBTexture = false;
						((TextureImporter)hairDataMapCoarseTI).textureCompression = TextureImporterCompression.CompressedHQ;
						((TextureImporter)hairDataMapCoarseTI).crunchedCompression = false;
						((TextureImporter)hairDataMapCoarseTI).streamingMipmaps = true;
						((TextureImporter)hairDataMapCoarseTI).mipmapFilter = (TextureImporterMipFilter)mipType;
						hairDataMapCoarseTI.SaveAndReimport();
					}
				}
			}
		}



		//*************************************************************************************************************************************************
		// Check or reset default property values
		//*************************************************************************************************************************************************
		bool CheckDefaults(String[] propertyNames)
		{
			bool isDefault = true;

			foreach (String propertyName in propertyNames)
			{
				int index = FindPropertyIndex(propertyName);

				if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Float || targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Range)
				{
					if (GetProperty(propertyName).floatValue != targetMat.shader.GetPropertyDefaultFloatValue(index)) isDefault = false;
				}
				if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Vector)
				{
					if (GetProperty(propertyName).vectorValue != targetMat.shader.GetPropertyDefaultVectorValue(index)) isDefault = false;
				}
				if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Color)
				{
					Vector4 colour = targetMat.shader.GetPropertyDefaultVectorValue(index);
					if (!GetProperty(propertyName).colorValue.Equals(new Color(colour.x, colour.y, colour.z, colour.w))) isDefault = false;
				}
			}

			return isDefault;
		}

		void SetDefaults(String[] propertyNames)
		{
			foreach (String propertyName in propertyNames)
			{
				int index = FindPropertyIndex(propertyName);

				if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Float || targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Range)
				{
					SetProperty(propertyName, targetMat.shader.GetPropertyDefaultFloatValue(index));
				}
				if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Vector)
				{
					SetProperty(propertyName, targetMat.shader.GetPropertyDefaultVectorValue(index));
				}
				if (targetMat.shader.GetPropertyType(index) == UnityEngine.Rendering.ShaderPropertyType.Color)
				{
					Vector4 colour = targetMat.shader.GetPropertyDefaultVectorValue(index);
					SetProperty(propertyName, new Color(colour.x, colour.y, colour.z, colour.w));
				}
			}
		}



		//*************************************************************************************************************************************************
		// Calculate various properties automatically
		//*************************************************************************************************************************************************
		private void CalculateQuickStuff()
		{
			if (variant != Variant.UltraLite)
			{
				// Ensure that the hair fade sliders remain in the correct order
				float[] hairFade = new float[4];
				hairFade[0] = GetProperty("_HairRootPoint").floatValue;
				hairFade[1] = GetProperty("_HairMidLowPoint").floatValue;
				hairFade[2] = GetProperty("_HairMidHighPoint").floatValue;
				hairFade[3] = GetProperty("_HairTipPoint").floatValue;
				bool doCheck = true;
				while (doCheck)
				{
					doCheck = false;
					for (int x = 2; x >= 0; x--)
					{
						if (hairFade[x] >= hairFade[x + 1])
						{
							if (hairFade[x] == recentFadePoints[x]) hairFade[x] = hairFade[x + 1] - 0.001f;
							else hairFade[x + 1] = hairFade[x] + 0.001f;
							doCheck = true;
						}
					}

					for (int x = 0; x <= 2; x++)
					{
						if (hairFade[x + 1] <= hairFade[x])
						{
							if (hairFade[x] == recentFadePoints[x]) hairFade[x] = hairFade[x + 1] - 0.001f;
							else hairFade[x + 1] = hairFade[x] + 0.001f;
							doCheck = true;
						}
					}
					if (doCheck)
					{
						SetProperty("_HairRootPoint", Mathf.Max(0.000f, Mathf.Min(0.997f, hairFade[0])));
						SetProperty("_HairMidLowPoint", Mathf.Max(0.001f, Mathf.Min(0.998f, hairFade[1])));
						SetProperty("_HairMidHighPoint", Mathf.Max(0.002f, Mathf.Min(0.999f, hairFade[2])));
						SetProperty("_HairTipPoint", Mathf.Max(0.003f, Mathf.Min(1.000f, hairFade[3])));
					}
				}
				recentFadePoints = hairFade;


				// Calibrate the Hue sliders so that the total brightness remains the same
				float[] toonHues = new float[6];
				toonHues[0] = GetProperty("_ToonHueRGB").floatValue;
				toonHues[1] = GetProperty("_ToonHueGBR").floatValue;
				toonHues[2] = GetProperty("_ToonHueBRG").floatValue;
				toonHues[3] = GetProperty("_ToonHueRBG").floatValue;
				toonHues[4] = GetProperty("_ToonHueBGR").floatValue;
				toonHues[5] = GetProperty("_ToonHueGRB").floatValue;
				float total = 0;
				foreach (float toonHue in toonHues) total += toonHue;
				if (Mathf.Abs(total - 1) > 0.001)
				{
					float weightAdjusted = total - 1;

					int z = 0;
					for (z = 0; z < 10; z++)
					{
						float selectedWeight = 0;
						float recentSelectedWeight = 0;
						total = 0;

						for (int x = 0; x < 6; x++)
						{
							if (toonHues[x] != recentToonHues[x])
							{
								selectedWeight = toonHues[x];
								recentSelectedWeight = recentToonHues[x];
							}
							total += toonHues[x];
						}
						if (Mathf.Abs(total - 1) > 0.001)
						{
							if (Mathf.Abs(recentSelectedWeight - 1) < 0.001)
							{
								if (toonHues[0] == 0)
								{
									toonHues[0] = 1 - selectedWeight;
									recentToonHues[0] = 1 - selectedWeight;
								}
								else
								{
									toonHues[1] = 1 - selectedWeight;
									recentToonHues[1] = 1 - selectedWeight;
								}
							}
							else
							{
								float otherWeights = total - selectedWeight;
								for (int x = 0; x < 6; x++)
								{
									if (toonHues[x] == recentToonHues[x]) toonHues[x] -= (weightAdjusted * toonHues[x]) / otherWeights;
								}
							}
						}
						else break;
					}

					if (z == 10)
					{
						toonHues[0] = 1.0f;
						for (int x = 1; x < 6; x++) toonHues[x] = 0f;
					}

					SetProperty("_ToonHueRGB", toonHues[0]);
					SetProperty("_ToonHueGBR", toonHues[1]);
					SetProperty("_ToonHueBRG", toonHues[2]);
					SetProperty("_ToonHueRBG", toonHues[3]);
					SetProperty("_ToonHueBGR", toonHues[4]);
					SetProperty("_ToonHueGRB", toonHues[5]);
				}
				recentToonHues = toonHues;


				// Calculate Sin and Cos of the fur markings rotation
				float _MarkingsRotation = GetProperty("_MarkingsRotation").floatValue / 180 * Mathf.PI;
				SetProperty("_Sin", Mathf.Sin(_MarkingsRotation));
				SetProperty("_Cos", Mathf.Cos(_MarkingsRotation));
			}

			// Calculate statistics about the fur thickness
			if (variant == Variant.UltraLite) furThickness = normalMagnitidue * GetProperty("_ScaleCalibration").floatValue * GetProperty("_FurShellSpacing").floatValue * 0.95f;
			else furThickness = normalMagnitidue * GetProperty("_ScaleCalibration").floatValue * GetProperty("_OverrideScale").floatValue * GetProperty("_FurShellSpacing").floatValue * 0.95f;
			layerDensity = Mathf.Max((Mathf.Min(furThickness, 0.025f) + 0.024f) / 2f, (Mathf.Min(0.05f, furThickness) + Mathf.Min(0.035f, furThickness)) / 2f) / furThickness;
		}



		private void CalculateSlowStuff()
		{
			if (Application.isPlaying) return;

			TextureFunctions textureFunctions = new TextureFunctions();

			if (variant != Variant.UltraLite)
			{
				// Which pipeline is this?
				string shaderPath = AssetDatabase.GetAssetPath(Shader.Find("Warren's Fast Fur/Fast Fur - Lite")).Replace("/FastFur-Lite.shader", "");
				string pipelineFilePath = shaderPath + "/FastFur-Pipeline.cginc";
				string fileContents = System.IO.File.ReadAllText(pipelineFilePath);
				Pipeline filePipeline = (Pipeline)(-1);
				if (fileContents.Contains("#define PIPELINE1")) filePipeline = Pipeline.Pipeline1;
				if (fileContents.Contains("#define PIPELINE2")) filePipeline = Pipeline.Pipeline2;
				if (fileContents.Contains("#define PIPELINE3")) filePipeline = Pipeline.Pipeline3;
				bool doWrite = false;
				// If the file copy is invalid, we default to the V2 Pipeline
				if (filePipeline.Equals((Pipeline)(-1)))
				{
					pipeline = Pipeline.Pipeline2;
					doWrite = true;
				}
				// If our properties have been changed by the user, then write those changes
				if (pipeline != prevPipeline && prevPipeline >= Pipeline.Pipeline1)
				{
					doWrite = true;
				}
				// Write the changes. Unfortunately, the older version of Unity that VR Chat uses does not
				// support conditional #pragma statements, so we need to write changes directly to the
				// shader files themselves.
				if (doWrite)
				{
					if (pipeline != Pipeline.Pipeline3 || GetProperty("_ConfirmPipeline").floatValue > 0)
					{
						SetProperty("_RenderPipeline", (int)pipeline);
						fileContents = "#define PIPELINE" + (int)(pipeline + 1) + "\n";
						System.IO.File.WriteAllText(pipelineFilePath, fileContents);
						if ((pipeline.Equals(Pipeline.Pipeline1) || filePipeline.Equals(Pipeline.Pipeline1)) && !pipeline.Equals(filePipeline))
						{
							string[] shaderPaths = new string[2];
							shaderPaths[0] = shaderPath + "/FastFur.shader";
							shaderPaths[1] = shaderPath + "/FastFur-Lite.shader";

							foreach (string path in shaderPaths)
							{
								if (System.IO.File.Exists(path))
								{
									string contents = System.IO.File.ReadAllText(path);
									if (pipeline.Equals(Pipeline.Pipeline1))
									{
										contents = contents.Replace("#pragma hull hull", "//#pragma hull hull");
										contents = contents.Replace("#pragma domain doma", "//#pragma domain doma");
										contents = contents.Replace("////#pragma hull hull", "//#pragma hull hull");
										contents = contents.Replace("////#pragma domain doma", "//#pragma domain doma");
									}
									else
									{
										contents = contents.Replace("//#pragma hull hull", "#pragma hull hull");
										contents = contents.Replace("//#pragma domain doma", "#pragma domain doma");
									}
									System.IO.File.WriteAllText(path, contents);
								}
							}
						}
						AssetDatabase.Refresh();
						prevPipeline = pipeline;
					}
				}
				else
				{
					// Update our properties to the match the file
					if (pipeline != filePipeline)
					{
						pipeline = filePipeline;
						SetProperty("_RenderPipeline", (int)filePipeline);
						SetProperty("_ConfirmPipeline", pipeline.Equals(Pipeline.Pipeline3) ? 1 : 0);
					}
					prevPipeline = pipeline;
				}


				// Fix non-standard property names
				Color oldColour = GetProperty("_Colour").colorValue;
				Color defaultColour = new Color(-1, -1, -1, -1);
				if (!oldColour.Equals(defaultColour))
				{
					SetProperty("_Color", oldColour);
					SetProperty("_Colour", defaultColour);
				}
				oldColour = GetProperty("_EmissionColour").colorValue;
				if (!oldColour.Equals(defaultColour))
				{
					SetProperty("_EmissionColor", oldColour);
					SetProperty("_EmissionColour", defaultColour);
				}


				// Create histograms of the hair length modifying textures and use them to calibrate the maximum hair length
				Texture2D hairMap = (Texture2D)GetProperty("_HairMap").textureValue;

				if (variant != Variant.UltraLite)
				{
					float hairMapHash = GetProperty("_HairMapHash").floatValue;

					Texture2D MarkingsMap = (Texture2D)GetProperty("_MarkingsMap").textureValue;
					float MarkingsMapHash = GetProperty("_MarkingsMapHash").floatValue;

					if (markingsMapTI != null)
					{
						if (MarkingsMapHash != MarkingsMap.imageContentsHash.GetHashCode())
						{
							SetProperty("_MarkingsMapPositiveCutoff", textureFunctions.ValuePosHistogram(MarkingsMap, 0.9999f));
							SetProperty("_MarkingsMapNegativeCutoff", textureFunctions.ValueNegHistogram(MarkingsMap, 0.9999f));
							SetProperty("_MarkingsMapHash", MarkingsMap.imageContentsHash.GetHashCode());
						}
					}
				}
			}

			try
			{
				// Calibrate the fur thickness to the mesh size. This allows the avatar to be scaled and have the fur scale as well.
				//
				bool success = false;
				Renderer targetRenderer = null;

				Transform[] allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>();
				foreach (Transform transform in allTransforms)
				{
					Renderer myRenderer = transform.gameObject.GetComponent<Renderer>() as Renderer;
					if (myRenderer == null) continue;
					if (!myRenderer.enabled) continue;
					Material[] materials = myRenderer.sharedMaterials;
					for (int x = 0; x < materials.Length; x++)
					{
						if (materials[x].GetInstanceID() == targetMat.GetInstanceID())
						{
							DebugMessage("Calibration found matching renderer:  " + myRenderer.name + " (" + x + ")");
							targetRenderer = myRenderer;
							success = true;
							break;
						}
						if (success) break;
					}
					if (success) break;
				}

				if (success)
				{
					float magnitude = 1;
					float magnitudeCheck = 0;

					if (targetRenderer is SkinnedMeshRenderer)
					{
						Mesh bakedMesh = new Mesh();
						((SkinnedMeshRenderer)targetRenderer).BakeMesh(bakedMesh);
						magnitude = bakedMesh.normals[0].magnitude;
						magnitudeCheck = bakedMesh.normals[1].magnitude;
					}

					if (targetRenderer is MeshRenderer)
					{
						MeshFilter meshFilter = targetRenderer.GetComponent<MeshFilter>();
						magnitude = meshFilter.sharedMesh.normals[0].magnitude;
						magnitudeCheck = meshFilter.sharedMesh.normals[1].magnitude;
					}

					normalMagnitidue = magnitude;

					DebugMessage("Calibration normal magnitude equals " + magnitude + " (check = " + magnitudeCheck + ")");

					if (GetProperty("_ScaleCalibration").floatValue < 0) SetProperty("_ScaleCalibration", 0.1f / magnitude);
				}
				else
				{
					DebugMessage("Calibration couldn't find renderer using material:  " + targetMat.name);
					if (GetProperty("_ScaleCalibration").floatValue < 0) SetProperty("_ScaleCalibration", 0.25f);
				}
			}
			catch (Exception e)
			{
				DebugMessage("Calibration encountered an error: " + e);
				if (GetProperty("_ScaleCalibration").floatValue < 0) SetProperty("_ScaleCalibration", 0.25f);
			}
		}
	}
}

#endif