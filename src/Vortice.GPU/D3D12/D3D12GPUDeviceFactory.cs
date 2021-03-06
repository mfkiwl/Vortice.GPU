// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using static Vortice.Direct3D12.D3D12;
using static Vortice.DXGI.DXGI;
using static Vortice.GPU.D3DUtils;

namespace Vortice.GPU.D3D12;

internal static class D3D12GPUDeviceFactory
{
    public static readonly Lazy<bool> IsSupported = new(CheckIsSupported);

    private static bool CheckIsSupported()
    {
        if (!PlatformInfo.IsWindows)
        {
            return false;
        }

#if NET6_0_OR_GREATER
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return false;
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
        {
            return false;
        }
#endif

        if (CreateDXGIFactory2(false, out IDXGIFactory4? dxgiFactory).Failure)
        {
            return false;
        }

        bool foundCompatibleDevice = false;
        for (int adapterIndex = 0; dxgiFactory!.EnumAdapters1(adapterIndex, out IDXGIAdapter1 adapter).Success; adapterIndex++)
        {
            AdapterDescription1 desc = adapter.Description1;

            // Don't select the Basic Render Driver adapter.
            if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
            {
                adapter.Dispose();

                continue;
            }

            if (IsSupported(adapter, FeatureLevel.Level_11_0))
            {
                foundCompatibleDevice = true;
                break;
            }
        }

        return foundCompatibleDevice;
    }

    public static D3D12GPUDevice Create(in GPUDeviceDescriptor descriptor)
    {
        using (IDXGIFactory4 factory = CreateDXGIFactory2<IDXGIFactory4>(descriptor.ValidationMode != ValidationMode.Disabled))
        {
            IDXGIAdapter1? adapter = default;

            IDXGIFactory6? dxgiFactory6 = factory.QueryInterfaceOrNull<IDXGIFactory6>();

            if (dxgiFactory6 != null)
            {
                GpuPreference gpuPreference = ToDXGI(descriptor.PowerPreference);

                for (int adapterIndex = 0; dxgiFactory6!.EnumAdapterByGpuPreference(adapterIndex, gpuPreference, out adapter).Success; adapterIndex++)
                {
                    AdapterDescription1 desc = adapter!.Description1;

                    // Don't select the Basic Render Driver adapter.
                    if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
                    {
                        adapter.Dispose();

                        continue;
                    }

                    if (IsSupported(adapter, FeatureLevel.Level_11_0))
                    {
                        break;
                    }
                }

                dxgiFactory6.Dispose();
            }

            if (adapter == null)
            {
                for (int adapterIndex = 0; factory.EnumAdapters1(adapterIndex, out adapter).Success; adapterIndex++)
                {
                    AdapterDescription1 desc = adapter.Description1;

                    // Don't select the Basic Render Driver adapter.
                    if ((desc.Flags & AdapterFlags.Software) != AdapterFlags.None)
                    {
                        adapter.Dispose();

                        continue;
                    }

                    if (IsSupported(adapter, FeatureLevel.Level_11_0))
                    {
                        break;
                    }
                }
            }

            if (adapter == null)
            {
                // Try WARP12 instead
                if (factory.EnumWarpAdapter(out adapter).Failure)
                {
                    throw new GPUException("WARP12 not available. Enable the 'Graphics Tools' optional feature");
                }
            }

            if (adapter == null)
            {
                throw new GPUException("No Direct3D 12 device found");
            }

            return new D3D12GPUDevice(adapter, descriptor);
        }
    }
}
