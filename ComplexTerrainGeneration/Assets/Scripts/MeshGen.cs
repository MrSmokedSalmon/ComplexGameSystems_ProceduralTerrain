using UnityEngine;


public static class MeshGen
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float scale, int LOD)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int simplificationIncrement = (LOD == 0 ? 1 : LOD * 2);
        int vertsPerLine = (width - 1) / simplificationIncrement + 1;
        
        MeshData meshData = new MeshData(vertsPerLine, vertsPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y += simplificationIncrement)
        for (int x = 0; x < width; x += simplificationIncrement)
        {
                
            meshData.verticies[vertexIndex] = new Vector3(
                topLeftX + x, 
                heightMap[y,x] * scale, 
                topLeftZ - y);
                
            meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                
            if (x < width - 1 && y < height - 1)
            {
                meshData.AddTriangle(vertexIndex, vertexIndex + vertsPerLine + 1, vertexIndex + vertsPerLine);
                meshData.AddTriangle(vertexIndex + vertsPerLine + 1, vertexIndex, vertexIndex + 1);
            }
                
            vertexIndex++;
        }

        return meshData;
    }
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float scale, AnimationCurve _heightCurve, int LOD)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        int simplificationIncrement = (LOD == 0 ? 1 : LOD * 2);
        int vertsPerLine = (width - 1) / simplificationIncrement + 1;
        
        MeshData meshData = new MeshData(vertsPerLine, vertsPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y += simplificationIncrement)
            for (int x = 0; x < width; x += simplificationIncrement)
            {
                
                meshData.verticies[vertexIndex] = new Vector3(
                    topLeftX + x, 
                    heightCurve.Evaluate(heightMap[y,x]) * scale, 
                    topLeftZ - y);
                
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                
                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + vertsPerLine + 1, vertexIndex + vertsPerLine);
                    meshData.AddTriangle(vertexIndex + vertsPerLine + 1, vertexIndex, vertexIndex + 1);
                }
                
                vertexIndex++;
            }

        return meshData;
    }
}

public class MeshData
{
    public Vector3[] verticies;
    public int[] triangles;
    public Vector2[] uvs;

    private int triangleIndex;    
    
    public MeshData(int meshWidth, int meshHeight)
    {
        verticies = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        return mesh;
    }
}