using System.Windows.Forms;
using Vortice.Direct2D1;
using Vortice.Mathematics;

// 게임 플레이 화면의 리소스, 발판 배치, 그리기와 BGM을 담당합니다.
class SceneMain : IDisposable
{
	// 1. 리소스 경로
	private const string BackgroundPath = "resource/background/PunchStep_Background.png";
	private const string PlatformPath = "resource/background/PunchStep_Platform.png";
	private const string GameplayBgmPath = "resource/sound/bgm/PunchStep_Gameplay.mp3";
	private const string PortalPath = "resource/animation/animation_monster/Portal_Idle_24Frame.png";
	private const int PortalFrameCount = 24;
	private const float PortalFrameWidth = 160.0f;
	private const double PortalFramesPerSecond = 20.0;
	private const float PortalRightInset = 12.0f;
	// 각 프레임에서 투명 여백을 제외한 포탈 본체입니다. 발까지 높이는 76px입니다.
	private static readonly Rect PortalSource = new(52.0f, 10.0f, 55.0f, 76.0f);

	// 2. 발판 배치 설정 (1280 × 720 기준, 좌표는 화면 왼쪽 위부터 시작)
	private const int FloorCount = 3;
	private const float PlatformLeftX = 33.0f;
	private const float FirstFloorTopY = 620.0f; // 1층: 아래쪽
	private const float FloorSpacing = 200.0f;   // 2층: 420, 3층: 220
	// 파란 발판(약 160px)의 중앙에 기본 자세 캐릭터(84px)를 맞춥니다.
	private const float PlayerLeftInset = (160.0f - 84.0f) * 0.5f;
	private const float BluePlatformWidth = 160.0f;

	// PNG의 넓은 투명 여백을 제외하고, 실제 흰색 발판 부분만 사용합니다.
	// Rect 인자는 왼쪽, 위쪽, 너비, 높이입니다. 원본 PNG는 수정하지 않습니다.
	private static readonly Rect PlatformSource = new(33.0f, 413.0f, 1216.0f, 32.0f);
	private static readonly Rect BackgroundSource = new(0.0f, 0.0f, 1280.0f, 720.0f);

	// 3. 로드한 이미지와 층별 출력 영역
	private G2Texture? _background;
	private G2Texture? _platform;
	private G2Texture? _portal;
	private double _portalAnimationTime;
	private readonly Rect[] _platformDestinations = new Rect[FloorCount];
	private Player? _player;
	private G2Audio? _gameplayBgm;
	private GameplaySession? _session;
	private MonsterRenderer? _monsters;
	private G2Font? _hudFont;
	private G2Font? _messageFont;
	private ID2D1SolidColorBrush? _panelBrush;
	public bool ReturnToTitleRequested { get; private set; }

	public void Initialize()
	{
		LoadResources();
		ArrangePlatforms();
		CreateGameplay();
		CreatePlayer();
		StartGameplayMusic();
	}

	private void CreateGameplay()
	{
		float portalCenter = PlatformLeftX + PlatformSource.Width - PortalRightInset - PortalSource.Width * 0.5f;
		float playerLeft = PlatformLeftX + PlayerLeftInset;
		_session = new GameplaySession(portalCenter - GameplaySession.MonsterWidth * 0.5f,
			PlatformLeftX, BluePlatformWidth, playerLeft + 18.0f, 48.0f);
		_monsters = new MonsterRenderer();
		_hudFont = new G2Font("Malgun Gothic", 23.0f);
		_messageFont = new G2Font("Malgun Gothic", 38.0f);
		_panelBrush = G2AppBase.Instance!.RenderTarget.CreateSolidColorBrush(new Color4(0.03f, 0.06f, 0.16f, 0.88f));
	}

	private void LoadResources()
	{
		_background = new G2Texture(BackgroundPath);
		_platform = new G2Texture(PlatformPath); // 한 번만 로드하여 세 층에서 공유합니다.
		_portal = new G2Texture(PortalPath); // 포탈 시트도 세 층에서 공유합니다.
	}

	private void ArrangePlatforms()
	{
		for (int floorIndex = 0; floorIndex < FloorCount; floorIndex++)
		{
			float topY = FirstFloorTopY - floorIndex * FloorSpacing;
			_platformDestinations[floorIndex] = new Rect(
				PlatformLeftX, topY, PlatformSource.Width, PlatformSource.Height);
		}
	}

	// 4. 플레이어 생성 및 입력 갱신
	private void CreatePlayer()
	{
		// 발판 배치에서 높이를 가져오므로 층 간격을 바꿔도 캐릭터가 발판에 맞춰집니다.
		float[] floorTopYs = _platformDestinations.Select(platform => platform.Y).ToArray();
		_player = new Player(PlatformLeftX + PlayerLeftInset, floorTopYs, _session!);
		_player.Initialize();
	}

