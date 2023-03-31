using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System;
using System.Reflection;


#if UNITY_EDITOR
public class FastFurGrooming : MonoBehaviour
{
	//--------------------------------------------------------------------------------
	// Mesh-related stuff

	public bool disableAllOtherScripts = true;
	public bool showDuplicateMaterialWarning = true;
	public Renderer initialTarget;
	public int overPainting = 16;

	private FastFurMat[] fastFurMats;
	private FastFurMat selectedFastFurMat;
	private Renderer selectedRenderer;
	private Material selectedMaterial;
	private int subMeshIndex;
	private int targetTriangleStartIndex;
	private int targetTriangleEndIndex;
	private int targetVertexStartIndex;
	private int targetVertexEndIndex;
	private MeshCollider myMeshCollider;
	private Mesh bakedMesh;
	private float lastMeshUpdate;

	private GameObject colliderGameObject;

	private int selectedUV = 0;

	private class FastFurMat
	{
		public Renderer renderer { get; set; }
		public Material material { get; set; }
		public int materialIndex { get; set; }
		public string filename { get; set; }
		public Texture albedoOriginalTexture;
		public Texture furShapeOriginalTexture;
	};


	//--------------------------------------------------------------------------------
	// Materials and textures
	private Material distanceMaterial;
	private Material directionMaterial;
	private Material fillMaterial;
	private Material groomingMaterial;
	private Material cursorMaterial;
	private Material fixEdgesMaterial;
	private Material stencilMaterial;
	private Material combMaterial;

	private RenderTexture distanceRenderTexture;
	private RenderTexture directionRenderTexture;
	private RenderTexture fillRenderTexture;
	private RenderTexture fillRenderTextureBuffer;

	private Texture albedoFallbackTexture;
	private RenderTexture albedoRenderTextureTarget;
	private RenderTexture albedoRenderTextureBuffer;
	private RenderTexture albedoRenderTextureFinal;

	private RenderTexture furShapeRenderTexturePreEdit;
	private RenderTexture furShapeRenderTextureBase;
	private RenderTexture furShapeRenderTextureTarget;
	private RenderTexture furShapeRenderTextureBuffer;
	private RenderTexture furShapeRenderTextureFinal;
	private RenderTexture[] furShapeRenderTextureUndo;

	private RenderTexture combRenderTextureTarget;
	private RenderTexture combRenderTextureBuffer;
	private RenderTexture combRenderTextureFinal;
	private RenderTexture[] combRenderTextureHistory;
	private RenderTexture combRenderTextureBlank;

	private Texture2D densityTexture;


	//--------------------------------------------------------------------------------
	// Camera control
	private Camera myCamera;
	private GameObject cameraGameObject;

	private int maxFrameRate = 60;

	[Range(0.0f, 25.0f)]
	public float turnSpeed = 2f;
	private float moveSpeed = 1f;
	private float yaw = 0f;
	private float pitch = 0f;

	public KeyCode forwardKey = KeyCode.W;
	public KeyCode backKey = KeyCode.S;
	public KeyCode leftKey = KeyCode.A;
	public KeyCode rightKey = KeyCode.D;
	public KeyCode upKey = KeyCode.E;
	public KeyCode downKey = KeyCode.Q;


	//--------------------------------------------------------------------------------
	// Command buffers
	private CommandBuffer distanceCommandBuffer;
	private CommandBuffer directionCommandBuffer;
	private CommandBuffer combCommandBuffer;
	private CommandBuffer groomCommandBuffer;


	//--------------------------------------------------------------------------------
	// GUI elements
	private Component[] guiComponents;

	private Slider lengthSlider;
	private Slider densitySlider;
	private Slider combingSlider;

	private Image warningImage;
	private Text warningText;

	private Text helpText;
	private Component helpPopupMove;
	private Component helpPopupSpeed;

	private Text sizeHandleText;
	private Text strengthHandleText;
	private Text falloffHandleText;
	private Text visibilityHandleText;

	private Text lengthHandleText;
	private Text densityHandleText;
	private Text combingHandleText;

	private Toggle lengthToggle;
	private Toggle densityToggle;
	private Toggle combingToggle;

	private Dropdown materialSelector;

	private Button undoButton;
	private Button redoButton;
	private Button saveButton;

	enum BrushMode { Normal, Increase, Decrease, Copy };
	private BrushMode brushMode;
	enum LengthMode { NoMask, UseMask };
	private LengthMode lengthMode;
	enum DensityMode { Absolute, Relative };
	private DensityMode densityMode;
	enum CombingMode { Both, Strength, Direction };
	private CombingMode combingMode;



	//--------------------------------------------------------------------------------
	// Active brush settings
	private float brushRadius = 12.5f;
	private float brushFalloff = 0.25f;
	private float brushStrength = 1.0f;
	private float brushVisibility = 0.5f;
	private float furHeight = 0.0f;
	private float furCombing = 0.5f;
	private float furDensity = 0.5f;

	private bool furHeightEnable = true;
	private bool furCombingEnable = false;
	private bool furDensityEnable = false;

	private int furHeightSetAll = 0;
	private int furCombingSetAll = 0;
	private int furDensitySetAll = 0;

	private bool furMirror = true;
	private bool furSphere = true;
	private bool furShowData = false;

	private float baseDensity = 1.0f;

	private float mirrorX;


	//--------------------------------------------------------------------------------
	// Working variables
	private Vector4 mouseHit;
	private Vector4 oldMouseHitWrite = Vector4.negativeInfinity;
	private Vector4 oldMouseHitComb = Vector4.negativeInfinity;

	private int hitIndex;
	private int hits;

	private bool startedSuccessfully = false;

	private int maxUndoSize = 100;
	private int currentUndo;
	private int highestUndo;
	private bool undoOverflow = false;
	private bool doWrite = false;

	private int validSamples;
	private int maxCombTextures = 10;

	private string filename;
	private float fileNotSaved = 0;

	private int activeGroomers = 0;

	private int activeMaterialIndex = 0;


	//--------------------------------------------------------------------------------
	// Getters and Setters
	public void SetSize(float value)
	{
		brushRadius = value;
	}
	public void SetFalloff(float value)
	{
		brushFalloff = value;
	}
	public void SetStrength(float value)
	{
		brushStrength = value;
	}
	public void SetVisibility(float value)
	{
		brushVisibility = value;
	}
	public void SetHeight(float value)
	{
		furHeight = value;
	}
	public void SetCombing(float value)
	{
		furCombing = value;
	}
	public void SetDensity(float value)
	{
		furDensity = value;
	}
	public void SetHeightEnable(bool value)
	{
		furHeightEnable = value;
		if (value)
		{
			if (densityToggle != null) densityToggle.SetIsOnWithoutNotify(false);
			if (combingToggle != null) combingToggle.SetIsOnWithoutNotify(false);
			furDensityEnable = false;
			furCombingEnable = false;
		}
	}
	public void SetCombingEnable(bool value)
	{
		furCombingEnable = value;
		if (value)
		{
			if (lengthToggle != null) lengthToggle.SetIsOnWithoutNotify(false);
			if (densityToggle != null) densityToggle.SetIsOnWithoutNotify(false);
			furHeightEnable = false;
			furDensityEnable = false;
		}

	}
	public void SetDensityEnable(bool value)
	{
		furDensityEnable = value;
		if (value)
		{
			if (lengthToggle != null) lengthToggle.SetIsOnWithoutNotify(false);
			if (combingToggle != null) combingToggle.SetIsOnWithoutNotify(false);
			furHeightEnable = false;
			furCombingEnable = false;
		}
	}
	public void SetHeightSetAll(int value)
	{
		furHeightSetAll = value;
	}
	public void SetCombingSetAll(int value)
	{
		furCombingSetAll = value;
	}
	public void SetDensitySetAll(int value)
	{
		furDensitySetAll = value;
	}
	public void SetMirror(bool value)
	{
		furMirror = value;
	}
	public void SetSphere(bool value)
	{
		furSphere = value;
	}
	public void SetShowData(bool value)
	{
		furShowData = value;
	}
	public void SetFilename(string value)
	{
		filename = value;
	}

