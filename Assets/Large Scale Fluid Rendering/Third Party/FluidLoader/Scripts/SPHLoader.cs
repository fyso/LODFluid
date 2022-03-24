using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.Runtime.InteropServices;

[Serializable]
public class SPHLoader
{
    [HideInInspector]
    public uint ParticleCount = 0;

    [HideInInspector]
    public uint DiffuseCount = 0;

    public List<uint> DiffuseParticleCount;
    public uint MaxDiffuseParticleCount = 0;

    public ComputeBuffer ParticleBuffer;
    public ComputeBuffer DiffuseParticleBuffer;

    public Mesh mesh;
    private int[] MeshIndex;
    private List<Vector3[]> MeshPosList;
    private List<Color[]> MeshColorList;
    
    private List<SParticle> ParticlesList;
    private List<SDiffuseParticle> DiffuseParticlesList;

    private uint m_Offset = 0;
    private int  OfflineFrameNum = 0;
    private int  ParticelStructSize;
    private int  DiffuseParticelStructSize;
    private bool isImportDiffuse = false;

    public void init(string vPath, bool vIsImport)
    {
        isImportDiffuse = vIsImport;
        initParticlesData(vPath);
        createComputeBuffer();
    }
    public void step(uint vFrameIndex, SettingAsset settingAssetm, ref RenderStructSetting.SRenderStruct renderStruct)
    {
        int index = (int)(vFrameIndex % OfflineFrameNum);
        if (index == 0) m_Offset = 0;

        ParticleBuffer.SetData(ParticlesList, index * (int)ParticleCount, 0, (int)ParticleCount);

        if(isImportDiffuse)
        {
            DiffuseParticleBuffer.SetData(DiffuseParticlesList, (int)m_Offset, 0, (int)DiffuseParticleCount[index]);
            m_Offset += DiffuseParticleCount[index];
            DiffuseCount =  DiffuseParticleCount[index];
        }

        mesh.vertices = MeshPosList[index];
        mesh.colors = MeshColorList[index];
        if (mesh.triangles.Length != MeshIndex.Length) mesh.triangles = MeshIndex;
        mesh.RecalculateNormals();

        SetRenderResouce(ref renderStruct);
    }

   
    private void initParticlesData(string vPath)
    {
        OfflineFrameNum = 0;
        ParticelStructSize = Marshal.SizeOf(typeof(SParticle));
        DiffuseParticelStructSize = Marshal.SizeOf(typeof(SDiffuseParticle));
        ParticlesList = new List<SParticle>();
        DiffuseParticlesList = new List<SDiffuseParticle>();
        DiffuseParticleCount = new List<uint>();

        string curMainPath = Directory.GetCurrentDirectory() + "/" + vPath;

        mesh = new Mesh();
        MeshPosList = new List<Vector3[]>();
        MeshColorList = new List<Color[]>();

        if (curMainPath.IndexOf(".particle", StringComparison.OrdinalIgnoreCase) >= 0 
            || curMainPath.IndexOf(".diffuse", StringComparison.OrdinalIgnoreCase) >= 0 
            || curMainPath.IndexOf(".posAndColor", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            OfflineFrameNum = 1;
            curMainPath = curMainPath.Split('.')[0];
            LoadParticlesSourceWithFilePath<SParticle>(curMainPath + ".particle", ref ParticleCount, ref ParticlesList);
            if (isImportDiffuse)
            {
                uint num = 0;
                LoadParticlesSourceWithFilePath<SDiffuseParticle>(curMainPath + ".diffuse", ref num, ref DiffuseParticlesList);
                DiffuseParticleCount.Add(num);
                if (num > MaxDiffuseParticleCount) MaxDiffuseParticleCount = num;
            }

            LoadMeshVertexWithFilePath(curMainPath + ".posAndColor");
        }
        else
        {
            string[] paths = curMainPath.Split('/');
            string indexPath = "";
            for (int i = 0; i < paths.Length - 1; i++)
            {
                indexPath = indexPath + paths[i] + "/";
            }
            LoadParticlesSourceWithDirctory(curMainPath);
        }
        LoadMeshIndex(curMainPath);
    }
    
    private void createComputeBuffer()
    {
        ParticleBuffer = new ComputeBuffer((int)ParticleCount, ParticelStructSize);
        if(isImportDiffuse) DiffuseParticleBuffer = new ComputeBuffer((int)MaxDiffuseParticleCount, DiffuseParticelStructSize);
    } 

    private void SetRenderResouce(ref RenderStructSetting.SRenderStruct voRenderStruct)
    {
        voRenderStruct.m_ParticleBuffer = ParticleBuffer;
        voRenderStruct.m_ParticleCount = ParticleCount;
        voRenderStruct.m_DiffuseBuffer = DiffuseParticleBuffer;
        voRenderStruct.m_DiffuseCount = DiffuseCount;
        voRenderStruct.m_Mesh = mesh;
    }
    private bool LoadParticlesSourceWithFilePath<T>(string filePath, ref uint voParticleCount, ref List<T> voParticles)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("Particles Data文件不存在");
            return false;
        }

