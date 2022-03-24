using SDFr;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LODFluid
{
    public class EnforceBoundarySolverInvoker : Singleton<EnforceBoundarySolverInvoker>
    {
        private ComputeShader ForceBasedBoundaryCS;
        private int solveBoundaryKernel;

        public EnforceBoundarySolverInvoker()
        {
            ForceBasedBoundaryCS = Resources.Load<ComputeShader>("Solver/EnforceBoundarySolver");
            solveBoundaryKernel = ForceBasedBoundaryCS.FindKernel("solveBoundary");
        }

        public void ApplyBoundaryInfluence(
            List<GameObject> vBoundaryObjects,
            ParticleBuffer vTargetParticle, 
            ComputeBuffer vParticleCountArgment,
            float vParticleRadius)
        {
            ForceBasedBoundaryCS.SetFloat("ParticleRadius", vParticleRadius);

            ForceBasedBoundaryCS.SetBuffer(solveBoundaryKernel, "TargetParticleIndirectArgment_R", vParticleCountArgment);
            ForceBasedBoundaryCS.SetBuffer(solveBoundaryKernel, "TargetParticlePosition_RW", vTargetParticle.ParticlePositionBuffer);
            ForceBasedBoundaryCS.SetBuffer(solveBoundaryKernel, "TargetParticleVelocity_RW", vTargetParticle.ParticleVelocityBuffer);
            ForceBasedBoundaryCS.SetBuffer(solveBoundaryKernel, "TargetParticleFilter_RW", vTargetParticle.ParticleFilterBuffer);

            for (int i = 0; i < vBoundaryObjects.Count; i++)
            {
                SDFData SDF = vBoundaryObjects[i].GetComponent<SDFBaker>().sdfData;
                if (SDF == null)
                {
                    Debug.LogError(i.ToString() + "Th Object is not a Boundary, there are no SDFBaker in it!");
                }

                ForceBasedBoundaryCS.SetFloats("SDFDomainMin", SDF.bounds.min.x, SDF.bounds.min.y, SDF.bounds.min.z);
                ForceBasedBoundaryCS.SetInts("SDFResolution", SDF.dimensions.x, SDF.dimensions.y, SDF.dimensions.z);
                ForceBasedBoundaryCS.SetFloats("SDFCellSize", SDF.voxelSize.x, SDF.voxelSize.y, SDF.voxelSize.z);

                Rigidbody CurrRigidbody = vBoundaryObjects[i].GetComponent<Rigidbody>();
                if (CurrRigidbody != null)
                    ForceBasedBoundaryCS.SetFloats("BoundaryVel", CurrRigidbody.velocity.x, CurrRigidbody.velocity.y, CurrRigidbody.velocity.z);
                else
                    ForceBasedBoundaryCS.SetFloats("BoundaryVel", 0, 0, 0);

                Vector3 Position = vBoundaryObjects[i].transform.position;
                Vector3 Scale = vBoundaryObjects[i].transform.localScale;
                Matrix4x4 Rotation = new Matrix4x4();
                Rotation.SetTRS(new Vector3(0, 0, 0), vBoundaryObjects[i].transform.rotation, new Vector3(1, 1, 1));
                ForceBasedBoundaryCS.SetFloats("Translate", Position.x, Position.y, Position.z);
                ForceBasedBoundaryCS.SetMatrix("Rotation", Rotation);
                ForceBasedBoundaryCS.SetMatrix("InvRotation", Rotation.inverse);

                ForceBasedBoundaryCS.SetTexture(solveBoundaryKernel, "SignedDistance_R", SDF.sdfTexture);
                ForceBasedBoundaryCS.DispatchIndirect(solveBoundaryKernel, vParticleCountArgment);
            }
        }
    }
}
