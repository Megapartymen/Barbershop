// This legacy code is obsolete, and has been replaced with the V3 pipeline. However, this code may be
// useful for situations where tesselation doesn't work. For example, apparently CVR has an option to
// limit tesellation, probably intended as a speed optimization, but it breaks the faster V3 code,
// thus forcing CVR users to use this slower V2 legacy code if that option is enabled.
//
// That being said, this old V2 code is still pretty damn fast. Its main drawback is lower resolution.


// Behold! A tesselation shader that does not tesselate! Instead, it does the exact opposite of
// its intended purpose and deletes triangles.
// 
// But why are we using a tesselation shader to do this? Because it's literally the
// only way (in DX11) to cull triangles before the geometry shader processes them.
//
// The result is a HUGE speed boost, for a few reasons.
// 
// First, regular backface culling happens AFTER the geometry shader does all of its calculations
// (because maybe the geometry shader will turn them around), but we're culling them before that.
// 
// Second, even if the only thing the geometry shader does is exit (because the avatar is far away
// and there are no layers of fur to render), it still takes a very significant amount of time to
// decide to do nothing. This isn't as bad in DX12, but in DX11 it cuts framerate roughly in half.
// 
// Thirdly, the geometry shader is kinda slow. That's not a problem when it's doing calculations that
// then keep the fragment shader busy, but if it's all stuff that doesn't actually end up on-screen
// then the geometry shader becomes a bottleneck in the render pipeline.
//
// (Side note: This is probably my favourite bit of code. It has a very old-school 90s demo-scene
// vibe to it. It's a simple hack that effectively turbo-charges the rest of my shader.)


[UNITY_domain("tri")]
[UNITY_outputcontrolpoints(3)]
[UNITY_outputtopology("triangle_cw")]
[UNITY_partitioning("integer")]
[UNITY_patchconstantfunc("patchConstantFunction")]
hullGeomInput hull(InputPatch<hullGeomInput, 3> patch, uint id : SV_OutputControlPointID)
{
	return patch[id];
}

struct TessellationFactors {
	float edge[3] : SV_TessFactor;
	float inside : SV_InsideTessFactor;
};

TessellationFactors patchConstantFunction(InputPatch<hullGeomInput, 3> patch)
{
	// How many layers are we rendering? 
	float visibleLayers = floor(max(max(patch[0].VISIBLELAYERS, patch[1].VISIBLELAYERS), patch[2].VISIBLELAYERS));


	// Is this triangle on the screen?
	// The vertex shader calculates its min/max possible xy positions for all points along the fur thickness, and
	// clamps the result between -1 and 1. Finding the min/max of all 3 vertices gives us the min/max of the whole
	// triangle. If we then subtract the max from the min, and the result is non-zero, then the axis is on-screen.
	// If both the x any y axis are on-screen, the triangle is visible. This errors on the side of inclusion.
	float2 screenPosMin = min(min(patch[0].screenPosMin, patch[1].screenPosMin), patch[2].screenPosMin);
	float2 screenPosMax = max(max(patch[0].screenPosMax, patch[1].screenPosMax), patch[2].screenPosMax);
	bool onScreen = all(abs(screenPosMin - screenPosMax) > 0.0001);


	// Cull backwards-facing triangles if all 3 vertexes are pointing too far away from the camera
	bool cullOkay = min(min(patch[0].FURCULLTEST, patch[1].FURCULLTEST), patch[2].FURCULLTEST) < HULL_BACKFACE_CULLING;

	float visible = visibleLayers >= 1 && onScreen && cullOkay ? 1.0 : 0.0;

	TessellationFactors f;
	f.edge[0] = visible;
	f.edge[1] = visible;
	f.edge[2] = visible;
	f.inside = 1;
	return f;
}

[UNITY_domain("tri")]
hullGeomInput doma(TessellationFactors factors, OutputPatch<hullGeomInput, 3> patch, float3 barycentricCoordinates : SV_DomainLocation)
{
	return(patch[barycentricCoordinates.x > 0.5 ? 0 : barycentricCoordinates.y > 0.5 ? 1 : 2]);
}