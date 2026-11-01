// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

using Vortice.Direct2D1;
using Vortice.DirectWrite;
using Vortice.Mathematics;

class G2D2DContext : IDisposable
{
	public static G2D2DContext? Instance { get; private set; }

	public ID2D1HwndRenderTarget RenderTarget { get; private set; }
	public ID2D1Factory1 Factory { get; }
	public IDWriteFactory DWriteFactory { get; }

	public G2D2DContext(IntPtr hwnd, int width, int height)
	{
		if (Instance != null)
		{
			throw new InvalidOperationException("G2D2DContext instance already exists.");
		}
		Factory = D2D1.D2D1CreateFactory<ID2D1Factory1>();
		DWriteFactory = DWrite.DWriteCreateFactory<IDWriteFactory>();
		RenderTarget = Factory.CreateHwndRenderTarget(
			  // 좌표와 픽셀을 1:1로 맞춥니다. 창 크기 배율은 G2AppBase에서 적용하므로,
			  // Windows 고해상도 배율이 여기서 중복 적용되지 않도록 합니다.
			  new RenderTargetProperties { DpiX = 96.0f, DpiY = 96.0f }
			, new HwndRenderTargetProperties
			{
				Hwnd = hwnd,
				PixelSize = new SizeI(width, height)
			}
		);
		Instance = this;
	}

	public void Resize(int width, int height)
	{
		RenderTarget.Resize(new SizeI(width, height));
	}

	public void Dispose()
	{
		RenderTarget.Dispose();
		DWriteFactory.Dispose();
		Factory.Dispose();
		Instance = null;
	}
}
