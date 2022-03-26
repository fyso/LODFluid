using SDFr;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class CubicMap
    {
        public ComputeBuffer Data;
        public Vector3 VolumeMapDomainMin;
        public float VolumeMapCellSize;
        public Vector3Int VolumeResolution;
        public int VertexCount;
        public int XEdgeCount;
        public int YEdgeCount;
        public int ZEdgeCount;
        public int TotalEdgeCount;
        public int TotalNodeCount;

        public CubicMap(SDFData vSDF)
        {
            int Extent = 0;
            VolumeMapCellSize = Mathf.Max(vSDF.voxelSize.x, Mathf.Max(vSDF.voxelSize.y, vSDF.voxelSize.z));
            VolumeMapDomainMin = vSDF.bounds.min - new Vector3(Extent, Extent, Extent) * VolumeMapCellSize;
            VolumeResolution = vSDF.dimensions + new Vector3Int(Extent * 2, Extent * 2, Extent * 2);
            VertexCount = (VolumeResolution.x + 1) * (VolumeResolution.y + 1) * (VolumeResolution.z + 1);
            XEdgeCount = (VolumeResolution.x + 0) * (VolumeResolution.y + 1) * (VolumeResolution.z + 1);
            YEdgeCount = (VolumeResolution.x + 1) * (VolumeResolution.y + 0) * (VolumeResolution.z + 1);
            ZEdgeCount = (VolumeResolution.x + 1) * (VolumeResolution.y + 1) * (VolumeResolution.z + 0);
            TotalEdgeCount = XEdgeCount + YEdgeCount + ZEdgeCount;
            TotalNodeCount = VertexCount + 2 * TotalEdgeCount;
            Data = new ComputeBuffer(TotalNodeCount, sizeof(float));
        }

        ~CubicMap()
        {
            Data.Dispose();
        }
    }

    public class VolumeMapBoundary
    {
        private ComputeShader m_GenerateVolumeMapCS;

        private int m_GenerateVolumeMapKernel;
        private int m_GenerateSignedDistanceMapKernel;
        private int m_QueryCloestPointAndVolumeKernel;
        private int m_ClearClosestPointAndVolumeKernel;

        private uint m_GenerateVolumeMapGroupThreadNum;
        private uint m_GenerateSignedDistanceMapGroupThreadNum;

        private float[] gaussian_weights_1_30 = new float[16 * 4]
        {
            0.027152459411758110563450685504f, 0, 0, 0,
            0.062253523938649010793788818319f, 0, 0, 0,
            0.095158511682492036287683845330f, 0, 0, 0,
            0.124628971255533488315947465708f, 0, 0, 0,
            0.149595988816575764523975067277f, 0, 0, 0,
            0.169156519395001675443168664970f, 0, 0, 0,
            0.182603415044922529064663763165f, 0, 0, 0,
            0.189450610455067447457366824892f, 0, 0, 0,
            0.189450610455067447457366824892f, 0, 0, 0,
            0.182603415044922529064663763165f, 0, 0, 0,
            0.169156519395001675443168664970f, 0, 0, 0,
            0.149595988816575764523975067277f, 0, 0, 0,
            0.124628971255533488315947465708f, 0, 0, 0,
            0.095158511682492036287683845330f, 0, 0, 0,
            0.062253523938649010793788818319f, 0, 0, 0,
            0.027152459411758110563450685504f, 0, 0, 0
        };

        private float[] gaussian_abscissae_1_30 = new float[16 * 4]
        {
            -0.989400934991649938510249739920f, 0, 0, 0,
            -0.944575023073232600268056557979f, 0, 0, 0,
            -0.865631202387831755196145877562f, 0, 0, 0,
            -0.755404408355002998654015300417f, 0, 0, 0,
            -0.617876244402643770570193737512f, 0, 0, 0,
            -0.458016777657227369680015272024f, 0, 0, 0,
            -0.281603550779258915426339626720f, 0, 0, 0,
            -0.095012509837637426635126303154f, 0, 0, 0,
            0.095012509837637426635126303154f, 0, 0, 0,
            0.281603550779258915426339626720f, 0, 0, 0,
            0.458016777657227369680015272024f, 0, 0, 0,
            0.617876244402643770570193737512f, 0, 0, 0,
            0.755404408355002998654015300417f, 0, 0, 0,
            0.865631202387831755196145877562f, 0, 0, 0,
            0.944575023073232600268056557979f, 0, 0, 0,
            0.989400934991649938510249739920f, 0, 0, 0
        };

        public VolumeMapBoundary()
        {
            m_GenerateVolumeMapCS = Resources.Load<ComputeShader>("Shaders/Solver/VolumeMapBoundarySolver");

            m_GenerateVolumeMapKernel = m_GenerateVolumeMapCS.FindKernel("generateVolumeMap");
            m_GenerateSignedDistanceMapKernel = m_GenerateVolumeMapCS.FindKernel("generateSignedDistanceMap");
            m_QueryCloestPointAndVolumeKernel = m_GenerateVolumeMapCS.FindKernel("queryCloestPointAndVolume");
            m_ClearClosestPointAndVolumeKernel = m_GenerateVolumeMapCS.FindKernel("clearClosestPointAndVolume");

            m_GenerateVolumeMapCS.GetKernelThreadGroupSizes(m_GenerateVolumeMapKernel, out m_GenerateVolumeMapGroupThreadNum, out _, out _);
            m_GenerateVolumeMapCS.GetKernelThreadGroupSizes(m_GenerateSignedDistanceMapKernel, out m_GenerateSignedDistanceMapGroupThreadNum, out _, out _);

            m_GenerateVolumeMapCS.SetFloats("gaussian_weights_1_30", gaussian_weights_1_30);
            m_GenerateVolumeMapCS.SetFloats("gaussian_abscissae_1_30", gaussian_abscissae_1_30);
        }

        public void GenerateBoundaryMapData(
            List<GameObject> vBoundaryObject,
            List<CubicMap> voVolumeMap,
            List<CubicMap> voSignedDistanceMap,
            float vSearchRadius, float vCubicZero)
        {
            voVolumeMap.Clear();
            voSignedDistanceMap.Clear();

            for (int i = 0; i < vBoundaryObject.Count; i++)
            {
                SDFData SDF = vBoundaryObject[i].GetComponent<SDFBaker>().sdfData;
                if (SDF == null)
                {
                    Debug.LogError(i.ToString() + "Th Object is not a Boundary, there are no SDFBaker in it!");
                }

                CubicMap VolumeMap = new CubicMap(SDF);
                CubicMap SignedDistanceMap = new CubicMap(SDF);

                m_GenerateVolumeMapCS.SetFloats("SDFDomainMin", SDF.bounds.min.x, SDF.bounds.min.y, SDF.bounds.min.z);
                m_GenerateVolumeMapCS.SetFloats("SDFCellSize", SDF.voxelSize.x, SDF.voxelSize.y, SDF.voxelSize.z);
                m_GenerateVolumeMapCS.SetInt("SDFResolutionX", SDF.dimensions.x);
                m_GenerateVolumeMapCS.SetInt("SDFResolutionY", SDF.dimensions.y);
                m_GenerateVolumeMapCS.SetInt("SDFResolutionZ", SDF.dimensions.z);

                m_GenerateVolumeMapCS.SetFloat("SearchRadius", vSearchRadius);
                m_GenerateVolumeMapCS.SetFloat("CubicZero", vCubicZero);

                m_GenerateVolumeMapCS.SetFloats("VolumeMapDomainMin", VolumeMap.VolumeMapDomainMin.x, VolumeMap.VolumeMapDomainMin.y, VolumeMap.VolumeMapDomainMin.z);
                m_GenerateVolumeMapCS.SetFloat("VolumeMapCellSize", VolumeMap.VolumeMapCellSize);
                m_GenerateVolumeMapCS.SetInt("VolumeMapResolutionX", VolumeMap.VolumeResolution.x);
                m_GenerateVolumeMapCS.SetInt("VolumeMapResolutionY", VolumeMap.VolumeResolution.y);
                m_GenerateVolumeMapCS.SetInt("VolumeMapResolutionZ", VolumeMap.VolumeResolution.z);

                m_GenerateVolumeMapCS.SetInt("VolumeMapTotalNodeCount", VolumeMap.TotalNodeCount);
                m_GenerateVolumeMapCS.SetInt("VolumeMapVertexNodeCount", VolumeMap.VertexCount);
                m_GenerateVolumeMapCS.SetInt("VolumeMapXEdgeNodeCount", VolumeMap.XEdgeCount);
                m_GenerateVolumeMapCS.SetInt("VolumeMapYEdgeNodeCount", VolumeMap.YEdgeCount);
                m_GenerateVolumeMapCS.SetInt("VolumeMapZEdgeNodeCount", VolumeMap.ZEdgeCount);

                m_GenerateVolumeMapCS.SetBuffer(m_GenerateSignedDistanceMapKernel, "SignedDistanceArray_RW", SignedDistanceMap.Data);
                m_GenerateVolumeMapCS.SetTexture(m_GenerateSignedDistanceMapKernel, "SignedDistanceTexture_R", SDF.sdfTexture);
                m_GenerateVolumeMapCS.Dispatch(m_GenerateSignedDistanceMapKernel, (int)Mathf.Ceil((float)VolumeMap.TotalNodeCount / m_GenerateSignedDistanceMapGroupThreadNum), 1, 1);

                m_GenerateVolumeMapCS.SetBuffer(m_GenerateVolumeMapKernel, "VolumeArray_RW", VolumeMap.Data);
                m_GenerateVolumeMapCS.SetTexture(m_GenerateVolumeMapKernel, "SignedDistanceTexture_R", SDF.sdfTexture);
                m_GenerateVolumeMapCS.SetBuffer(m_GenerateVolumeMapKernel, "SignedDistanceArray_RW", SignedDistanceMap.Data);
                m_GenerateVolumeMapCS.Dispatch(m_GenerateVolumeMapKernel, (int)Mathf.Ceil((float)VolumeMap.TotalNodeCount / m_GenerateVolumeMapGroupThreadNum), 1, 1);

                voVolumeMap.Add(VolumeMap);
                voSignedDistanceMap.Add(SignedDistanceMap);
            }
        }

        public void QueryClosestPointAndVolume(
            ComputeBuffer vTargetParticleIndirectArgment,
            ParticleBuffer vTargetParticle,
            List<GameObject> vBoundaryObject,
            List<CubicMap> vVolumeMap,
            List<CubicMap> vSignedDistanceMap,
            ComputeBuffer voClosestPoint,
            ComputeBuffer voDistance,
            ComputeBuffer voVolume,
            ComputeBuffer voBoundaryVelocity,
            float vSearchRadius,
            float vParticleRadius)
        {
            m_GenerateVolumeMapCS.SetBuffer(m_ClearClosestPointAndVolumeKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            m_GenerateVolumeMapCS.SetBuffer(m_ClearClosestPointAndVolumeKernel, "ClosestPoint_RW", voClosestPoint);
            m_GenerateVolumeMapCS.SetBuffer(m_ClearClosestPointAndVolumeKernel, "Distance_RW", voDistance);
            m_GenerateVolumeMapCS.SetBuffer(m_ClearClosestPointAndVolumeKernel, "Volume_RW", voVolume);
            m_GenerateVolumeMapCS.DispatchIndirect(m_ClearClosestPointAndVolumeKernel, vTargetParticleIndirectArgment);

            for (int i = 0; i < vBoundaryObject.Count; i++)
            {
                SDFData SDF = vBoundaryObject[i].GetComponent<SDFBaker>().sdfData;
                if (SDF == null)
                {
                    Debug.LogError(i.ToString() + "Th Object is not a Boundary, there are no SDFBaker in it!");
                }

                m_GenerateVolumeMapCS.SetFloats("SDFDomainMin", SDF.bounds.min.x, SDF.bounds.min.y, SDF.bounds.min.z);
                m_GenerateVolumeMapCS.SetFloats("SDFCellSize", SDF.voxelSize.x, SDF.voxelSize.y, SDF.voxelSize.z);
                m_GenerateVolumeMapCS.SetInt("SDFResolutionX", SDF.dimensions.x);
                m_GenerateVolumeMapCS.SetInt("SDFResolutionY", SDF.dimensions.y);
                m_GenerateVolumeMapCS.SetInt("SDFResolutionZ", SDF.dimensions.z);

                m_GenerateVolumeMapCS.SetFloat("SearchRadius", vSearchRadius);
                m_GenerateVolumeMapCS.SetFloat("ParticleRadius", vParticleRadius);

                m_GenerateVolumeMapCS.SetFloats("VolumeMapDomainMin", vVolumeMap[i].VolumeMapDomainMin.x, vVolumeMap[i].VolumeMapDomainMin.y, vVolumeMap[i].VolumeMapDomainMin.z);
                m_GenerateVolumeMapCS.SetFloat("VolumeMapCellSize", vVolumeMap[i].VolumeMapCellSize);
                m_GenerateVolumeMapCS.SetInt("VolumeMapResolutionX", vVolumeMap[i].VolumeResolution.x);
                m_GenerateVolumeMapCS.SetInt("VolumeMapResolutionY", vVolumeMap[i].VolumeResolution.y);
                m_GenerateVolumeMapCS.SetInt("VolumeMapResolutionZ", vVolumeMap[i].VolumeResolution.z);

                m_GenerateVolumeMapCS.SetInt("VolumeMapTotalNodeCount", vVolumeMap[i].TotalNodeCount);
                m_GenerateVolumeMapCS.SetInt("VolumeMapVertexNodeCount", vVolumeMap[i].VertexCount);
                m_GenerateVolumeMapCS.SetInt("VolumeMapXEdgeNodeCount", vVolumeMap[i].XEdgeCount);
                m_GenerateVolumeMapCS.SetInt("VolumeMapYEdgeNodeCount", vVolumeMap[i].YEdgeCount);
                m_GenerateVolumeMapCS.SetInt("VolumeMapZEdgeNodeCount", vVolumeMap[i].ZEdgeCount);

                Vector3 Position = vBoundaryObject[i].transform.position;
                Matrix4x4 Rotation = new Matrix4x4();
                Rotation.SetTRS(new Vector3(0, 0, 0), vBoundaryObject[i].transform.rotation, new Vector3(1, 1, 1));
                m_GenerateVolumeMapCS.SetFloats("Translate", Position.x, Position.y, Position.z);
                m_GenerateVolumeMapCS.SetMatrix("Rotation", Rotation);
                m_GenerateVolumeMapCS.SetMatrix("InvRotation", Rotation.inverse);

                Vector3 BundaryVelocity = Vector3.zero;
                Rigidbody CurrRigidBody = vBoundaryObject[i].GetComponent<Rigidbody>();
                if (vBoundaryObject[i].GetComponent<Rigidbody>())
                {
                    BundaryVelocity = CurrRigidBody.velocity;
                }
                m_GenerateVolumeMapCS.SetFloats("BoundaryVelocity", BundaryVelocity.x, BundaryVelocity.y, BundaryVelocity.z);

                m_GenerateVolumeMapCS.SetBuffer(m_QueryCloestPointAndVolumeKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                m_GenerateVolumeMapCS.SetBuffer(m_QueryCloestPointAndVolumeKernel, "TargetParticlePosition_RW", vTargetParticle.ParticlePositionBuffer);
                m_GenerateVolumeMapCS.SetBuffer(m_QueryCloestPointAndVolumeKernel, "ClosestPoint_RW", voClosestPoint);
                m_GenerateVolumeMapCS.SetBuffer(m_QueryCloestPointAndVolumeKernel, "Distance_RW", voDistance);
                m_GenerateVolumeMapCS.SetBuffer(m_QueryCloestPointAndVolumeKernel, "Volume_RW", voVolume);
                m_GenerateVolumeMapCS.SetBuffer(m_QueryCloestPointAndVolumeKernel, "BoundaryVelocity_RW", voBoundaryVelocity);
                m_GenerateVolumeMapCS.SetBuffer(m_QueryCloestPointAndVolumeKernel, "VolumeArray_RW", vVolumeMap[i].Data);
                m_GenerateVolumeMapCS.SetBuffer(m_QueryCloestPointAndVolumeKernel, "SignedDistanceArray_RW", vSignedDistanceMap[i].Data);

                m_GenerateVolumeMapCS.DispatchIndirect(m_QueryCloestPointAndVolumeKernel, vTargetParticleIndirectArgment);
            }
        }
    }
}