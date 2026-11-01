using Vortice.Mathematics;

// 게임 플레이 화면의 리소스, 발판 배치, 그리기와 BGM을 담당합니다.
class SceneMain : IDisposable
{
	// 1. 리소스 경로
	private const string BackgroundPath = "resource/background/PunchStep_Background.png";
	private const string PlatformPath = "resource/background/PunchStep_Platform.png";
	private const string GameplayBgmPath = "resource/sound/bgm/PunchStep_Gameplay.mp3";

	// 2. 발판 배치 설정 (1280 × 720 기준, 좌표는 화면 왼쪽 위부터 시작)
	private const int FloorCount = 3;
	private const float PlatformLeftX = 33.0f;
	private const float FirstFloorTopY = 620.0f; // 1층: 아래쪽
	private const float FloorSpacing = 200.0f;   // 2층: 420, 3층: 220
	private const float PlayerLeftInset = 16.0f; // 발판 왼쪽 끝에서 캐릭터까지의 여유

	// PNG의 넓은 투명 여백을 제외하고, 실제 흰색 발판 부분만 사용합니다.
	// Rect 인자는 왼쪽, 위쪽, 너비, 높이입니다. 원본 PNG는 수정하지 않습니다.
	private static readonly Rect PlatformSource = new(33.0f, 413.0f, 1216.0f, 32.0f);
	private static readonly Rect BackgroundSource = new(0.0f, 0.0f, 1280.0f, 720.0f);

	// 3. 로드한 이미지와 층별 출력 영역
	private G2Texture? _background;
	private G2Texture? _platform;
	private readonly Rect[] _platformDestinations = new Rect[FloorCount];
	private Player? _player;
	private G2Audio? _gameplayBgm;

	public void Initialize()
	{
		LoadResources();
		ArrangePlatforms();
		CreatePlayer();
		StartGameplayMusic();
	}

	private void LoadResources()
	{
		_background = new G2Texture(BackgroundPath);
		_platform = new G2Texture(PlatformPath); // 한 번만 로드하여 세 층에서 공유합니다.
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
		_player = new Player(PlatformLeftX + PlayerLeftInset, floorTopYs);
		_player.Initialize();
	}

	public void Update(G2InputContext input)
	{
		_player?.Update(input);
	}

	// 5. 출력 순서: 배경 → 발판 → 플레이어
	public void Render()
	{
		DrawBackground();
		DrawPlatforms();
		_player?.Render();
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
		_platform?.Dispose();
		_platform = null;
		_background?.Dispose();
		_background = null;
	}
}
