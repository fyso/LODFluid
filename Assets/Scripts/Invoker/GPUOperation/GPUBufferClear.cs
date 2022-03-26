using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class GPUBufferClear
    {
        private ComputeShader GPUBufferClearCS;
        private int clearUIntBufferWithZeroKernel;
        private uint clearUIntBufferWithZeroGroupThreadNum;
        private int clearFloatBufferWithZeroKernel;
        private uint clearFloatBufferWithZeroGroupThreadNum;
        private int clearUIntBufferWithSequenceKernel;
        private uint clearUIntBufferWithSequenceGroupThreadNum;

        public GPUBufferClear()
        {
            GPUBufferClearCS = Resources.Load<ComputeShader>("Shaders/GPU Operation/GPUBufferClear");
            clearUIntBufferWithZeroKernel = GPUBufferClearCS.FindKernel("clearUIntBufferWithZero");
            GPUBufferClearCS.GetKernelThreadGroupSizes(clearUIntBufferWithZeroKernel, out clearUIntBufferWithZeroGroupThreadNum, out _, out _);
            clearFloatBufferWithZeroKernel = GPUBufferClearCS.FindKernel("clearFloatBufferWithZero");
            GPUBufferClearCS.GetKernelThreadGroupSizes(clearFloatBufferWithZeroKernel, out clearFloatBufferWithZeroGroupThreadNum, out _, out _);
            clearUIntBufferWithSequenceKernel = GPUBufferClearCS.FindKernel("clearUIntBufferWithSequence");
            GPUBufferClearCS.GetKernelThreadGroupSizes(clearUIntBufferWithSequenceKernel, out clearUIntBufferWithSequenceGroupThreadNum, out _, out _);
        }

        public void ClearUIntBufferWithZero(int vBufferSize, ComputeBuffer vTargetBuffer)
        {
            GPUBufferClearCS.SetInt("BufferSize", vBufferSize);
            GPUBufferClearCS.SetBuffer(clearUIntBufferWithZeroKernel, "TargetUIntBuffer_RW", vTargetBuffer);
            GPUBufferClearCS.Dispatch(clearUIntBufferWithZeroKernel, (int)Mathf.Ceil(((float)vBufferSize / clearUIntBufferWithZeroGroupThreadNum)), 1, 1);
        }

        public void ClearUIntBufferWithSequence(int vBufferSize, ComputeBuffer vTargetBuffer)
        {
            GPUBufferClearCS.SetInt("BufferSize", vBufferSize);
            GPUBufferClearCS.SetBuffer(clearUIntBufferWithSequenceKernel, "TargetUIntBuffer_RW", vTargetBuffer);
            GPUBufferClearCS.Dispatch(clearUIntBufferWithSequenceKernel, (int)Mathf.Ceil(((float)vBufferSize / clearUIntBufferWithSequenceGroupThreadNum)), 1, 1);
        }

        public void ClearFloatBufferWithZero(int vBufferSize, ComputeBuffer vTargetBuffer)
        {
            GPUBufferClearCS.SetInt("BufferSize", vBufferSize);
            GPUBufferClearCS.SetBuffer(clearFloatBufferWithZeroKernel, "TargetFloatBuffer_RW", vTargetBuffer);
            GPUBufferClearCS.Dispatch(clearFloatBufferWithZeroKernel, (int)Mathf.Ceil(((float)vBufferSize / clearFloatBufferWithZeroGroupThreadNum)), 1, 1);
        }
    }
}
