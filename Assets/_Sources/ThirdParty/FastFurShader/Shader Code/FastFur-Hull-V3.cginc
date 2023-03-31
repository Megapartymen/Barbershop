// This is V2 of my continuing efforts to hack the Hull + Domain shaders!
// 
// Sure, they're built for tessellation, but I want to use them for a different purpose. I want them
// to selectively cull or copy whole triangles, not split them up.
// 
// Normally, the Geometry shader must reserve enough memory for all possible fur layers, even if it
// decides not to render them. The result is that even when no fur is rendering, a full 50% of the
// render time is wasted on the Geometry shader deciding to do nothing (at least in DX11).
// 
// But the Hull shader can be told to do multiplication by 0, so it can throw out everything early in
// the render pipeline, resulting in a massive speed boost when the fur doesn't need to be rendered,
// because the Geometry shader never runs. That was the hack I used in V1.
// 
// In V2 I take it even further. The Hull shader takes over the task of copying triangles. If I want 6
// triangles, I can get exactly 6 triangles, and they will be fed one-at-a-time to the Geometry shader,
// which then only ever has to reserve enough memory for a single triangle.
// 
// The Domain shader participates in this hack by ignoring the domainLocation that it is supposed
// to be using to tesselate. I don't want to tesselate. Instead it just passes the corner data directly.
// 
// There are some limitations and quirks that need to be taken in to consideration:
// 
//  - We can render 0 by setting the edge factor to 0
//  - We can render 1 by setting the edge factor to 1
//  - We can render 3 by setting the edge factor to 1, inside factor to 2
//  - We can render 4 by setting the edge factor to 2, inside factor to 2
//  - We can render 5 by setting the edge factor to 3, inside factor to 2
//  - etc...
// 
// We can't render 2 triangles, because if we set the edge factor to 2 the hull shader will also force the
// inside factor to a minimum of 2, which results in 4 triangles. I'm sure there are sound technical reasons
// why this happens, but in any case we can't do anything about it.
// 
// The Domain shader doesn't know we want the original triangles preserved, so in its efforts to optimize
// tesselation it only processes each vertex once. That creates problems for us, because that vertex might
// be the X corner on one triangle, but the Y corner on another, so which data do we send? No matter which
// corners we choose the result is that some triangles will get duplicate copies of one corner, but be missing
// the data for the 3rd.
//
// The solution is two-part. First, we make the centre vertex X+Y+Z. Then we only need to make sure the other
// 2 corners aren't the same, because if we have any 2 corners, we can subtract them from the centre to recover
// the 3rd corner. If we are only rendering 1 or 3 triangles, this works right away.
//
// The second part is what to do if we want 4 or more triangles. In that case, we need to alternate between
// sending the Y and X corner data for triangles that are along the edge that is being split up by our edge
// factor. The result is that no triangle gets duplicate data:  Y <--t--> X <--t--> Y <--t--> X <--t--> Z


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
	float triangles = ceil(visibleLayers * PIPELINE3_CHUNKSCALE);


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
	f.edge[0] = visible ? max(1, floor(triangles - 2)) : 0;
	f.edge[1] = 1;
	f.edge[2] = 1;
	f.inside = triangles >= 2 ? 2 : 1;
	return f;
}

[UNITY_domain("tri")]
hullGeomInput doma(TessellationFactors factors, OutputPatch<hullGeomInput, 3> patch, float3 domainLocation : SV_DomainLocation)
{
#define RETURN(corner, index) {hullGeomInput o = patch[corner]; o.HULLCOORDS = float2(corner, index); return(o);}

	// Note that there are tiny rounding errors when multiplying by the domainLocation,
	// hence the use of 0.999, round(), floor(), etc...

	// If this is a corner, pass it through.
	if (domainLocation.x > 0.999) RETURN(0, 0);
	if (domainLocation.y > 0.999) RETURN(1, 1);
	if (domainLocation.z > 0.999) RETURN(2, 2);

	float visibleLayers = floor(max(max(patch[0].VISIBLELAYERS, patch[1].VISIBLELAYERS), patch[2].VISIBLELAYERS));
	float triangles = ceil(visibleLayers * PIPELINE3_CHUNKSCALE);

	// If this is the centre point, then pass it through by adding all 3 verticies together.
	// The Geometry shader can subtract the two known corners to recover the 3rd corner.
	if (domainLocation.x > 0.3 && domainLocation.y > 0.3 && domainLocation.z > 0.3)
	{
		hullGeomInput o = patch[0];
		o.VISIBLELAYERS = visibleLayers;
#define BLEND(var) o.var = patch[0].var + patch[1].var + patch[2].var;
		BLEND(worldPos);
		BLEND(worldNormal);
		BLEND(uv);
#if !defined(PREPASS)
		BLEND(lightData1);
		BLEND(lightData2);
#endif
#if defined(USE_SHADOWS) && (defined(SHADOWS_SCREEN) || defined(SHADOWS_DEPTH) || defined(SHADOWS_CUBE))
		BLEND(_ShadowCoord);
#endif
		BLEND(furData);
		BLEND(windEffect);
		o.HULLCOORDS = float2(-1, 2);// -1 indicates this is the blended corner
		return(o);
	}

	// We're on the edge
	float subTriangle = round((triangles - 2.0) * domainLocation.z);
	float index = subTriangle + 4.0;

	// If this is an odd-number point, send the X corner
	if (((subTriangle * 0.5) - floor(subTriangle * 0.5)) > 0.25) RETURN(0, index);
	// Otherwise send the Y corner
	RETURN(1, index);
}