	public void SetBrushMode(int value)
	{
		brushMode = (BrushMode)value;
	}
	public void SetLengthMode(int value)
	{
		lengthMode = (LengthMode)value;
	}
	public void SetDensityMode(int value)
	{
		densityMode = (DensityMode)value;
	}
	public void SetCombingMode(int value)
	{
		combingMode = (CombingMode)value;
	}



	//////////////////////////////////////////////////////////////////////////////////
	//--------------------------------------------------------------------------------
	// Start is called before the first frame update
	//--------------------------------------------------------------------------------
	//////////////////////////////////////////////////////////////////////////////////

	void Start()
	{
		new WaitForSeconds(.5f);

		// Disable all other scripts, but first check if VR Chat is trying to publish
		if (disableAllOtherScripts)
		{
			MonoBehaviour[] scripts = GameObject.FindObjectsOfType<MonoBehaviour>();
			foreach (MonoBehaviour script in scripts)
			{
				if (script.ToString().Contains("VRCSDK")) return;
			}

			foreach (MonoBehaviour script in scripts)
			{
				if (script.ToString().Contains("PipelineManager")) continue;
				if (script != this) script.enabled = false;
			}
		}


		// Prep the GUI
		if (!initializeGUI()) return;


		// Prep the materials
		if (!initializeGroomingMats()) return;
		int startingIndex = initializeMats();
		if (startingIndex < 0) return;
		if (!loadRenderer(fastFurMats[startingIndex])) return;
		if (!loadMaterial(fastFurMats[startingIndex])) return;
		activeMaterialIndex = materialSelector.value;
		lastMeshUpdate = Time.realtimeSinceStartup;


		// Prep the camera
		if (!initializeCamera()) return;
		addCommandBuffers();

		startedSuccessfully = true;
	}



	//////////////////////////////////////////////////////////////////////////////////
	//--------------------------------------------------------------------------------
	// OnApplicationQuit is called when the user exits
	//--------------------------------------------------------------------------------
	//////////////////////////////////////////////////////////////////////////////////

	void OnApplicationQuit()
	{
		foreach (FastFurMat mat in fastFurMats)
		{
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(mat.filename);
			importer.textureCompression = TextureImporterCompression.Compressed;
			importer.SaveAndReimport();
		}
		AssetDatabase.Refresh();
	}



	//////////////////////////////////////////////////////////////////////////////////
	//--------------------------------------------------------------------------------
	// Update is called every frame
	//--------------------------------------------------------------------------------
	//////////////////////////////////////////////////////////////////////////////////

