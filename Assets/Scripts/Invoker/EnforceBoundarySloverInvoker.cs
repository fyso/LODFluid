using SDFr;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LODFluid
{
    public class EnforceBoundarySloverInvoker : Singleton<EnforceBoundarySloverInvoker>
    {
        private ComputeShader ForceBasedBoundaryCS;
        private int solveBoundaryKernel;

        public EnforceBoundarySloverInvoker()
        {
            ForceBasedBoundaryCS = Resources.Load<ComputeShader>("Slover/EnforceBoundarySlover");
            solveBoundaryKernel = ForceBasedBoundaryCS.FindKernel("solveBoundary");
        }

        public void ApplyBoundaryInfluence(
            List<GameObject> vBoundaryObject,
            ParticleBuffer vTargetParticle, 
            ComputeBuffer vParticleCountArgment)
        {
            ForceBasedBoundaryCS.SetBuffer(solveBoundaryKernel, "TargetParticleIndirectArgment_R", vParticleCountArgment);
            ForceBasedBoundaryCS.SetBuffer(solveBoundaryKernel, "TargetParticlePosition_RW", vTargetParticle.ParticlePositionBuffer);
            ForceBasedBoundaryCS.SetBuffer(solveBoundaryKernel, "TargetParticleVelocity_RW", vTargetParticle.ParticleVelocityBuffer);

            for (int i = 0; i < vBoundaryObject.Count; i++)
            {
                SDFData SDF = vBoundaryObject[i].GetComponent<SDFBaker>().sdfData;
                if (SDF == null)
                {
                    Debug.LogError(i.ToString() + "Th Object is not a Boundary, there are no SDFBaker in it!");
                }

                ForceBasedBoundaryCS.SetFloats("SDFDomainMin", SDF.bounds.min.x, SDF.bounds.min.y, SDF.bounds.min.z);
                ForceBasedBoundaryCS.SetInts("SDFResolution", SDF.dimensions.x, SDF.dimensions.y, SDF.dimensions.z);
                ForceBasedBoundaryCS.SetFloats("SDFCellSize", SDF.voxelSize.x, SDF.voxelSize.y, SDF.voxelSize.z);

                Rigidbody CurrRigidbody = vBoundaryObject[i].GetComponent<Rigidbody>();
                if (CurrRigidbody != null)
                    ForceBasedBoundaryCS.SetFloats("BoundaryVel", CurrRigidbody.velocity.x, CurrRigidbody.velocity.y, CurrRigidbody.velocity.z);
                else
                    ForceBasedBoundaryCS.SetFloats("BoundaryVel", 0, 0, 0);

                Vector3 Position = vBoundaryObject[i].transform.position;
                Vector3 Scale = vBoundaryObject[i].transform.localScale;
                Matrix4x4 Rotation = new Matrix4x4();
                Rotation.SetTRS(new Vector3(0, 0, 0), vBoundaryObject[i].transform.rotation, new Vector3(1, 1, 1));
                ForceBasedBoundaryCS.SetFloats("Translate", Position.x, Position.y, Position.z);
                ForceBasedBoundaryCS.SetFloats("Scale", Scale.x, Scale.y, Scale.z);
                ForceBasedBoundaryCS.SetMatrix("Rotation", Rotation);
                ForceBasedBoundaryCS.SetMatrix("InvRotation", Rotation.inverse);

                ForceBasedBoundaryCS.SetTexture(solveBoundaryKernel, "SignedDistance_R", SDF.sdfTexture);
                ForceBasedBoundaryCS.DispatchIndirect(solveBoundaryKernel, vParticleCountArgment);
            }
        }
    }
}