	public void Update(G2InputContext input, double deltaTime)
	{
		// 24프레임을 20FPS로 반복하며, 프레임 속도와 관계없이 같은 속도로 재생합니다.
		_portalAnimationTime = (_portalAnimationTime + deltaTime) % (PortalFrameCount / PortalFramesPerSecond);
		if (_session!.IsGameOver)
		{
			_session.Update(deltaTime); // 마지막으로 터지는 애니메이션은 마저 재생합니다.
			if (input.IsKeyDown(Keys.R) || input.IsKeyDown(Keys.Enter))
			{
				_session.Reset();
				input.Reset();
				_gameplayBgm?.Play(isLooping: true);
			}
			else if (input.IsKeyDown(Keys.Escape))
			{
				ReturnToTitleRequested = true;
			}
			return;
		}
		_player?.Update(input);
		_session.Update(deltaTime);
		if (_session.IsGameOver)
		{
			_gameplayBgm?.Stop();
		}
	}

	// 5. 몬스터보다 포탈을 나중에 그려 포탈 속에서 걸어 나오는 모습으로 만듭니다.
	public void Render()
	{
		DrawBackground();
		DrawPlatforms();
		foreach (MonsterState monster in _session!.Monsters)
		{
			_monsters?.Render(monster, _platformDestinations[monster.Floor].Y);
		}
		DrawPortals();
		_player?.Render();
		DrawGameplayHud();
	}

	private void DrawGameplayHud()
	{
		GameplaySession session = _session!;
		var target = G2AppBase.Instance!.RenderTarget;
		target.FillRectangle(new Rect(20.0f, 16.0f, 1240.0f, 88.0f), _panelBrush!);
		Color4 white = new(1.0f, 1.0f, 1.0f, 1.0f);
		Color4 healthColor = session.HealthFeedbackTime > 0.0
			? (session.LastHealthChange > 0 ? new(0.45f, 1.0f, 0.65f, 1.0f) : new(1.0f, 0.35f, 0.4f, 1.0f))
			: white;
		_hudFont?.DrawText($"ROUND {session.Round}   |   등장 {session.SpawnedThisRound} / 50   |   처리 {session.ResolvedThisRound} / 50",
			new Rect(40.0f, 25.0f, 880.0f, 34.0f), white);
		_hudFont?.DrawText($"HP {session.Health}", new Rect(1050.0f, 25.0f, 180.0f, 34.0f), healthColor);
		_hudFont?.DrawText("↑ ↓ 층 이동   SPACE 공격   |   가시는 펀치 · 하트는 같은 층에서 받기",
			new Rect(40.0f, 63.0f, 1190.0f, 32.0f), new Color4(0.7f, 0.84f, 1.0f, 1.0f));
		if (session.IsGameOver)
		{
			target.FillRectangle(new Rect(260.0f, 245.0f, 760.0f, 205.0f), _panelBrush!);
			_messageFont?.DrawText("GAME OVER", new Rect(475.0f, 270.0f, 480.0f, 64.0f), white);
			_hudFont?.DrawText($"도달 라운드 {session.Round}   ·   처리 {session.ResolvedThisRound} / 50",
				new Rect(390.0f, 345.0f, 600.0f, 36.0f), white);
			_hudFont?.DrawText("R / ENTER 다시 시작     ESC 타이틀",
				new Rect(390.0f, 394.0f, 620.0f, 36.0f), white);
		}
	}

	private void DrawBackground()
	{
		Rect screenBounds = new(0.0f, 0.0f,
			GameGlobal.ScreenSize.Width, GameGlobal.ScreenSize.Height);
		_background?.Draw(screenBounds, BackgroundSource);
	}

	private void DrawPlatforms()
	{
		foreach (Rect destination in _platformDestinations)
		{
			_platform?.Draw(destination, PlatformSource);
		}
	}

	private void DrawPortals()
	{
		int frameIndex = Math.Min((int)(_portalAnimationTime * PortalFramesPerSecond), PortalFrameCount - 1);
		Rect source = new(
			frameIndex * PortalFrameWidth + PortalSource.X, PortalSource.Y,
			PortalSource.Width, PortalSource.Height);
		foreach (Rect platform in _platformDestinations)
		{
			// 발판 오른쪽 끝에서 12px 안쪽에 두고, 포탈의 발을 발판 윗면에 맞춥니다.
			Rect destination = new(
				platform.X + platform.Width - PortalRightInset - PortalSource.Width,
				platform.Y - PortalSource.Height, PortalSource.Width, PortalSource.Height);
			_portal?.Draw(destination, source);
		}
	}

	// 6. 플레이 장면 BGM: 장면에 들어올 때 한 번 시작하고, 끝까지 무한 반복합니다.
	private void StartGameplayMusic()
	{
		_gameplayBgm = new G2Audio(GameplayBgmPath);
		_gameplayBgm.Play(isLooping: true);
	}

	private void StopGameplayMusic()
	{
		_gameplayBgm?.Stop();
		_gameplayBgm?.Dispose();
		_gameplayBgm = null;
	}

	// 7. 장면 종료 시 BGM과 이미지 해제
	public void Dispose()
	{
		// 메인 화면 등 다른 장면으로 전환할 때도 이 장면을 Dispose하면 음악이 멈춥니다.
		StopGameplayMusic();
		_player?.Dispose();
		_player = null;
		_monsters?.Dispose();
		_monsters = null;
		_session = null;
		_panelBrush?.Dispose();
		_panelBrush = null;
		_messageFont?.Dispose();
		_messageFont = null;
		_hudFont?.Dispose();
		_hudFont = null;
		_portal?.Dispose();
		_portal = null;
		_platform?.Dispose();
		_platform = null;
		_background?.Dispose();
		_background = null;
	}
}
