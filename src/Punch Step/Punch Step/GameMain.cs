// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

class GameMain : G2AppBase
{
	public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
	public override string GameName => GameGlobal.GameName;

	// 게임 전체 흐름만 연결하고, 실제 화면 구성은 SceneMain에서 관리합니다.
	private SceneMain? _scene;

	protected override void Initialize()
	{
		_scene = new SceneMain();
		_scene.Initialize();
	}

	protected override void Update()
	{
		_scene?.Update(Input);
	}

	protected override void Render()
	{
		_scene?.Render();
	}

	public override void Dispose()
	{
		// 이미지를 먼저 해제하고, 이후 프레임워크의 그래픽 장치를 해제합니다.
		_scene?.Dispose();
		_scene = null;
		base.Dispose();
	}
}