	void Update()
	{
		try
		{
			Application.targetFrameRate = maxFrameRate;
			moveCamera();
			if (!startedSuccessfully) return;


			// Before we do anything else, run a sanity check. If something has changed that would break the Fur Grooming
			// then we want to stop everything and inform the user, rather than just quietly breaking.
			sanityCheck();


			// Has the material selection changed?
			if (materialSelector.value != activeMaterialIndex)
			{
				activeMaterialIndex = materialSelector.value;

				// Save any filename changes
				foreach (Component c in guiComponents)
				{
					if (c.name.Equals("FileName")) selectedFastFurMat.filename = ((InputField)c.GetComponentInChildren(typeof(InputField))).text;
				}

				// Load the new material
				loadRenderer(fastFurMats[activeMaterialIndex]);
				loadMaterial(fastFurMats[activeMaterialIndex]);
			}


			bool textureChanged = false;

			// Render the fur grooming
			Graphics.ExecuteCommandBuffer(distanceCommandBuffer);
			Graphics.ExecuteCommandBuffer(directionCommandBuffer);
			if (!furSphere)
			{
				Graphics.Blit(directionRenderTexture, fillRenderTexture, fillMaterial);
			}
			else
			{
				Graphics.Blit(directionRenderTexture, fillRenderTextureBuffer, fixEdgesMaterial);
				Graphics.Blit(fillRenderTextureBuffer, fillRenderTexture, fixEdgesMaterial);
			}
			Graphics.ExecuteCommandBuffer(combCommandBuffer);
			Graphics.ExecuteCommandBuffer(groomCommandBuffer);

			Graphics.Blit(selectedFastFurMat.albedoOriginalTexture, albedoRenderTextureTarget, cursorMaterial);

			fixEdges(false);


			// Check to see if the mouse is over the mesh
			mouseHit = Vector4.negativeInfinity;
			Ray rayCast = myCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			bool success = false;
			hits = 0;

			while (Physics.Raycast(rayCast, out hit))
			{
				// We got a hit, but was it the correct material?
				hits++;
				hitIndex = hit.triangleIndex * 3;
				if (hitIndex >= targetTriangleStartIndex && hitIndex <= targetTriangleEndIndex)
				{
					success = true;
					break;
				}
				// Not the right material, so move a tiny bit further ahead and do another raycast in the same direction
				rayCast = new Ray(hit.point + rayCast.direction.normalized * 0.00001f, rayCast.direction);
			}

			if (success)
			{
				mouseHit = hit.point;

				// Check if we have a valid combing sample
				if (mouseHit != oldMouseHitComb && !EventSystem.current.IsPointerOverGameObject())
				{
					for (int i = maxCombTextures - 1; i > 0; i--) Graphics.Blit(combRenderTextureHistory[i - 1], combRenderTextureHistory[i]);
					Graphics.Blit(combRenderTextureFinal, combRenderTextureHistory[0]);
					oldMouseHitComb = mouseHit;
					validSamples++;
				}

				// If the mouse is clicked, save the changes
				if (mouseHit != oldMouseHitWrite && Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
				{
					textureChanged = true;
					Graphics.Blit(furShapeRenderTextureFinal, furShapeRenderTextureBase);
					oldMouseHitWrite = mouseHit;
				}

				// If the middle mouse button is clicked, sample the texture
				if (Input.GetMouseButton(2))
				{
					Texture2D outputTexture = new Texture2D(1, 1, TextureFormat.ARGB32, true, true);
					outputTexture.filterMode = FilterMode.Point;
					RenderTexture.active = furShapeRenderTexturePreEdit;
					outputTexture.ReadPixels(new Rect((int)(hit.textureCoord.x * furShapeRenderTexturePreEdit.width), (int)((1 - hit.textureCoord.y) * furShapeRenderTexturePreEdit.height), 1, 1), 0, 0);
					outputTexture.Apply();
					RenderTexture.active = null;
					Color pixel = outputTexture.GetPixel(0, 0);
					Color densityPixel = densityTexture.GetPixel((int)(hit.textureCoord.x * densityTexture.width), (int)((1 - hit.textureCoord.y) * densityTexture.height));

					furHeight = pixel.b;
					Vector2 combing = new Vector2(pixel.r * 2 - 1, pixel.g * 2 - 1);
					furCombing = Mathf.Min(1, combing.magnitude); // Due to rounding errors, the combing length can sometimes go slightly past 1
					furDensity = Mathf.Round(pixel.a * 64) / 64; // Drop some precision, otherwise compression artifacts will cause visible seams

					if ((int)densityMode == 1)
					{
						//Debug.Log("Density pixel = " + densityPixel.r);
						// Unpack the density so that 0 -> 0.01, 0.5 -> 1, 1 -> 100
						float actualDensity = Mathf.Pow(10, (float)densityPixel.r * 4 - 2) * Mathf.Pow(10, furDensity * 4 - 2);
						//Debug.Log("Actual density = " + actualDensity);
						furDensity = actualDensity / baseDensity;
						// Re-pack the density so that 0.01 -> 0, 1 -> 0.5, 100 -> 1
						furDensity = Mathf.Min(1, Mathf.Max(0, ((Mathf.Log10(furDensity) + 2) * 0.25f)));
					}

					// Set the GUI sliders
					lengthSlider.value = furHeight;
					densitySlider.value = furDensity;
					combingSlider.value = furCombing;
				}
			}
			else
			{
				for (int i = 0; i < 10; i++) Graphics.Blit(combRenderTextureBlank, combRenderTextureHistory[i]);
				oldMouseHitWrite = Vector4.negativeInfinity;
				oldMouseHitComb = Vector4.negativeInfinity;
				validSamples = 0;
			}

			// If the Set All button is clicked, copy the groomed texture 
			if (furHeightSetAll > 0 || furCombingSetAll > 0 || furDensitySetAll > 0)
			{
				textureChanged = true;
				Graphics.Blit(furShapeRenderTextureFinal, furShapeRenderTextureBase);
			}

			// Check if we should make changes permanent
			if (doWrite)
			{
				if ((!Input.GetMouseButton(0) && (furHeightSetAll + furCombingSetAll + furDensitySetAll) == 0) || furHeightSetAll == 1 || furCombingSetAll == 1 || furDensitySetAll == 1)
				{
					writeChanges();
					doWrite = false;
				}
			}
			else
			{
				if ((Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) || furHeightSetAll > 0 || furCombingSetAll > 0 || furDensitySetAll > 0) doWrite = true;
			}


			// Update the shader properties
			distanceMaterial.SetVector("_FurGroomMouseHit", mouseHit);
			distanceMaterial.SetInt("_FurMirror", furMirror ? 1 : 0);
			directionMaterial.SetVector("_FurGroomMouseHit", mouseHit);
			directionMaterial.SetInt("_FurMirror", furMirror ? 1 : 0);
			distanceMaterial.SetFloat("_FurMirrorX", mirrorX);
			directionMaterial.SetFloat("_FurMirrorX", mirrorX);

			fillMaterial.SetFloat("_FurGroomBrushRadius", brushRadius);

			cursorMaterial.SetFloat("_FurGroomBrushRadius", brushRadius);
			cursorMaterial.SetFloat("_FurGroomBrushFalloff", brushFalloff);
			cursorMaterial.SetFloat("_FurGroomBrushVisibility", brushVisibility);

			groomingMaterial.SetFloat("_FurGroomBrushRadius", brushRadius);
			groomingMaterial.SetFloat("_FurGroomBrushFalloff", brushFalloff);
			groomingMaterial.SetFloat("_FurGroomBrushStrength", brushStrength);

			groomingMaterial.SetInt("_FurGroomFurHeightEnabled", furHeightEnable ? 1 : 0);
			groomingMaterial.SetInt("_FurGroomFurCombingEnabled", furCombingEnable && validSamples > maxCombTextures ? 1 : 0);
			groomingMaterial.SetInt("_FurGroomFurDensityEnabled", furDensityEnable ? 1 : 0);

			groomingMaterial.SetFloat("_FurGroomFurHeight", furHeight);
			groomingMaterial.SetFloat("_FurGroomFurCombing", furCombing);
			groomingMaterial.SetFloat("_FurGroomFurDensity", furDensity);

			groomingMaterial.SetInt("_FurGroomFurHeightSetAll", furHeightSetAll);
			groomingMaterial.SetInt("_FurGroomFurCombingSetAll", furCombingSetAll);
			groomingMaterial.SetInt("_FurGroomFurDensitySetAll", furDensitySetAll);

			groomingMaterial.SetInt("_FurBrushMode", (int)brushMode);
			groomingMaterial.SetInt("_FurLengthMode", (int)lengthMode);
			groomingMaterial.SetInt("_FurDensityMode", (int)densityMode);
			groomingMaterial.SetInt("_FurCombingMode", (int)combingMode);

			groomingMaterial.SetTexture("_FurCombPosition2", combRenderTextureHistory[maxCombTextures - 1]);

			groomingMaterial.SetTexture("_FurGroomingMask", selectedMaterial.GetTexture("_FurGroomingMask"));

			selectedUV = selectedMaterial.GetInt("_SelectedUV");

			distanceMaterial.SetInt("_SelectedUV", selectedUV);
			directionMaterial.SetInt("_SelectedUV", selectedUV);
			combMaterial.SetInt("_SelectedUV", selectedUV);
			groomingMaterial.SetInt("_SelectedUV", selectedUV);

			distanceMaterial.SetMatrix("_FurMeshMatrix", myMeshCollider.transform.localToWorldMatrix);
			directionMaterial.SetMatrix("_FurMeshMatrix", myMeshCollider.transform.localToWorldMatrix);

			combMaterial.SetMatrix("_FurMeshMatrix", myMeshCollider.transform.localToWorldMatrix);
			groomingMaterial.SetMatrix("_FurMeshMatrix", myMeshCollider.transform.localToWorldMatrix);

			// Update the debugging options
			selectedMaterial.SetInt("_FurDebugLength", furShowData && furHeightEnable ? 1 : 0);
			selectedMaterial.SetInt("_FurDebugDensity", furShowData && furDensityEnable ? 1 : 0);
			selectedMaterial.SetInt("_FurDebugCombing", furShowData && furCombingEnable ? 1 : 0);

			if (furShowData) selectedMaterial.EnableKeyword("FASTFUR_DEBUGGING");

			if (furHeightSetAll > 0) furHeightSetAll--;
			if (furCombingSetAll > 0) furCombingSetAll--;
			if (furDensitySetAll > 0) furDensitySetAll--;

			// Periodically update the mesh collider and the command buffers
			if (lastMeshUpdate < (Time.realtimeSinceStartup - 3))
			{
				// First check if we need to exit
				MonoBehaviour[] scripts = GameObject.FindObjectsOfType<MonoBehaviour>();
				foreach (MonoBehaviour script in scripts)
				{
					//Debug.Log(script.ToString());
					if (script.ToString().Contains("RuntimeBlueprintCreation"))
					{
						// ABORT!!! ABORT!!! VR Chat is trying to publish the avatar, so we need to shut ourselves down.
						guiComponents = gameObject.GetComponentsInChildren(typeof(Component), true);
						foreach (Component c in guiComponents)
						{
							if (c.name.Equals("FurGroom GUI")) c.gameObject.SetActive(false);
							this.gameObject.SetActive(false);
							return;
						}
					}
				}

				// Re-loading the renderer updates the mesh collider and the command buffers
				loadRenderer(selectedFastFurMat);
			}

			// Update the sliders
			sizeHandleText.text = "" + string.Format("{0:0.0}", Mathf.Round(brushRadius * 10) * 0.1);
			strengthHandleText.text = "" + Mathf.Round(brushStrength * 100);
			falloffHandleText.text = "" + Mathf.Round(brushFalloff * 100);
			visibilityHandleText.text = "" + Mathf.Round(brushVisibility * 100);

			lengthHandleText.text = "" + Mathf.Round(furHeight * 255);
			densityHandleText.text = "" + Mathf.Round(furDensity * 255);
			combingHandleText.text = "" + Mathf.Round(furCombing * 255);

			// Update the slider colours
			if (furHeightEnable && (int)brushMode != 3) { lengthSlider.image.color = Color.white; }
			else lengthSlider.image.color = Color.grey;
			if (furDensityEnable && (int)brushMode != 3) { densitySlider.image.color = Color.white; }
			else densitySlider.image.color = Color.grey;
			if (furCombingEnable && (int)brushMode != 3 && (int)combingMode != 2) { combingSlider.image.color = Color.white; }
			else combingSlider.image.color = Color.grey;

			// Update the Save button
			if (fileNotSaved == 0 && !textureChanged)
			{
				saveButton.image.color = Color.white;
				materialSelector.image.color = Color.white;
				materialSelector.enabled = true;
			}
			else
			{
				float pulseSpeed = 1f + ((float)currentUndo / ((float)maxUndoSize * 0.5f));
				float pulseIntensity = 0.1f * pulseSpeed;
				fileNotSaved += pulseSpeed;

				float pulseRed = pulseIntensity - Mathf.Sin((float)fileNotSaved * 0.02f) * pulseIntensity;
				float pulseBlue = (2 * pulseIntensity) + Mathf.Sin((float)fileNotSaved * 0.02f) * (2 * pulseIntensity);
				float fadeIn = Mathf.Min(1f, ((float)fileNotSaved * 0.1f));
				pulseRed = 1f - (pulseRed * fadeIn);
				pulseBlue = 1f - (pulseBlue * fadeIn);
				saveButton.image.color = new Color(pulseRed, 1f - (0.75f * fadeIn), pulseBlue, 1f);
				materialSelector.image.color = Color.grey;
				materialSelector.enabled = false;
			}
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Failed during Update(): " + e);
			return;
		}
	}






	//////////////////////////////////////////////////////////////////////////////////
	//--------------------------------------------------------------------------------
	// Various functions
	//--------------------------------------------------------------------------------
	//////////////////////////////////////////////////////////////////////////////////



	//--------------------------------------------------------------------------------
	// Initialize GUI

	private bool initializeGUI()
	{
		try
		{
			// Create the GUI
			guiComponents = gameObject.GetComponentsInChildren(typeof(Component), true);
			foreach (Component c in guiComponents)
			{
				if (c is MonoBehaviour) ((MonoBehaviour)c).enabled = true;
				if (!c.name.Equals("FurGroom Error Message") && !c.name.Equals("FurGroom Warning Message") && !c.name.Equals("Template")) c.gameObject.SetActive(true);

				if (c.name.Equals("FurGroom Warning Message")) warningImage = (Image)c.GetComponentInChildren(typeof(Image));
				if (c.name.Equals("FurGroom Warning Message Text")) warningText = (Text)c.GetComponentInChildren(typeof(Text));
				if (c.name.Equals("FurGroomHelpText")) helpText = (Text)c.GetComponentInChildren(typeof(Text));
				if (c.name.Equals("LengthSlider")) lengthSlider = (Slider)c.GetComponentInChildren(typeof(Slider));
				if (c.name.Equals("DensitySlider")) densitySlider = (Slider)c.GetComponentInChildren(typeof(Slider));
				if (c.name.Equals("CombingSlider")) combingSlider = (Slider)c.GetComponentInChildren(typeof(Slider));
				if (c.name.Equals("SizeHandleText")) sizeHandleText = (Text)c.GetComponentInChildren(typeof(Text));
				if (c.name.Equals("StrengthHandleText")) strengthHandleText = (Text)c.GetComponentInChildren(typeof(Text));
				if (c.name.Equals("FalloffHandleText")) falloffHandleText = (Text)c.GetComponentInChildren(typeof(Text));
				if (c.name.Equals("VisibilityHandleText")) visibilityHandleText = (Text)c.GetComponentInChildren(typeof(Text));
				if (c.name.Equals("LengthHandleText")) lengthHandleText = (Text)c.GetComponentInChildren(typeof(Text));
				if (c.name.Equals("DensityHandleText")) densityHandleText = (Text)c.GetComponentInChildren(typeof(Text));
				if (c.name.Equals("CombingHandleText")) combingHandleText = (Text)c.GetComponentInChildren(typeof(Text));
				if (c.name.Equals("ToggleLength")) lengthToggle = (Toggle)c.GetComponentInChildren(typeof(Toggle));
				if (c.name.Equals("ToggleDensity")) densityToggle = (Toggle)c.GetComponentInChildren(typeof(Toggle));
				if (c.name.Equals("ToggleCombing")) combingToggle = (Toggle)c.GetComponentInChildren(typeof(Toggle));
				if (c.name.Equals("Undo")) undoButton = (Button)c.GetComponentInChildren(typeof(Button));
				if (c.name.Equals("Redo")) redoButton = (Button)c.GetComponentInChildren(typeof(Button));
				if (c.name.Equals("Save")) saveButton = (Button)c.GetComponentInChildren(typeof(Button));
				if (c.name.Equals("MaterialSelection")) materialSelector = (Dropdown)c.GetComponentInChildren(typeof(Dropdown));
				if (c.name.Equals("HelpPopupMove")) helpPopupMove = c;
				if (c.name.Equals("HelpPopupSpeed")) helpPopupSpeed = c;
			}

			if (helpText == null || lengthSlider == null || densitySlider == null || combingSlider == null || sizeHandleText == null ||
				strengthHandleText == null || falloffHandleText == null || visibilityHandleText == null || lengthHandleText == null ||
				densityHandleText == null || combingHandleText == null || lengthToggle == null || densityToggle == null ||
				combingToggle == null || undoButton == null || redoButton == null || saveButton == null || materialSelector == null ||
				warningImage == null || warningText == null)
			{
				errorMessage("The Fur Grooming GUI is missing some elements. Please upgrade the Fur Grooming prefab to the newest version.");
				return false;
			}
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Unable to initialize GUI: " + e);
			return false;
		}

		return true;
	}



	//--------------------------------------------------------------------------------
	// Initialize camera
	private bool initializeCamera()
	{
		try
		{
			Camera[] cameras = Camera.allCameras;
			foreach (Camera camera in cameras) camera.enabled = false;

			cameraGameObject = new GameObject("Fur Grooming Camera");
			myCamera = cameraGameObject.AddComponent<Camera>();
			myCamera.nearClipPlane = 0.01f;
			myCamera.enabled = true;

			// Position the camera to be facing the first renderer
			myCamera.transform.SetPositionAndRotation(selectedRenderer.transform.position, Quaternion.identity);
			if (initialTarget != null)
			{
				myCamera.transform.SetPositionAndRotation(initialTarget.transform.position, Quaternion.identity);
			}
			myCamera.transform.Translate(0, 1, 1);
			myCamera.transform.Rotate(0, 180, 0);
			myCamera.nearClipPlane = 0.01f;
			yaw = myCamera.transform.eulerAngles.y;
			pitch = myCamera.transform.eulerAngles.x;
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Unable to initialize camera: " + e);
			return false;
		}

		return true;
	}



	//--------------------------------------------------------------------------------
	// Initialize grooming materials
	private bool initializeGroomingMats()
	{
		try
		{
			// Create the materials used for grooming
			fillMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Fill"));
			distanceMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Distance"));
			directionMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Direction"));
			combMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Comb"));
			stencilMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Stencil"));
			cursorMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Cursor"));
			groomingMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Groomer"));
			fixEdgesMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Fix Edges"));

			if (fillMaterial == null || distanceMaterial == null || directionMaterial == null || combMaterial == null
				|| stencilMaterial == null || cursorMaterial == null || groomingMaterial == null || fixEdgesMaterial == null)
			{
				errorMessage("The Fur Grooming GUI can't find required shaders. Perhaps the shader package needs to be re-installed?");
				return false;
			}

		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Unable to initialize grooming materials: " + e);
			return false;
		}

		return true;
	}



	//--------------------------------------------------------------------------------
	// Initialize materials
	private int initializeMats()
	{
		try
		{
			// Generate a list of all of the Fast Fur materials in the project, along with their parent renderers and their filenames
			List<FastFurMat> fastFurMatsList = new List<FastFurMat>();
			List<Dropdown.OptionData> materialNames = new List<Dropdown.OptionData>();

			int index = 0;
			int startingIndex = -1;
			Transform[] allTransforms = Object.FindObjectsOfType<Transform>();
			foreach (Transform transform in allTransforms)
			{
				Renderer myRenderer = transform.gameObject.GetComponent<Renderer>() as Renderer;
				if (myRenderer == null) continue;
				if (!myRenderer.enabled) continue;
				Material[] materials = myRenderer.materials;
				for (int x = 0; x < materials.Length; x++)
				{
					if (materials[x].shader.name.StartsWith("Warren's Fast Fur/Fast Fur") || materials[x].shader.name.StartsWith("Warren's Fast Fur/Older Versions/Fast Fur"))
					{
						FastFurMat newFastFurMat = new FastFurMat();
						newFastFurMat.renderer = myRenderer;
						newFastFurMat.material = materials[x];
						newFastFurMat.materialIndex = x;

						try
						{
							newFastFurMat.filename = AssetDatabase.GetAssetPath(materials[x].GetTexture("_FurShapeMap").GetInstanceID());
							newFastFurMat.furShapeOriginalTexture = AssetDatabase.LoadAssetAtPath<Texture>(newFastFurMat.filename);
							TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(newFastFurMat.filename);
							importer.textureCompression = TextureImporterCompression.Uncompressed;
							importer.SaveAndReimport();
						}
						catch (System.Exception e)
						{
							errorMessage("The '" + newFastFurMat.material.name + "' material does not have a Fur Shape Data Map texture. You must create the texture first before you can edit it.");
							return -1;
						}

						try
						{
							newFastFurMat.albedoOriginalTexture = materials[x].GetTexture("_MainTex");
						}
						catch (System.Exception e)
						{
						}

						fastFurMatsList.Add(newFastFurMat);

						Dropdown.OptionData dropDownItem = new Dropdown.OptionData();
						dropDownItem.text = myRenderer.gameObject.name + " - ";
						dropDownItem.text += materials[x].name.Replace(" (Instance)", "");
						materialNames.Add(dropDownItem);
						if (initialTarget != null)
						{
							if (myRenderer.Equals(initialTarget) && startingIndex < 0)
							{
								startingIndex = index;
							}
						}
						index++;
					}
				}
			}
			if (fastFurMatsList.Count == 0)
			{
				errorMessage("Unable to locate any Fast Fur Materials. ");
				return (-1);
			}

			if (startingIndex < 0) startingIndex = 0;

			fastFurMats = fastFurMatsList.ToArray();

			materialSelector.options = materialNames;
			materialSelector.value = startingIndex;

			return (startingIndex);
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Unable to initialize materials: " + e);
		}

		return -1;
	}



	//--------------------------------------------------------------------------------
	// Load renderer
	private bool loadRenderer(FastFurMat myFastFurMat)
	{
		Renderer renderer = myFastFurMat.renderer;

		try
		{
			if (renderer is SkinnedMeshRenderer || renderer is MeshRenderer)
			{
				// Create a mesh collider, attach it to a new game object, then bake it from the target mesh
				if (colliderGameObject == null)
				{
					colliderGameObject = new GameObject("Fur Grooming Mesh Collider");
					myMeshCollider = colliderGameObject.AddComponent<MeshCollider>();
				}

				if (renderer is SkinnedMeshRenderer)
				{
					bakedMesh = new Mesh();
					((SkinnedMeshRenderer)renderer).BakeMesh(bakedMesh);
					myMeshCollider.sharedMesh = bakedMesh;

				}

				else if (renderer is MeshRenderer)
				{
					MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
					myMeshCollider.sharedMesh = meshFilter.sharedMesh;
				}

				colliderGameObject.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
				mirrorX = renderer.transform.position.x;

				lastMeshUpdate = Time.realtimeSinceStartup;

				addCommandBuffers();
			}

			selectedRenderer = renderer;


		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("The Target Renderer failed to load: " + e.ToString());
			return false;
		}

		return true;
	}



	//--------------------------------------------------------------------------------
	// Load material
	private bool loadMaterial(FastFurMat myFastFurMat)
	{
		try
		{
			string warningText = "";
			for (int x = 0; x < fastFurMats.Length; x++)
			{
				if (fastFurMats[x] != myFastFurMat)
				{
					// Using a material in multiple places seems to be a common problem that people are having trouble figuring out.
					// I've added a bunch of code to hopefully make it easy for people to determine where the multiple copies are.
					if (fastFurMats[x].filename.Equals(myFastFurMat.filename) && showDuplicateMaterialWarning)
					{
						warningText = "The target 'Fur Shape Data Map' texture is being used in multiple places. It is being used on the '";
						warningText += myFastFurMat.renderer.name + "' mesh in material " + myFastFurMat.materialIndex + ": '" + myFastFurMat.material.name.Replace(" (Instance)", "") + "', and also on the '";
						warningText += fastFurMats[x].renderer.name + "' mesh in material " + x + ": '" + fastFurMats[x].material.name.Replace(" (Instance)", "") + "'. ";
						warningText += "When you save, YOU WILL BE SAVING TO BOTH LOCATIONS! Edits might overwrite each other, and may also cause edge corruption if the overpainting is too large. To prevent this, you should make a separate copy of the material with its own separate 'Fur Shape Data Map' texture.";
						warningMessage(warningText);
					}
				}
			}
			warningMessage(warningText);

			if (myFastFurMat.renderer is SkinnedMeshRenderer)
			{
				targetTriangleStartIndex = ((SkinnedMeshRenderer)myFastFurMat.renderer).sharedMesh.GetSubMesh(myFastFurMat.materialIndex).indexStart;
				targetTriangleEndIndex = targetTriangleStartIndex + ((SkinnedMeshRenderer)myFastFurMat.renderer).sharedMesh.GetSubMesh(myFastFurMat.materialIndex).indexCount;
				targetVertexStartIndex = ((SkinnedMeshRenderer)myFastFurMat.renderer).sharedMesh.GetSubMesh(myFastFurMat.materialIndex).baseVertex;
				targetVertexEndIndex = targetVertexStartIndex + ((SkinnedMeshRenderer)myFastFurMat.renderer).sharedMesh.GetSubMesh(myFastFurMat.materialIndex).vertexCount;
			}
			else if (myFastFurMat.renderer is MeshRenderer)
			{
				MeshFilter meshFilter = myFastFurMat.renderer.GetComponent<MeshFilter>();
				targetTriangleStartIndex = meshFilter.sharedMesh.GetSubMesh(myFastFurMat.materialIndex).indexStart;
				targetTriangleEndIndex = targetTriangleStartIndex + meshFilter.sharedMesh.GetSubMesh(myFastFurMat.materialIndex).indexCount;
				targetVertexStartIndex = meshFilter.sharedMesh.GetSubMesh(myFastFurMat.materialIndex).baseVertex;
				targetVertexEndIndex = targetVertexStartIndex + meshFilter.sharedMesh.GetSubMesh(myFastFurMat.materialIndex).vertexCount;
			}
			else
			{
				errorMessage("The Fur Grooming currently only supports 'Mesh Renderers' and 'Skinned Mesh Renderers'. Since you appear to be editing something else, let Warren know on the Discord server and he'll try to add support for what you are trying to edit.");
				return false;
			}


			Material material = myFastFurMat.material;
			if (material.GetTexture("_FurShapeMap") == null)
			{
				errorMessage("The Target Material does not have a Fur Shape Data Map texture");
				return false;
			}
			if (material.GetTexture("_HairMap") == null)
			{
				errorMessage("The Target Material does not have a Hair Pattern Map texture. You must create the texture first before you can edit the material.");
				return false;
			}


			// Restore default settings to the de-selected material
			if (selectedFastFurMat != null)
			{
				selectedFastFurMat.material.SetTexture("_MainTex", selectedFastFurMat.albedoOriginalTexture);
				selectedFastFurMat.material.SetTexture("_FurShapeMap", selectedFastFurMat.furShapeOriginalTexture);
			}

			selectedFastFurMat = myFastFurMat;
			selectedMaterial = material;
			subMeshIndex = myFastFurMat.materialIndex;
			material.SetFloat("_CameraProximityTouch", 0);
			material.EnableKeyword("FASTFUR_DEBUGGING");
			// Re-load the 'furShapeOriginalTexture', since it might have changed if the material is being shared
			myFastFurMat.furShapeOriginalTexture = AssetDatabase.LoadAssetAtPath<Texture>(myFastFurMat.filename);


			// Set the filename
			foreach (Component c in guiComponents)
			{
				if (c.name.Equals("FileName")) ((InputField)c.GetComponentInChildren(typeof(InputField))).text = myFastFurMat.filename;
			}

			// Create the albedo textures that will be used to display the cursor
			Texture mainTex = material.GetTexture("_MainTex");
			if (mainTex == null)
			{
				albedoFallbackTexture = new Texture2D(1024, 1024, TextureFormat.ARGB32, false, true);
				var newPixels = ((Texture2D)albedoFallbackTexture).GetPixels();
				var newColour = new Color(1f, 1f, 1f, 1f);
				for (int x = 0; x < newPixels.Length; x++) newPixels[x] = newColour;

				((Texture2D)albedoFallbackTexture).SetPixels(newPixels);
				((Texture2D)albedoFallbackTexture).Apply();
				mainTex = albedoFallbackTexture;
			}
			albedoRenderTextureTarget = new RenderTexture(mainTex.width, mainTex.height, 0, RenderTextureFormat.ARGB32);
			albedoRenderTextureTarget.filterMode = FilterMode.Point;
			albedoRenderTextureBuffer = new RenderTexture(mainTex.width, mainTex.height, 0, RenderTextureFormat.ARGB32);
			albedoRenderTextureBuffer.filterMode = FilterMode.Point;
			albedoRenderTextureFinal = new RenderTexture(mainTex.width, mainTex.height, 0, RenderTextureFormat.ARGB32);
			material.SetTexture("_MainTex", albedoRenderTextureFinal);

			// We want to edit the un-compressed version of the fur texture


			Texture furTex = material.GetTexture("_FurShapeMap");
			if (furTex == null)
			{
				errorMessage("The Fur Shape Data Map is missing on the target material.");
				return false;
			}
			int targetWidth = furTex.width;
			int targetHeight = furTex.height;
			furShapeRenderTexturePreEdit = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			furShapeRenderTexturePreEdit.filterMode = FilterMode.Point;
			furShapeRenderTextureBase = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			furShapeRenderTextureBase.filterMode = FilterMode.Point;
			furShapeRenderTextureTarget = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			furShapeRenderTextureTarget.filterMode = FilterMode.Point;
			furShapeRenderTextureBuffer = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			furShapeRenderTextureBuffer.filterMode = FilterMode.Point;
			furShapeRenderTextureFinal = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			furShapeRenderTextureUndo = new RenderTexture[maxUndoSize + 1];
			for (int i = 0; i <= maxUndoSize; i++) furShapeRenderTextureUndo[i] = new RenderTexture(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			currentUndo = 0;
			highestUndo = 0;
			undoOverflow = false;
			doWrite = false;
			validSamples = 0;
			material.SetTexture("_FurShapeMap", furShapeRenderTextureFinal);
			Graphics.Blit(furTex, furShapeRenderTextureBase);
			Graphics.Blit(furTex, furShapeRenderTexturePreEdit);
			Graphics.Blit(furTex, furShapeRenderTextureUndo[0]);
			groomingMaterial.SetTexture("_FurShapeMap", furShapeRenderTextureBase);
			groomingMaterial.SetTexture("_FurShapeMapPreEdit", furShapeRenderTexturePreEdit);
			groomingMaterial.SetTexture("_FurGroomingMask", material.GetTexture("_FurGroomingMask"));
			groomingMaterial.SetFloat("_FurMinHeight", material.GetFloat("_FurMinHeight"));
			stencilMaterial.SetTexture("_FurShapeMap", myFastFurMat.furShapeOriginalTexture);

			// Create the distance and direction textures. Note that these are high-precision textures.
			distanceRenderTexture = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
			distanceRenderTexture.filterMode = FilterMode.Point;
			directionRenderTexture = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			directionRenderTexture.filterMode = FilterMode.Point;
			fillRenderTexture = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			fillRenderTexture.filterMode = FilterMode.Point;
			fillRenderTextureBuffer = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			fillRenderTextureBuffer.filterMode = FilterMode.Point;
			directionMaterial.SetTexture("_MainTex", distanceRenderTexture);
			cursorMaterial.SetTexture("_DirectionTex", fillRenderTexture);
			combMaterial.SetTexture("_DirectionTex", fillRenderTexture);
			groomingMaterial.SetTexture("_DirectionTex", fillRenderTexture);

			// Create the comb textures. Note that these are high-precision textures.
			combRenderTextureTarget = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			combRenderTextureTarget.filterMode = FilterMode.Point;
			combRenderTextureBuffer = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			combRenderTextureBuffer.filterMode = FilterMode.Point;
			combRenderTextureFinal = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			combRenderTextureFinal.filterMode = FilterMode.Point;
			combRenderTextureBlank = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			combRenderTextureBlank.filterMode = FilterMode.Point;
			combRenderTextureHistory = new RenderTexture[maxCombTextures];
			for (int i = 0; i < maxCombTextures; i++)
			{
				combRenderTextureHistory[i] = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
				combRenderTextureHistory[i].filterMode = FilterMode.Point;
			}

			// Determine the average density of the mesh and use that as a baseline for density painting
			Material densityCheckMaterial = new Material(Shader.Find("Warren's Fast Fur/Internal Utilities/Density Check"));
			RenderTexture densityCheckRenderTextureTarget = new RenderTexture(furTex.width, furTex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			densityCheckMaterial.SetMatrix("_FurMeshMatrix", myMeshCollider.transform.localToWorldMatrix);
			CommandBuffer densityCommandBuffer = new CommandBuffer();
			densityCommandBuffer.name = "FastFurDensityCheck";
			densityCommandBuffer.SetRenderTarget(densityCheckRenderTextureTarget);
			densityCommandBuffer.DrawMesh(myMeshCollider.sharedMesh, Matrix4x4.identity, densityCheckMaterial);
			Graphics.ExecuteCommandBuffer(densityCommandBuffer);
			densityTexture = new Texture2D(furTex.width, furTex.height, TextureFormat.ARGB32, false, true);
			densityTexture.filterMode = FilterMode.Point;
			RenderTexture.active = densityCheckRenderTextureTarget;
			densityTexture.ReadPixels(new Rect(0, 0, furTex.width, furTex.height), 0, 0);
			densityTexture.Apply();
			RenderTexture.active = null;
			Color[] pixels = densityTexture.GetPixels();
			float[] pixelCount = new float[256];
			float totalPixels = furTex.width * furTex.height;
			for (int x = 0; x < 256; x++) pixelCount[x] = 0;
			for (int x = 0; x < totalPixels; x++)
			{
				if (pixels[x].a > 0) pixelCount[(int)(pixels[x].r * 255)]++;
			}
			float activePixels = 0;
			float total = 0;
			for (int x = 0; x < 256; x++)
			{
				//Debug.Log("Density " + x + " = " + pixelCount[x]);
				activePixels += pixelCount[x];
				total += pixelCount[x] * x;
			}
			float finalResult = total / activePixels;
			// Unpack the density so that 0 -> 0.001, 0.5 -> 1, 1 -> 1000
			baseDensity = Mathf.Pow(10, ((float)finalResult / 255) * 6 - 3);
			groomingMaterial.SetFloat("_FurBaseDensity", baseDensity);

			groomingMaterial.SetTexture("_FurCombPosition1", combRenderTextureHistory[0]);
			groomingMaterial.SetTexture("_FurCombPosition2", combRenderTextureHistory[maxCombTextures - 1]);

			lastMeshUpdate = Time.realtimeSinceStartup - 5;
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("The Target Material failed to load: " + e.ToString());
			return false;
		}

		return true;
	}



	//--------------------------------------------------------------------------------
	// Add command buffers
	private void addCommandBuffers()
	{
		try
		{
			distanceCommandBuffer = new CommandBuffer();
			distanceCommandBuffer.name = "Fast Fur Distance";
			distanceCommandBuffer.SetRenderTarget(distanceRenderTexture);
			distanceCommandBuffer.DrawMesh(myMeshCollider.sharedMesh, Matrix4x4.identity, distanceMaterial, subMeshIndex);

			directionCommandBuffer = new CommandBuffer();
			directionCommandBuffer.name = "Fast Fur Direction";
			directionCommandBuffer.SetRenderTarget(directionRenderTexture);
			directionCommandBuffer.DrawMesh(myMeshCollider.sharedMesh, Matrix4x4.identity, directionMaterial, subMeshIndex);

			combCommandBuffer = new CommandBuffer();
			combCommandBuffer.name = "Fast Fur Comb";
			combCommandBuffer.SetRenderTarget(combRenderTextureTarget);
			combCommandBuffer.DrawMesh(myMeshCollider.sharedMesh, Matrix4x4.identity, combMaterial, subMeshIndex);

			groomCommandBuffer = new CommandBuffer();
			groomCommandBuffer.name = "Fast Fur Grooming";
			groomCommandBuffer.SetRenderTarget(furShapeRenderTextureTarget);
			groomCommandBuffer.DrawMesh(myMeshCollider.sharedMesh, Matrix4x4.identity, groomingMaterial, subMeshIndex);
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Can't add command buffers: " + e.ToString());
			return;
		}
	}



	//--------------------------------------------------------------------------------
	// Fix edges by adding overpainting. If we are finished grooming, also apply the
	// stencil to the grooming map, so that we preserve other parts of the map that we
	// were not editing.
	private void fixEdges(bool finalFix)
	{
		try
		{
			if (overPainting == 0)
			{
				Graphics.Blit(albedoRenderTextureTarget, albedoRenderTextureFinal);
				Graphics.Blit(combRenderTextureTarget, combRenderTextureFinal);
				Graphics.Blit(furShapeRenderTextureTarget, furShapeRenderTextureFinal);
			}
			else if (overPainting == 1)
			{
				Graphics.Blit(albedoRenderTextureTarget, albedoRenderTextureFinal, fixEdgesMaterial);
				Graphics.Blit(combRenderTextureTarget, combRenderTextureFinal, fixEdgesMaterial);
				Graphics.Blit(furShapeRenderTextureTarget, furShapeRenderTextureFinal, fixEdgesMaterial);
			}
			else
			{
				Graphics.Blit(albedoRenderTextureTarget, albedoRenderTextureBuffer, fixEdgesMaterial);
				Graphics.Blit(combRenderTextureTarget, combRenderTextureBuffer, fixEdgesMaterial);
				Graphics.Blit(furShapeRenderTextureTarget, furShapeRenderTextureBuffer, fixEdgesMaterial);
			}

			int x = 1;
			while (x < overPainting)
			{
				if (x + 1 == overPainting)
				{
					Graphics.Blit(albedoRenderTextureBuffer, albedoRenderTextureFinal, fixEdgesMaterial);
					Graphics.Blit(combRenderTextureBuffer, combRenderTextureFinal, fixEdgesMaterial);
					Graphics.Blit(furShapeRenderTextureBuffer, furShapeRenderTextureFinal, fixEdgesMaterial);
					x++;
				}
				else if (x + 2 == overPainting)
				{
					Graphics.Blit(albedoRenderTextureBuffer, albedoRenderTextureFinal, fixEdgesMaterial);
					Graphics.Blit(combRenderTextureBuffer, combRenderTextureFinal, fixEdgesMaterial);
					Graphics.Blit(furShapeRenderTextureBuffer, furShapeRenderTextureFinal, fixEdgesMaterial);

					Graphics.Blit(albedoRenderTextureFinal, albedoRenderTextureBuffer, fixEdgesMaterial);
					Graphics.Blit(combRenderTextureFinal, combRenderTextureBuffer, fixEdgesMaterial);
					Graphics.Blit(furShapeRenderTextureFinal, furShapeRenderTextureBuffer, fixEdgesMaterial);

					Graphics.Blit(albedoRenderTextureBuffer, albedoRenderTextureFinal);
					Graphics.Blit(combRenderTextureBuffer, combRenderTextureFinal);
					Graphics.Blit(furShapeRenderTextureBuffer, furShapeRenderTextureFinal);
					x += 2;
				}
				else
				{
					Graphics.Blit(albedoRenderTextureBuffer, albedoRenderTextureFinal, fixEdgesMaterial);
					Graphics.Blit(combRenderTextureBuffer, combRenderTextureFinal, fixEdgesMaterial);
					Graphics.Blit(furShapeRenderTextureBuffer, furShapeRenderTextureFinal, fixEdgesMaterial);

					Graphics.Blit(albedoRenderTextureFinal, albedoRenderTextureBuffer, fixEdgesMaterial);
					Graphics.Blit(combRenderTextureFinal, combRenderTextureBuffer, fixEdgesMaterial);
					Graphics.Blit(furShapeRenderTextureFinal, furShapeRenderTextureBuffer, fixEdgesMaterial);
					x += 2;
				}
			}

			Graphics.Blit(furShapeRenderTextureFinal, furShapeRenderTextureBuffer);
			Graphics.Blit(furShapeRenderTextureBuffer, furShapeRenderTextureFinal, stencilMaterial);
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Failed to Fix Edges: " + e.ToString());
			return;
		}
	}



	//--------------------------------------------------------------------------------
	// Write current changes to the texture
	private void writeChanges()
	{
		try
		{
			if (currentUndo == maxUndoSize)
			{
				undoOverflow = true;
				for (int x = 0; x < maxUndoSize; x++)
				{
					Graphics.Blit(furShapeRenderTextureUndo[x + 1], furShapeRenderTextureUndo[x]);
				}
			}
			else currentUndo++;

			fileNotSaved++;
			highestUndo = currentUndo;
			undoButton.interactable = true;
			redoButton.interactable = false;

			Graphics.Blit(furShapeRenderTextureFinal, furShapeRenderTexturePreEdit);
			Graphics.Blit(furShapeRenderTexturePreEdit, furShapeRenderTextureUndo[currentUndo]);
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Failed to write changes: " + e.ToString());
			return;
		}
	}



	//--------------------------------------------------------------------------------
	// Undo changes
	public void doUndo()
	{
		try
		{
			if (currentUndo == 0) return;

			currentUndo--;
			if (currentUndo == 0 && !undoOverflow) fileNotSaved = 0;
			undoButton.interactable = (currentUndo != 0);
			redoButton.interactable = true;

			Graphics.Blit(furShapeRenderTextureUndo[currentUndo], furShapeRenderTexturePreEdit);
			Graphics.Blit(furShapeRenderTextureUndo[currentUndo], furShapeRenderTextureBase);
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Failed to undo: " + e.ToString());
			return;
		}
	}



	//--------------------------------------------------------------------------------
	// Redo changes
	public void doRedo()
	{
		try
		{
			if (currentUndo == highestUndo) return;

			currentUndo++;
			fileNotSaved++;
			undoButton.interactable = true;
			redoButton.interactable = (currentUndo != highestUndo);

			Graphics.Blit(furShapeRenderTextureUndo[currentUndo], furShapeRenderTexturePreEdit);
			Graphics.Blit(furShapeRenderTextureUndo[currentUndo], furShapeRenderTextureBase);
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Failed to redo: " + e.ToString());
			return;
		}
	}



	//--------------------------------------------------------------------------------
	// Save changes
	public void doSave()
	{
		try
		{
			if (filename.Length < 1) return;
			fixEdges(true);
			Texture2D outputTexture = new Texture2D(furShapeRenderTexturePreEdit.width, furShapeRenderTexturePreEdit.height, TextureFormat.ARGB32, true, true);
			outputTexture.filterMode = FilterMode.Point;
			RenderTexture.active = furShapeRenderTexturePreEdit;
			outputTexture.ReadPixels(new Rect(0, 0, outputTexture.width, outputTexture.height), 0, 0);
			outputTexture.Apply();
			RenderTexture.active = null;

			byte[] bytes = outputTexture.EncodeToPNG();
			System.IO.File.WriteAllBytes(filename, bytes);

			// If the filename has changed, then we need to set the texture import setting
			TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(filename);
			importer.sRGBTexture = false;
			importer.crunchedCompression = false;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			importer.streamingMipmaps = true;
			importer.SaveAndReimport();

			// Re-load the material from the newly saved version
			AssetDatabase.Refresh();
			fastFurMats[activeMaterialIndex].furShapeOriginalTexture = AssetDatabase.LoadAssetAtPath<Texture>(fastFurMats[activeMaterialIndex].filename);
			loadMaterial(fastFurMats[activeMaterialIndex]);

			undoOverflow = false;
			currentUndo = 0;
			highestUndo = 0;
			fileNotSaved = 0;

			undoButton.interactable = false;
			redoButton.interactable = false;

			Graphics.Blit(furShapeRenderTexturePreEdit, furShapeRenderTextureUndo[0]);
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Failed to save: " + e.ToString());
			return;
		}
	}



	//--------------------------------------------------------------------------------
	// Move camera
	private void moveCamera()
	{
		try
		{
			float speedChange = Input.GetAxis("Mouse ScrollWheel");
			if (speedChange != 0) helpPopupSpeed.gameObject.SetActive(false);
			if (speedChange > 0) moveSpeed *= (1 + (speedChange * 10));
			if (speedChange < 0) moveSpeed /= (1 - (speedChange * 10));
			if (moveSpeed < 0.015625f) moveSpeed = 0.015625f;
			if (moveSpeed > 128f) moveSpeed = 128f;
			if (speedChange != 0)
			{
				helpText.text = helpText.text.Substring(0, 67) + moveSpeed.ToString().Substring(0, Mathf.Min(5, moveSpeed.ToString().Length)) + ")";
			}
			float shiftPressed = Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift) ? 2 : 1;

			if (Input.GetMouseButton(1))
			{
				yaw += turnSpeed * Input.GetAxis("Mouse X");
				pitch -= turnSpeed * Input.GetAxis("Mouse Y");
				myCamera.transform.eulerAngles = new Vector3(pitch, yaw, 0f);

				if (Input.GetKey(forwardKey))
				{
					myCamera.transform.Translate(0, 0, 0.001f * moveSpeed * shiftPressed);
					helpPopupMove.gameObject.SetActive(false);
				}
				if (Input.GetKey(backKey))
				{
					myCamera.transform.Translate(0, 0, -0.001f * moveSpeed * shiftPressed);
					helpPopupMove.gameObject.SetActive(false);
				}
				if (Input.GetKey(upKey))
				{
					myCamera.transform.Translate(0, 0.001f * moveSpeed * shiftPressed, 0);
					helpPopupMove.gameObject.SetActive(false);
				}
				if (Input.GetKey(downKey))
				{
					myCamera.transform.Translate(0, -0.001f * moveSpeed * shiftPressed, 0);
					helpPopupMove.gameObject.SetActive(false);
				}
				if (Input.GetKey(rightKey))
				{
					myCamera.transform.Translate(0.001f * moveSpeed * shiftPressed, 0, 0);
					helpPopupMove.gameObject.SetActive(false);
				}
				if (Input.GetKey(leftKey))
				{
					myCamera.transform.Translate(-0.001f * moveSpeed * shiftPressed, 0, 0);
					helpPopupMove.gameObject.SetActive(false);
				}
			}
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Failed to move camera: " + e.ToString());
			return;
		}
	}



	//--------------------------------------------------------------------------------
	// Display an error message
	private void errorMessage(string error)
	{
		Component[] guiComponents = gameObject.GetComponentsInChildren(typeof(Component), true);
		foreach (Component c in guiComponents)
		{
			if (c.name.StartsWith("FurGroom Error Message")) c.gameObject.SetActive(true);
			if (c.name.Equals("FurGroom GUI")) c.gameObject.SetActive(false);
			if (c.name.Equals("FurGroom Error Message Text")) ((Text)c.GetComponentInChildren(typeof(Text))).text = "ERROR: " + error;
		}
		startedSuccessfully = false;
	}



	//--------------------------------------------------------------------------------
	// Display a warning message
	private void warningMessage(string warning)
	{
		warningText.text = "WARNING: " + warning;
		warningImage.gameObject.SetActive(warning.Length > 0);
	}



	//--------------------------------------------------------------------------------
	// Sanity checks
	private void sanityCheck()
	{
		try
		{
			if (selectedFastFurMat.renderer != selectedRenderer)
			{
				string error = "Something (possibly a custom script?) has interfered with the Fur Grooming. The selected renderer was modified while the Fur Grooming was running: '";
				error += selectedFastFurMat.renderer.name + "' -> '" + selectedRenderer.name + "'";
				return;
			}

			if (selectedFastFurMat.material != selectedMaterial)
			{
				string error = "Something (possibly a custom script?) has interfered with the Fur Grooming. The selected material was modified while the Fur Grooming was running: '";
				error += selectedFastFurMat.material.name + "' -> '" + selectedMaterial.name + "'";
				errorMessage(error);
				return;
			}

			Material[] checkMats = selectedRenderer.materials;
			bool found = false;
			foreach (Material mat in checkMats)
			{
				if (mat.GetInstanceID() == selectedMaterial.GetInstanceID()) found = true;
			}
			if (!found)
			{
				errorMessage("Something (possibly a custom script?) has interfered with the Fur Grooming. The selected material is no longer attached to the selected renderer.");
				return;
			}

			if (selectedFastFurMat.material.GetTexture("_FurShapeMap").GetInstanceID() != furShapeRenderTextureFinal.GetInstanceID())
			{
				string error = "Something (possibly a custom script?) has interfered with the Fur Grooming. The selected fur shape texture was modified while the Fur Grooming was running: '";
				error += selectedFastFurMat.material.GetTexture("_FurShapeMap").GetInstanceID() + "' -> '" + furShapeRenderTextureFinal.GetInstanceID() + "'";
				errorMessage(error);
				return;
			}

			if (selectedFastFurMat.material.GetTexture("_MainTex").GetInstanceID() != albedoRenderTextureFinal.GetInstanceID())
			{
				string error = "Something (possibly a custom script?) has interfered with the Fur Grooming. The selected albedo texture was modified while the Fur Grooming was running: '";
				error += selectedFastFurMat.material.GetTexture("_MainTex").GetInstanceID() + "' -> '" + albedoRenderTextureFinal.GetInstanceID() + "'";
				errorMessage(error);
				return;
			}

			if (myMeshCollider == null)
			{
				errorMessage("Something (possibly a custom script?) has interfered with the Fur Grooming. The mesh collider was removed.");
				return;
			}

			Camera[] cameras = Camera.allCameras;
			foreach (Camera camera in cameras)
			{
				if (camera.isActiveAndEnabled && camera != myCamera)
				{
					errorMessage("Something (possibly a custom script?) has interfered with the Fur Grooming. The active camera was changed while the Fur Grooming was running.");
					return;
				}
			}

			FastFurGrooming[] groomingPrefabs = (FastFurGrooming[])Resources.FindObjectsOfTypeAll(typeof(FastFurGrooming));
			activeGroomers = 0;
			foreach (FastFurGrooming groomer in groomingPrefabs) if (groomer.isActiveAndEnabled) activeGroomers++;
			if (activeGroomers > 1)
			{
				errorMessage("There is more than 1 active copy of the Fur Grooming. Please remove the other Fur Grooming prefabs from the active Heirarchy.");
				return;
			}
		}

		catch (System.Exception e)
		{
			Debug.LogError(e.ToString());
			errorMessage("Failed Sanity Check: " + e.ToString());
			return;
		}
	}
}
#endif
