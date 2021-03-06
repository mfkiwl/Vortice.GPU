// Copyright © Amer Koleci and Contributors.
// Licensed under the MIT License (MIT). See LICENSE in the repository root for more information.

namespace Vortice.GPU;

/// <summary>
/// A <see langword="class"/> with methods to inspect the available devices on the current machine.
/// </summary>
internal static partial class GPUDeviceHelper
{
    public static void Shutdown()
    {
#if GPU_VULKAN_BACKEND
        Vulkan.VulkanGPUDeviceFactory.Shutdown();
#endif

#if GPU_D3D11_BACKEND
        //D3D11.D3D11GPUDeviceFactory.Shutdown();
#endif

#if GPU_D3D12_BACKEND
        //D3D12.D3D12GPUDeviceFactory.Shutdown();
#endif
    }

    public static GPUDevice CreateDevice(in GPUDeviceDescriptor descriptor)
    {
        GPUBackend backend = descriptor.PreferredBackend;
        if (backend == GPUBackend.Count)
        {
            backend = GetPlatformBackend();
        }

        switch (backend)
        {
#if GPU_VULKAN_BACKEND
            case GPUBackend.Vulkan:
                if (IsBackendSupported(GPUBackend.Vulkan))
                {
                    return Vulkan.VulkanGPUDeviceFactory.Create(descriptor);
                }

                throw new GPUException($"{nameof(GPUBackend.Vulkan)} is not supported");
#endif

#if GPU_D3D11_BACKEND
            case GPUBackend.Direct3D11:
                if (IsBackendSupported(GPUBackend.Direct3D11))
                {
                    return D3D11.D3D11GPUDeviceFactory.Create(descriptor);
                }

                throw new GPUException($"{nameof(GPUBackend.Direct3D11)} is not supported");
#endif

#if GPU_D3D12_BACKEND
            case GPUBackend.Direct3D12:
                if (IsBackendSupported(GPUBackend.Direct3D12))
                {
                    return D3D12.D3D12GPUDeviceFactory.Create(descriptor);
                }

                throw new GPUException($"{nameof(GPUBackend.Direct3D12)} is not supported");
#endif

            default:
                throw new GPUException($"{backend} is not supported!");
        }
    }

    public static bool IsBackendSupported(GPUBackend backend)
    {
        if (backend == GPUBackend.Count)
        {
            backend = GetPlatformBackend();
        }

        switch (backend)
        {
#if GPU_VULKAN_BACKEND
            case GPUBackend.Vulkan:
                return Vulkan.VulkanGPUDeviceFactory.IsSupported.Value;
#endif

#if GPU_D3D11_BACKEND
            case GPUBackend.Direct3D11:
                return D3D11.D3D11GPUDeviceFactory.IsSupported.Value;
#endif

#if GPU_D3D12_BACKEND
            case GPUBackend.Direct3D12:
                return D3D12.D3D12GPUDeviceFactory.IsSupported.Value;
#endif

            default:
                return false;
        }
    }

    public static GPUBackend GetPlatformBackend()
    {
        if (PlatformInfo.IsWindows)
        {
#if GPU_D3D12_BACKEND
            if (D3D12.D3D12GPUDeviceFactory.IsSupported.Value)
                return GPUBackend.Direct3D12;
#endif
        }
        else if (PlatformInfo.IsAndroid || PlatformInfo.IsLinux)
        {
#if GPU_VULKAN_BACKEND
            if (Vulkan.VulkanGPUDeviceFactory.IsSupported.Value)
                return GPUBackend.Direct3D12;
#endif
        }
        else if (PlatformInfo.IsMacOS)
        {
            // TODO: Metal
        }

        return GPUBackend.Count;
    }
}
