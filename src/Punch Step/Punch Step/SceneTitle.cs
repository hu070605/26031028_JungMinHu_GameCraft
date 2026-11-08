using System.Windows.Forms;
using Vortice.Mathematics;

// 타이틀 이미지, 마우스 입력과 버튼 클릭 애니메이션을 관리합니다.
class SceneTitle : IDisposable
{
	public enum Action { None, StartGame, ExitGame }

	private const double ClickAnimationDuration = 0.20;
	private const float MaximumButtonScale = 1.10f;
	private static readonly Rect ScreenBounds = new(0.0f, 0.0f, 1280.0f, 720.0f);
	// 투명한 화면 전체가 아니라, 실제 버튼 그림의 영역만 사용합니다.
	private static readonly Rect StartButtonBounds = new(423.0f, 394.0f, 434.0f, 110.0f);
	private static readonly Rect ExitButtonBounds = new(460.0f, 522.0f, 360.0f, 82.0f);

	private G2Texture? _background;
	private G2Texture? _startButton;
	private G2Texture? _exitButton;
	private Action _pendingAction;
	private double _animationTime;

	public void Initialize()
	{
		_background = new G2Texture("resource/title/PunchStep_Title_Background.png");
		_startButton = new G2Texture("resource/title/PunchStep_Title_Start.png");
		_exitButton = new G2Texture("resource/title/PunchStep_Title_Exit.png");
	}

	public Action Update(G2InputContext input, double deltaTime)
	{
		// 재생 중에는 추가 클릭을 받지 않고, 원래 크기로 돌아온 뒤 동작합니다.
		if (_pendingAction != Action.None)
		{
			_animationTime += deltaTime;
			if (_animationTime >= ClickAnimationDuration)
			{
				Action completedAction = _pendingAction;
				_pendingAction = Action.None;
				_animationTime = 0.0;
				return completedAction;
			}
			return Action.None;
		}

		if (input.IsButtonDown(MouseButtons.Left))
		{
			if (IsInside(input.MousePosition, StartButtonBounds))
			{
				BeginClickAnimation(Action.StartGame);
			}
			else if (IsInside(input.MousePosition, ExitButtonBounds))
			{
				BeginClickAnimation(Action.ExitGame);
			}
		}
		return Action.None;
	}

	private static bool IsInside(System.Drawing.PointF mouse, Rect bounds)
	{
		return mouse.X >= bounds.X && mouse.X < bounds.X + bounds.Width &&
			mouse.Y >= bounds.Y && mouse.Y < bounds.Y + bounds.Height;
	}

	private void BeginClickAnimation(Action action)
	{
		_pendingAction = action;
		_animationTime = 0.0;
	}

	public void Render()
	{
		_background?.Draw(ScreenBounds, ScreenBounds);
		RenderButton(_startButton, StartButtonBounds, Action.StartGame);
		RenderButton(_exitButton, ExitButtonBounds, Action.ExitGame);
	}

	private void RenderButton(G2Texture? texture, Rect bounds, Action action)
	{
		float scale = 1.0f;
		if (_pendingAction == action)
		{
			float progress = Math.Clamp((float)(_animationTime / ClickAnimationDuration), 0.0f, 1.0f);
			// 1배 → 1.1배 → 1배. 버튼 중심을 기준으로 부드럽게 확대합니다.
			scale += (MaximumButtonScale - 1.0f) * MathF.Sin(progress * MathF.PI);
		}

		float width = bounds.Width * scale;
		float height = bounds.Height * scale;
		Rect destination = new(
			bounds.X + (bounds.Width - width) * 0.5f,
			bounds.Y + (bounds.Height - height) * 0.5f, width, height);
		texture?.Draw(destination, bounds);
	}

	public void Dispose()
	{
		_exitButton?.Dispose();
		_exitButton = null;
		_startButton?.Dispose();
		_startButton = null;
		_background?.Dispose();
		_background = null;
	}
}
