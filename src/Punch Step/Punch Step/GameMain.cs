// -------------------------------------------------------------------------------------------------------------------------------------------------------------
// Author: 3dapi (https://github.com/3dapi)
// -------------------------------------------------------------------------------------------------------------------------------------------------------------

class GameMain : G2AppBase
{
	public override System.Drawing.Size ScreenSize => GameGlobal.ScreenSize;
	public override string GameName => GameGlobal.GameName;

	// 처음에는 타이틀을 표시하고, 시작 버튼을 누르면 플레이 장면을 만듭니다.
	private SceneTitle? _title;
	private SceneMain? _scene;

	protected override void Initialize()
	{
		_title = new SceneTitle();
		_title.Initialize();
	}

	protected override void Update()
	{
		if (_title != null)
		{
			switch (_title.Update(Input, DeltaTime))
			{
				case SceneTitle.Action.StartGame:
					StartGameplay();
					break;
				case SceneTitle.Action.ExitGame:
					Close();
					break;
			}
			return;
		}
		_scene?.Update(Input, DeltaTime);
		if (_scene?.ReturnToTitleRequested == true)
		{
			_scene.Dispose();
			_scene = null;
			_title = new SceneTitle();
			_title.Initialize();
			Input.Reset();
		}
	}

	private void StartGameplay()
	{
		_scene = new SceneMain();
		_scene.Initialize(); // 인게임에 진입할 때 BGM도 시작합니다.
		_title?.Dispose();
		_title = null;
		Input.Reset();
	}

	protected override void Render()
	{
		if (_title != null)
		{
			_title.Render();
			return;
		}
		_scene?.Render();
	}

	public override void Dispose()
	{
		// 이미지를 먼저 해제하고, 이후 프레임워크의 그래픽 장치를 해제합니다.
		_title?.Dispose();
		_title = null;
		_scene?.Dispose();
		_scene = null;
		base.Dispose();
	}
}