        FileStream fs = new FileStream(filePath, FileMode.Open);
        BinaryReader br = new BinaryReader(fs);
        byte[] byteArr = new byte[(int)br.BaseStream.Length];
        br.Read(byteArr, 0, (int)br.BaseStream.Length);

        int partileStructSize = Marshal.SizeOf(typeof(T));
        int particleIndex = 0;
        int numParticle = (int)br.BaseStream.Length / partileStructSize;
        while (particleIndex < numParticle)
        {
            IntPtr _StructIntPtr = Marshal.AllocHGlobal(partileStructSize);
            Marshal.Copy(byteArr, particleIndex * partileStructSize, _StructIntPtr, partileStructSize);
            object _StructObject = Marshal.PtrToStructure(_StructIntPtr, typeof(T));
            Marshal.FreeHGlobal(_StructIntPtr);

            voParticles.Add((T)_StructObject);
            particleIndex++;
        }
        voParticleCount = (uint)numParticle;

        br.Close();
        fs.Close();
        return true;
    }

    private bool LoadParticlesSourceWithDirctory(string dirctoryPath)
    {
        DirectoryInfo folder = new DirectoryInfo(dirctoryPath);
        FileInfo[] fileArray = folder.GetFiles("*.particle");
        Array.Sort(fileArray, new FileNameSort());

        foreach (FileInfo file in fileArray)
        {
            if (!LoadParticlesSourceWithFilePath<SParticle>(file.FullName, ref ParticleCount, ref ParticlesList))
                return false;

            OfflineFrameNum++;
        }

        FileInfo[] meshArray = folder.GetFiles("*.pos");
        Array.Sort(meshArray, new FileNameSort());
        foreach (FileInfo file in meshArray)
        {
            LoadMeshVertexWithFilePath(file.FullName);
        }

        if (isImportDiffuse)
        {
            FileInfo[] fileDiffuseArr = folder.GetFiles("*.diffuse");
            Array.Sort(fileDiffuseArr, new FileNameSort());

            foreach (FileInfo file in fileDiffuseArr)
            {
                uint num = 0;
                if (!LoadParticlesSourceWithFilePath<SDiffuseParticle>(file.FullName, ref num, ref DiffuseParticlesList))
                    return false;
                DiffuseParticleCount.Add(num);
                if (num > MaxDiffuseParticleCount) MaxDiffuseParticleCount = num;
            }
        }
        return true;
    }

    private bool LoadMeshVertexWithFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("Mesh Vertex文件不存在");
            return false;
        }

        FileStream fs = new FileStream(filePath, FileMode.Open);
        BinaryReader br = new BinaryReader(fs);
        byte[] byteArr = new byte[(int)br.BaseStream.Length];
        br.Read(byteArr, 0, (int)br.BaseStream.Length);

        int vertexStructSize = Marshal.SizeOf(typeof(SMeshVertexAttribute));
        int vertexNum = (int)br.BaseStream.Length / vertexStructSize;

        Vector3[] pos = new Vector3[vertexNum];
        Color[] color = new Color[vertexNum];
        for (int i = 0; i < vertexNum; i++)
        {
            IntPtr _StructIntPtr = Marshal.AllocHGlobal(vertexStructSize);
            Marshal.Copy(byteArr, i * vertexStructSize, _StructIntPtr, vertexStructSize);
            object _StructObject = Marshal.PtrToStructure(_StructIntPtr, typeof(SMeshVertexAttribute));
            Marshal.FreeHGlobal(_StructIntPtr);

            pos[i] = ((SMeshVertexAttribute)_StructObject).Position;
            color[i] = ((SMeshVertexAttribute)_StructObject).Color;
        }

        MeshPosList.Add(pos);
        MeshColorList.Add(color);
        br.Close();
        fs.Close();
        return true;
    }

    private bool LoadMeshIndex(string filePath)
    {
        string[] paths = filePath.Split('/');
        string indexPath = "";
        for (int i = 0; i < paths.Length - 2; i++)
        {
            indexPath = indexPath + paths[i] + "/";
        }
        indexPath += ".index";

        if (!File.Exists(indexPath))
        {
            Debug.LogError("Mesh Index文件不存在");
            return false;
        }

        FileStream fs = new FileStream(indexPath, FileMode.Open);
        BinaryReader br = new BinaryReader(fs);
        byte[] byteArr = new byte[(int)br.BaseStream.Length];
        br.Read(byteArr, 0, (int)br.BaseStream.Length);

        int indexNum = (int)br.BaseStream.Length / 4;
        MeshIndex = new int[indexNum];
        for (int i = 0; i < indexNum; i++)
        {
            IntPtr _StructIntPtr = Marshal.AllocHGlobal(4);
            Marshal.Copy(byteArr, i * 4, _StructIntPtr, 4);
            object _StructObject = Marshal.PtrToStructure(_StructIntPtr, typeof(uint));
            Marshal.FreeHGlobal(_StructIntPtr);

            MeshIndex[i] = (int)((uint)_StructObject);
        }
        br.Close();
        fs.Close();
        return true;
    }


    public void free()
    {
        if (ParticleBuffer != null) ParticleBuffer.Dispose();
        if(DiffuseParticleBuffer != null) DiffuseParticleBuffer.Dispose();
    }
}
