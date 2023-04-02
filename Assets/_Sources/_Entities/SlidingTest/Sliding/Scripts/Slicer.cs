using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts
{
    class Slicer
    {
        /// <summary>
        /// Slice the object by the plane 
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="objectToCut"></param>
        /// <returns></returns>
        public static GameObject[] Slice(Plane plane, GameObject objectToCut)
        {            
            //Get the current mesh and its verts and tris
            Mesh mesh = objectToCut.GetComponent<MeshFilter>().mesh;
            var a = mesh.GetSubMesh(0);
            Sliceable sliceable = objectToCut.GetComponent<Sliceable>();

            if(sliceable == null)
            {
                throw new NotSupportedException("Cannot slice non sliceable object, add the sliceable script to the object or inherit from sliceable to support slicing");
            }
            
            //Create left and right slice of hollow object
            SlicesMetadata slicesMeta = new SlicesMetadata(plane, mesh, sliceable.IsSolid, sliceable.ReverseWireTriangles, sliceable.ShareVertices, sliceable.SmoothVertices);            

            GameObject positiveObject = CreateMeshGameObject(objectToCut);
            positiveObject.name = string.Format("{0}_positive", objectToCut.name);

            GameObject negativeObject = CreateMeshGameObject(objectToCut);
            negativeObject.name = string.Format("{0}_negative", objectToCut.name);

            var negativeSliceable = negativeObject.GetComponent<Sliceable>();
            var positiveSliceable = positiveObject.GetComponent<Sliceable>();

            var positiveSideMeshData = slicesMeta.PositiveSideMesh;
            var negativeSideMeshData = slicesMeta.NegativeSideMesh;

            var negativeObjectDistanceFromRoot = GetDistanceFromRoot(negativeObject, negativeSideMeshData, sliceable.Root);
            var positiveObjectDistanceFromRoot = GetDistanceFromRoot(positiveObject, positiveSideMeshData, sliceable.Root);

            if (positiveObjectDistanceFromRoot == 0 || negativeObjectDistanceFromRoot == 0)
            {
                negativeSliceable.IsRoot = true;
                positiveSliceable.IsRoot = true;
                negativeSliceable.UseGravity = false;
                positiveSliceable.UseGravity = false;
            }
            else if (positiveObjectDistanceFromRoot > negativeObjectDistanceFromRoot)
            {
                negativeSliceable.IsRoot = true;
                negativeSliceable.UseGravity = false;
                positiveSliceable.IsRoot = false;
                positiveSliceable.UseGravity = true;
            }
            else
            {
                negativeSliceable.IsRoot = false;
                negativeSliceable.UseGravity = true;
                positiveSliceable.IsRoot = true;
                positiveSliceable.UseGravity = false;
            }

            negativeSliceable.Root = sliceable.Root;
            positiveSliceable.Root = sliceable.Root;

            positiveObject.GetComponent<MeshFilter>().mesh = positiveSideMeshData;
            negativeObject.GetComponent<MeshFilter>().mesh = negativeSideMeshData;

            SetupCollidersAndRigidBodys(ref positiveObject, positiveSideMeshData, positiveSliceable.UseGravity, positiveSliceable.IsRoot);
            SetupCollidersAndRigidBodys(ref negativeObject, negativeSideMeshData, negativeSliceable.UseGravity, negativeSliceable.IsRoot);

            return new GameObject[] { positiveObject, negativeObject};
        }

        private static float GetDistanceFromRoot(GameObject slice, Mesh mesh, Transform root)
        {
            var vertices = mesh.vertices;
            Vector3 verticesSum = new Vector3(0f, 0f, 0f);

            for (int i = 0; i < vertices.Length; i++)
            {
                verticesSum += vertices[i];
            }

            var meshCenter = verticesSum * (1f / vertices.Length);
            var reducedMeshCenter = meshCenter + slice.transform.position;

            float distance = Vector3.Distance(root.position, reducedMeshCenter);

            if (distance > 0)
            {
                return distance;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Creates the default mesh game object.
        /// </summary>
        /// <param name="originalObject">The original object.</param>
        /// <returns></returns>
        private static GameObject CreateMeshGameObject(GameObject originalObject)
        {
            var originalMaterial = originalObject.GetComponent<MeshRenderer>().materials;

            GameObject meshGameObject = new GameObject();
            Sliceable originalSliceable = originalObject.GetComponent<Sliceable>();

            meshGameObject.AddComponent<MeshFilter>();
            meshGameObject.AddComponent<MeshRenderer>();
            Sliceable sliceable = meshGameObject.AddComponent<Sliceable>();

            sliceable.IsSolid = originalSliceable.IsSolid;
            sliceable.ReverseWireTriangles = originalSliceable.ReverseWireTriangles;
            sliceable.UseGravity = originalSliceable.UseGravity;
            sliceable.gameObject.layer = originalSliceable.gameObject.layer;

            meshGameObject.GetComponent<MeshRenderer>().materials = originalMaterial;

            meshGameObject.transform.localScale = originalObject.transform.localScale;
            meshGameObject.transform.rotation = originalObject.transform.rotation;
            meshGameObject.transform.position = originalObject.transform.position;

            meshGameObject.tag = originalObject.tag;

            return meshGameObject;
        }
        
        

        /// <summary>
        /// Add mesh collider and rigid body to game object
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="mesh"></param>
        private static void SetupCollidersAndRigidBodys(ref GameObject gameObject, Mesh mesh, bool useGravity, bool isRoot)
        {
            if (isRoot)
            {
                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.convex = true;
            }

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = useGravity;
            rb.isKinematic = isRoot;
        }
    }
}
